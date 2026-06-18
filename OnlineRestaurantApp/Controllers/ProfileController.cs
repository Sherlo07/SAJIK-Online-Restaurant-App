using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineRestaurantApp.Controllers
{
    [Route("profile")]
    public class ProfileController : Controller
    {
        private readonly OnlineRestaurantDbContext _db;

        public ProfileController(OnlineRestaurantDbContext db) => _db = db;

        private string? GetEmail() => HttpContext.Session.GetString("loggedinuser");

        /// <summary>
        /// Redirects to Login with a returnUrl built from the current request (path + query).
        /// </summary>
        private IActionResult GoLoginCurrent()
        {
            var returnUrl = $"{Request.Path}{Request.QueryString}";
            return RedirectToAction("Login", "User", new { returnUrl });
        }

        /// <summary>
        /// Redirects to Login with a specific returnUrl you pass.
        /// </summary>
        private IActionResult GoLogin(string returnUrl)
            => RedirectToAction("Login", "User", new { returnUrl });

        // ==============================
        // ✅ GET: /profile/orders
        // ==============================
        [HttpGet("orders")]
        public async Task<IActionResult> MyOrders()
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return GoLoginCurrent();

            // Ensure user exists
            var user = await _db.users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return GoLoginCurrent();

            // include delivery to decide status
            var orders = await _db.orders
                .Include(o => o.Delivery)
                .Where(o => o.UserId == user.UserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lazy-mark delivered if due
            var now = DateTime.Now;
            var activeStatuses = new[] { "Assigned", "InTransit", "OutForDelivery" };
            bool changed = false;

            foreach (var o in orders)
            {
                var d = o.Delivery;
                if (d?.ExpectedOn != null &&
                    d.ExpectedOn <= now &&
                    activeStatuses.Contains(d.DeliveryStatus))
                {
                    d.DeliveryStatus = "Delivered";
                    changed = true;
                }
            }
            if (changed)
                await _db.SaveChangesAsync();

            return View("MyOrders", orders);
        }

        // ==============================
        // ✅ GET: /profile/payments
        // ==============================
        [HttpGet("payments")]
        public async Task<IActionResult> MyPayments()
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return GoLoginCurrent();

            var user = await _db.users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return GoLoginCurrent();

            var payments = await _db.payments
                .Where(p => p.UserId == user.UserId)
                .OrderByDescending(p => p.PaidOnDate)
                .AsNoTracking()
                .ToListAsync();

            return View("MyPayments", payments);
        }

        // ==============================
        // ✅ GET: /profile/address
        // ==============================
        [HttpGet("address")]
        public async Task<IActionResult> DeliveryAddress()
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return GoLoginCurrent();

            var user = await _db.users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return GoLoginCurrent();

            var addr = await _db.DeliveryAddresses
                .Where(a => a.UserId == user.UserId && a.IsDefault)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return View("DeliveryAddress", addr ?? new DeliveryAddress());
        }

        // ==============================
        // ✅ POST: /profile/address
        // ==============================
        [HttpPost("address")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAddress(
            string addressLine1,
            string? addressLine2,
            string city,
            string state,
            string pincode,
            string? landmark)
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return GoLoginCurrent();

            var user = await _db.users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return GoLoginCurrent();

            if (string.IsNullOrWhiteSpace(addressLine1) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(state) ||
                string.IsNullOrWhiteSpace(pincode))
            {
                TempData["Err"] = "Please fill Address, City, State and Pincode.";
                return RedirectToAction("DeliveryAddress");
            }

            var digitsOnlyPincode = new string((pincode ?? "").Where(char.IsDigit).ToArray());
            if (digitsOnlyPincode.Length < 5 || digitsOnlyPincode.Length > 10)
            {
                TempData["Err"] = "Please enter a valid pincode.";
                return RedirectToAction("DeliveryAddress");
            }

            // Find existing default
            var existingDefault = await _db.DeliveryAddresses
                .FirstOrDefaultAsync(a => a.UserId == user.UserId && a.IsDefault);

            if (existingDefault == null)
            {
                // Make others non-default
                var all = await _db.DeliveryAddresses.Where(a => a.UserId == user.UserId).ToListAsync();
                foreach (var a in all) a.IsDefault = false;

                var newAddr = new DeliveryAddress
                {
                    UserId = user.UserId,
                    AddressLine1 = addressLine1.Trim(),
                    AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),
                    City = city.Trim(),
                    State = state.Trim(),
                    Pincode = digitsOnlyPincode.Trim(),
                    Landmark = string.IsNullOrWhiteSpace(landmark) ? null : landmark.Trim(),
                    IsDefault = true,
                    CreatedOn = DateTime.UtcNow
                };

                _db.DeliveryAddresses.Add(newAddr);
            }
            else
            {
                existingDefault.AddressLine1 = addressLine1.Trim();
                existingDefault.AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
                existingDefault.City = city.Trim();
                existingDefault.State = state.Trim();
                existingDefault.Pincode = digitsOnlyPincode.Trim();
                existingDefault.Landmark = string.IsNullOrWhiteSpace(landmark) ? null : landmark.Trim();
                existingDefault.IsDefault = true;

                _db.DeliveryAddresses.Update(existingDefault);
            }

            await _db.SaveChangesAsync();

            TempData["Ok"] = "Delivery address saved successfully.";
            return RedirectToAction("DeliveryAddress");
        }

        // ==============================
        // ✅ GET: /profile/feedbacks
        // ==============================
        [HttpGet("feedbacks")]
        public async Task<IActionResult> MyFeedbacks()
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return GoLoginCurrent();

            // Ensure user exists (optional guard)
            var user = await _db.users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return GoLoginCurrent();

            // Fetch this user's feedbacks ordered by newest first
            var feedbacks = await _db.feedbacks
                .AsNoTracking()
                .Where(f => f.Email == email)
                .OrderByDescending(f => f.CreatedOn)
                .ToListAsync();

            return View("MyFeedbacks", feedbacks);
        }
    }
}