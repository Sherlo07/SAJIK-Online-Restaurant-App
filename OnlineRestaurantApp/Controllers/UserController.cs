using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;

namespace OnlineRestaurantApp.Controllers
{
    public class UserController : Controller
    {
        private readonly OnlineRestaurantDbContext _context;

        public UserController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        // ------------------------- AUTH ------------------------- 

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Keep raw returnUrl for the view; we'll validate before redirecting on POST
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateUser(UserLogin ul, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View("Login", ul);

            var email = (ul.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = await _context.users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "The username you entered is not registered.");
                return View("Login", ul);
            }

            if (!string.Equals(user.Password, ul.Password, StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "The password you entered is incorrect.");
                return View("Login", ul);
            }

            if (user.Status != true)
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact the administrator.");
                return View("Login", ul);
            }

            var role = await _context.roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
            if (role == null)
            {
                ModelState.AddModelError(string.Empty, "Could not verify user role.");
                return View("Login", ul);
            }

            HttpContext.Session.Remove("CART");
            HttpContext.Session.SetString("loggedinuser", user.Email);
            HttpContext.Session.SetString("loggedinuserRole", role.RoleName);

            // ---- sanitize returnUrl ----
            string SafeDefault() => "/FoodItems/Browse"; // or "/cart/items" if you prefer

