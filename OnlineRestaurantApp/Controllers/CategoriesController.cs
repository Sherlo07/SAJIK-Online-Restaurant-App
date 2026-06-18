using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;
using OnlineRestaurantApp.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineRestaurantApp.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly OnlineRestaurantDbContext _context;

        public CategoriesController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        [Route("irfan")]
        public async Task<IActionResult> Index(
                           string sortOrder,
                           string currentFilter,
                           string searchString,
                           int? pageNumber)
        {

            //validate
            string? loggedInUser = HttpContext.Session.GetString("loggedinuser");
            string? loggedinuserRole = HttpContext.Session.GetString("loggedinuserRole");

            if (loggedInUser != null && loggedinuserRole == "Admin")
            {
                ViewBag.loggedInUserId = loggedInUser;
                ViewData["CurrentSort"] = sortOrder;
                ViewData["NameSort"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                ViewData["DiscountSort"] = sortOrder == "disc_asce" ? "disc_desc" : "disc_asce";

                if (searchString != null)
                {
                    pageNumber = 1;
                }
                else
                {
                    searchString = currentFilter;
                }

                var categories = from c in _context.categories
                                 select c;

                if (searchString != null)
                {
                    categories = from c in _context.categories
                                 where c.CategoryName.Contains(searchString)
                                 select c;
                    ViewData["CurrentFilter"] = searchString;
                }
                switch (sortOrder)
                {
                    case "name_desc":
                        categories = from c in categories
                                     orderby c.CategoryName descending
                                     select c;
                        ViewData["NameSort"] = "";
                        break;

                    case "disc_desc":
                        categories = from c in categories
                                     orderby c.CategoryDiscount descending
                                     select c;
                        ViewData["DiscountSort"] = "disc_asce";
                        break;
                    case "disc_asce":
                        categories = from c in categories
                                     orderby c.CategoryDiscount
                                     select c;
                        ViewData["DiscountSort"] = "disc_desc";
                        break;
                    default:
                        categories = from c in categories
                                     orderby c.CategoryName
                                     select c;
                        ViewData["NameSort"] = "name_desc";
                        break;
                }

                var filteredTotal = await categories.CountAsync();                // after filter
                var overallTotal = await _context.categories.CountAsync();       // all rows

                ViewData["FilteredTotal"] = filteredTotal;
                ViewData["OverallTotal"] = overallTotal;

                int pageSize = 3;
                return View(await PaginatedList<Category>.CreateAsync(categories.AsNoTracking(), pageNumber ?? 1, pageSize));

            }

            return RedirectToAction("Login", "User"); // Login.cshtml + _Layout.cshtml
        }





        
        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,CategoryDescritpion,CategoryImage,CategoryStatus,CategoryDiscount")] Category category)
        {
            if (category.CategoryImage == null || category.CategoryImage.Length == 0 || category.CategoryImage.Length > 524288 || Path.GetExtension(category.CategoryImage.FileName) != ".jpg")
            {
                ModelState.AddModelError("CategoryImage", "Invalid Image Size");
                return View(category);
            }
            string fileName = category.CategoryImage.FileName; //biryani.jpg
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories/", fileName); //wwwroot/images/categories/biryani.jpg
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await category.CategoryImage.CopyToAsync(fs);
                category.CategoryImagePath = "/images/categories/" + fileName;
            }
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,CategoryDescritpion,CategoryImage,CategoryImagePath,CategoryStatus,CategoryDiscount")] Category category)
        {
            
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (category.CategoryImage != null)
            {
                if (category.CategoryImage.Length > 524588 || Path.GetExtension(category.CategoryImage.FileName) != ".jpg")
                {
                    ModelState.AddModelError("CategoryImage", "Add valid size");
                    return View(category);
                }
                else
                {
                    string fileName = category.CategoryImage.FileName;
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories/", fileName);
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await category.CategoryImage.CopyToAsync(fs);
                        category.CategoryImagePath = "/images/categories/" + fileName;
                    }

                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.categories.FindAsync(id);
            if (category != null)
            {
                _context.categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.categories.Any(e => e.CategoryId == id);
        }
    }
}
