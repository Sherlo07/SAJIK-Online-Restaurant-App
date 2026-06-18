using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Filters;
using OnlineRestaurantApp.IRepository;
using OnlineRestaurantApp.Components;
using Microsoft.AspNetCore.Http;
using System.IO; // Path, FileStream
using System.Linq;
using System;
using System.Threading.Tasks;

namespace OnlineRestaurantApp.Controllers
{
    [ServiceFilter(typeof(ActionLogFilter))]
    public class FeedbackController : Controller
    {
        private readonly IFeedbackRepository _feedbackRepository;

        public FeedbackController(IFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        // -------------------- LIST --------------------
        // GET: /Feedback

        public async Task<IActionResult> Index(
                    string? createdRange,  // "", "Today", "Last7", "Last30", "ThisMonth"
                    string? email,         // contains
                    int? pageNumber,
                    int? pageSize)
        {
            // Dropdown options for Created On
            var createdOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "All",          Value = "",          Selected = string.IsNullOrWhiteSpace(createdRange) },
                new SelectListItem { Text = "Today",        Value = "Today",     Selected = createdRange == "Today" },
                new SelectListItem { Text = "Last 7 days",  Value = "Last7",     Selected = createdRange == "Last7" },
                new SelectListItem { Text = "Last 30 days", Value = "Last30",    Selected = createdRange == "Last30" },
                new SelectListItem { Text = "This month",   Value = "ThisMonth", Selected = createdRange == "ThisMonth" },
            };
            ViewBag.CreatedOptions = createdOptions;

            int pageIndex = pageNumber.GetValueOrDefault(1);
            int size = pageSize.GetValueOrDefault(10);
            if (pageIndex <= 0) pageIndex = 1;
            if (size <= 0) size = 10;

            // Query repo
            var (items, total) = await _feedbackRepository.GetPagedAsync(createdRange, email, pageIndex, size);

            // Pass UI state via ViewBag (no ViewModel)
            ViewBag.CreatedRange = createdRange ?? "";
            ViewBag.Email = email ?? "";
            ViewBag.PageIndex = pageIndex;
            ViewBag.PageSize = size;
            ViewBag.TotalCount = total;

            // computed for pagination
            var totalPages = (int)Math.Ceiling((double)total / size);
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = pageIndex > 1;
            ViewBag.HasNextPage = pageIndex < totalPages;

            return View(items);
        }


        // -------------------- DETAILS --------------------
        // GET: /Feedback/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _feedbackRepository.GetByIdAsync(id.Value);
            if (feedback == null) return NotFound();

            return View(feedback);
        }

        // -------------------- CREATE --------------------
        // GET: /Feedback/Create
        public IActionResult Create()
        {
            // Require login to submit feedback
            var email = HttpContext.Session.GetString("loggedinuser");
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login", "User", new { returnUrl = "/Feedback/Create" });

            return View();
        }

        // POST: /Feedback/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FeedbackId,Name,Subject,Description,ItemImage,Email,Mobile")] Feedback feedback)
        {
            // Require login (again, on POST)
            var sessionEmail = HttpContext.Session.GetString("loggedinuser");
            if (string.IsNullOrWhiteSpace(sessionEmail))
                return RedirectToAction("Login", "User", new { returnUrl = "/Feedback/Create" });

            // System fields
            feedback.Status = FbStatus.Open;
            feedback.RemarksByAdmin = string.Empty;
            feedback.CreatedOn = DateTime.Now;

            // Always associate to logged-in user (critical)
            feedback.Email = sessionEmail.Trim();

            // OPTIONAL: Handle image only if posted
            if (feedback.ItemImage != null && feedback.ItemImage.Length > 0)
            {
                try
                {
                    feedback.FeedbackImagePath = await SaveFeedbackImageAsync(feedback.ItemImage);
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError("ItemImage", ex.Message);
                    return View(feedback);
                }
            }

            if (!ModelState.IsValid)
                return View(feedback);

            await _feedbackRepository.InsertAsync(feedback);
            await _feedbackRepository.SaveAsync();

            // After creating, take user to their My Feedbacks
            return RedirectToAction("MyFeedbacks", "Profile");
        }

        // -------------------- EDIT --------------------
        // GET: /Feedback/Edit/5
        [HttpGet]
        public async Task<ActionResult<Feedback>> Edit(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _feedbackRepository.GetByIdAsync(id.Value);
            if (feedback == null) return NotFound();

            ViewBag.StatusList = new SelectList(
                Enum.GetValues(typeof(FbStatus)).Cast<FbStatus>()
                    .Select(s => new { Id = (int)s, Name = s.ToString() }),
                "Id", "Name", (int)feedback.Status
            );

            return View(feedback);
        }

        // POST: /Feedback/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("FeedbackId,Name,Subject,FeedbackImagePath,ItemImage,Description,Email,Mobile,Status,RemarksByAdmin,CreatedOn")]
            Feedback feedback)
        {
            if (id != feedback.FeedbackId)
                return NotFound();

            // Load existing tracked entity to preserve non‑editable fields & image
            var existing = await _feedbackRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            // If a new image is uploaded -> validate + save; else keep existing path
            if (feedback.ItemImage != null && feedback.ItemImage.Length > 0)
            {
                try
                {
                    var savedPath = await SaveFeedbackImageAsync(feedback.ItemImage);
                    existing.FeedbackImagePath = savedPath;
                }
                catch (InvalidDataException ex)
                {
                    ViewBag.StatusList = new SelectList(
                        Enum.GetValues(typeof(FbStatus)).Cast<FbStatus>()
                            .Select(s => new { Id = (int)s, Name = s.ToString() }),
                        "Id", "Name", (int)feedback.Status
                    );
                    ModelState.AddModelError("ItemImage", ex.Message);
                    return View(feedback);
                }
            }
            // else: keep existing.FeedbackImagePath as-is

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new SelectList(
                    Enum.GetValues(typeof(FbStatus)).Cast<FbStatus>()
                        .Select(s => new { Id = (int)s, Name = s.ToString() }),
                    "Id", "Name", (int)feedback.Status
                );
                return View(feedback);
            }

            // Map editable fields
            existing.Name = feedback.Name;
            existing.Subject = feedback.Subject;
            existing.Description = feedback.Description;
            existing.Email = feedback.Email;     // You can also force to session email if required
            existing.Mobile = feedback.Mobile;
            existing.Status = feedback.Status;
            existing.RemarksByAdmin = feedback.RemarksByAdmin;

            // Keep original CreatedOn
            existing.CreatedOn = existing.CreatedOn;

            await _feedbackRepository.UpdateAsync(existing);
            await _feedbackRepository.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // -------------------- DELETE --------------------
        // GET: /Feedback/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _feedbackRepository.GetByIdAsync(id.Value);
            if (feedback == null) return NotFound();

            return View(feedback);
        }

        // POST: /Feedback/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(id);
            if (feedback != null)
            {
                await _feedbackRepository.DeleteAsync(id);
                await _feedbackRepository.SaveAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // -------------------- Helpers --------------------
        private const long MaxImageBytes = 512 * 1024; // 512 KB
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

        private async Task<string> SaveFeedbackImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidDataException("File is empty.");

            if (file.Length > MaxImageBytes)
                throw new InvalidDataException("Image too large. Max size is 512 KB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidDataException("Only .jpg, .jpeg, .png files are allowed.");

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "feedbacks");
            Directory.CreateDirectory(uploads); // ensure folder exists

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploads, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(fs);
            }

            return "/images/feedbacks/" + fileName;
        }


    }
}