            bool IsUnsafeApi(string url)
            {
                if (string.IsNullOrWhiteSpace(url)) return true;
                url = url.ToLowerInvariant();

                // POST-only or API-like endpoints you should not GET after login:
                if (url.StartsWith("/cart/add") || url.StartsWith("/cart/update")
                    || url.StartsWith("/cart/summary") || url.StartsWith("/cart/count"))
                    return true;

                // You may *allow* these cart GET pages:
                // /cart/items and /cart/checkout are safe GET pages—no block needed.

                return false;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && !IsUnsafeApi(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Fallback: role-based or a single common page
            if (string.Equals(role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Admin");

            if (string.Equals(role.RoleName, "Customer", StringComparison.OrdinalIgnoreCase))
                return Redirect(SafeDefault());

            ModelState.AddModelError(string.Empty, "Your account has an unrecognized role configuration.");
            return View("Login", ul);
        }


        [HttpGet] // Keep GET if your navbar uses a link; switch to [HttpPost] + anti-forgery if you use a form.
        public IActionResult Logout()
        {
            // Clear session-based auth flags
            HttpContext.Session.Remove("loggedinuser");
            HttpContext.Session.Remove("loggedinuserRole");

            // ✅ Clear cart so checkout count becomes 0 immediately after logout
            HttpContext.Session.Remove("CART");

            // You could also call HttpContext.Session.Clear() to remove everything
            // HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            // Strongly typed to User in the view; no prefill needed
            return View();
        }


        [HttpPost]

        public IActionResult VerifyEmail(User model)

        {

            if (model.Email == null)

            {

                return View(model);

            }

            var user = _context.users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)

            {

                ModelState.AddModelError("Email", "This email is not registered.");

                return View(model);

            }

            // If email exists, redirect to ResetPassword

            return RedirectToAction("ResetPassword", new { email = model.Email });

        }

        public IActionResult ResetPassword(string email)

        {

            ViewBag.Email = email;

            return View(); // This loads ResetPassword.cshtml

        }

        [HttpPost]

        public IActionResult ResetPasswordUser(string email, string newPassword, string confirmPassword)

        {

            if (newPassword != confirmPassword)

            {

                ModelState.AddModelError(" ", "New Password and Confirm Password do not match.");

                ViewBag.Email = email; // keep email in view

                return View();

            }

            var user = _context.users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            if (user != null)

            {

                user.Password = newPassword;

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Password reset successful! Please login with your new credentials.";

                return RedirectToAction("Login");

            }

            ModelState.AddModelError("", "Could not reset password. Email not found.");

            return View();

        }

        [HttpGet]

        public IActionResult ChangePassword()

        {

            var username = HttpContext.Session.GetString("loggedinuser");

            if (string.IsNullOrEmpty(username))

            {

                return RedirectToAction("Login");

            }

            return View();

        }

        [HttpPost]

        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)

        {

            var email = HttpContext.Session.GetString("loggedinuser");

            if (string.IsNullOrEmpty(email))

            {

                return RedirectToAction("Login");

            }

            var user = _context.users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            if (user == null)

            {

                ModelState.AddModelError("", "User not found.");

                return View();

            }

            // Step 1: Check current password

            if (user.Password != currentPassword)

            {

                ModelState.AddModelError("", "Current password is incorrect.");

                return View();

            }

            // Step 2: Check new vs confirm

            if (newPassword != confirmPassword)

            {

                ModelState.AddModelError("", "New password and confirm password do not match.");

                return View();

            }

            // Step 3: Update password

            user.Password = newPassword;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password changed successfully!";

            return View();

        }

        

        


        // --------------------- REGISTRATION ---------------------

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            Role? userRole = _context.roles.FirstOrDefault(r => r.RoleName == "Customer");

            var newUser = new User
            {
                RoleId = userRole?.RoleId ?? 2
            };

            if (userRole != null)
                ViewBag.defaultRole = userRole.RoleName;

            return View(newUser);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterUser(User u)
        {
            u.CreatedOn = DateTime.Now;
            u.Status = true;

            if (!ModelState.IsValid)
                return View("Register", u);

            try
            {
                var email = (u.Email ?? string.Empty).Trim().ToLowerInvariant();

                if (_context.users.Any(user => user.Email.ToLower() == email))
                {
                    ModelState.AddModelError("UserId", "This Username is already taken.");
                    return View("Register", u);
                }

                // Default role: Customer
                Role? defaultRole = _context.roles.FirstOrDefault(r => r.RoleName == "Customer");
                u.RoleId = defaultRole?.RoleId ?? 2;

                _context.users.Add(u);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login with your new credentials.";
                return RedirectToAction("Login");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration.");
                return View("Register", u);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCustomerStatus(int userId)
        {
            // Optional: authorization (kept from your code)
            string loggedInUser = HttpContext.Session.GetString("loggedinuser");
            string loggedinuserRole = HttpContext.Session.GetString("loggedinuserRole");
            if (loggedInUser == null || loggedinuserRole != "Admin")
            {
                return RedirectToAction("Login", "User");
            }

            // 1) Load tracked entity
            var user = await _context.users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Customers), "User");
            }

            // 2) Flip the value stored in DB (do NOT rely on hidden field)
            bool oldStatus = user.Status;
            user.Status = !user.Status;

            try
            {
                // 3) Save
                await _context.SaveChangesAsync();

                // 4) Feedback
                TempData["Success"] = $"Customer {(user.Status ? "enabled" : "disabled")} successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "The user could not be updated due to a concurrency issue. Please try again.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the user.";
                // Optional: log ex
            }

            // 5) Always redirect to the correct controller/action
            return RedirectToAction(nameof(Customers), "User");
        }
        // Example list action name (adjust if your list action is different)
        public async Task<IActionResult> Customers()
        {
            // Find the RoleId for "Customer"
            var customerRoleId = await _context.roles
                .Where(r => r.RoleName == "Customer")
                .Select(r => r.RoleId)
                .FirstOrDefaultAsync();

            // If role not present, return empty list with a message
            if (customerRoleId == 0)
            {
                TempData["Error"] = "Customer role not configured.";
                return View(Enumerable.Empty<User>());
            }

            // Get only users with RoleId == Customer
            var users = await _context.users
                .AsNoTracking()
                .Where(u => u.RoleId == customerRoleId)
                .OrderBy(u => u.UserFirstName)
                .ThenBy(u => u.UserLastName)
                .ToListAsync();

            return View(users);
        }


        // ------------------------ USER HOME ------------------------

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}