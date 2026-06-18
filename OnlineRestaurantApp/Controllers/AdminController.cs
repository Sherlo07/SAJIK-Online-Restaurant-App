using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;

namespace OnlineRestaurantApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly OnlineRestaurantDbContext _context;

        public AdminController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        private bool IsAdminLoggedIn()
        {
            var user = HttpContext.Session.GetString("loggedinuser");
            var role = HttpContext.Session.GetString("loggedinuserRole");

            return !string.IsNullOrEmpty(user) &&
                   string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        // -------------------- ADMIN DASHBOARD --------------------
        
        public IActionResult Index()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "User");

            ViewBag.loggedInUserId = HttpContext.Session.GetString("loggedinuser");
            return View(); // Index.cshtml + _AdminLayout.cshtml
        }



        [HttpGet]
        public async Task<IActionResult> Orders(string status = "All")
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "User");

            var now = DateTime.Now;
            var activeStatuses = new[] { "Assigned", "InTransit", "OutForDelivery" };

            // Include delivery, payment, and items so the view can render everything
            IQueryable<Order> query = _context.orders
                .Include(o => o.user)
                .Include(o => o.payment)
                .Include(o => o.orderDetailsItems)
                .Include(o => o.Delivery)          // <-- new
                .AsQueryable();

            // Keep your existing filter semantics
            switch (status?.Trim().ToLowerInvariant())
            {
                case "assigned":
                    query = query.Where(o => o.IsAssigned);
                    break;
                case "not assigned":
                    query = query.Where(o => !o.IsAssigned);
                    break;
                default:
                    // All
                    break;
            }

            // (Optional, but matches user side) Lazy mark "Delivered" when ExpectedOn has passed
            // This keeps admin and user views consistent without needing a background job.
            var due = await _context.Deliveries
                .Where(d => d.ExpectedOn != null
                            && d.ExpectedOn <= now
                            && activeStatuses.Contains(d.DeliveryStatus))
                .ToListAsync();
            if (due.Count > 0)
            {
                foreach (var d in due) d.DeliveryStatus = "Delivered";
                await _context.SaveChangesAsync();
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders); // Views/Admin/Orders.cshtml
        }


        private const int PartnerBusyWindowMinutes = 30;
        private static readonly string[] ActiveStatuses = { "Assigned", "InTransit", "OutForDelivery" };

        // -------------------- DELIVERY MANAGEMENT --------------------
        [HttpGet]
        public async Task<IActionResult> DeliveryManagement()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "User");

            var now = DateTime.Now;  // local time

            // Unassigned, paid orders
            var unassignedOrders = await _context.orders
                .Include(o => o.payment)
                .Include(o => o.user)
                .Where(o => o.payment != null && o.payment.Status == true
                            && o.IsAssigned == false)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderAmount,
                    Customer = o.user != null ? o.user.UserFirstName : "Customer"
                })
                .ToListAsync();

            ViewBag.OrdersSelectList = unassignedOrders.Select(o => new SelectListItem
            {
                Value = o.OrderId.ToString(),
                Text = $"#{o.OrderId} • ₹{o.OrderAmount:0.00} • {o.Customer}"
            }).ToList();

            // Busy partners = have an ACTIVE delivery whose AssignedOn is within last 30 mins
            var busyEmployeeIds = await _context.Deliveries
                .Where(d => ActiveStatuses.Contains(d.DeliveryStatus)
                            && d.AssignedOn > now.AddMinutes(-PartnerBusyWindowMinutes))
                .Select(d => d.EmployeeId)
                .Distinct()
                .ToListAsync();

            var availableEmployees = await _context.Employees
                .Where(e => e.IsActive && !busyEmployeeIds.Contains(e.EmployeeId))
                .OrderBy(e => e.FullName)
                .Select(e => new { e.EmployeeId, e.FullName, e.Role })
                .ToListAsync();

            ViewBag.EmployeesSelectList = availableEmployees.Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = $"{e.FullName} ({e.Role})"
            }).ToList();

            return View(); // Views/Admin/DeliveryManagement.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDelivery(int? selectedOrderId, int? selectedEmployeeId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login", "User");

            if (selectedOrderId is null || selectedEmployeeId is null)
            {
                TempData["Err"] = "Please select both an Order and an Employee.";
                return RedirectToAction(nameof(DeliveryManagement));
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Load order
                var order = await _context.orders
                    .Include(o => o.payment)
                    .FirstOrDefaultAsync(o => o.OrderId == selectedOrderId.Value);

                if (order == null)
                {
                    TempData["Err"] = "Order not found.";
                    return RedirectToAction(nameof(DeliveryManagement));
                }

                if (order.payment?.Status != true)
                {
                    TempData["Err"] = $"Order #{order.OrderId} is not paid.";
                    return RedirectToAction(nameof(DeliveryManagement));
                }

                if (order.IsAssigned == true)
                {
                    TempData["Err"] = $"Order #{order.OrderId} is already assigned.";
                    return RedirectToAction(nameof(DeliveryManagement));
                }

                // Load employee
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == selectedEmployeeId.Value && e.IsActive);

                if (employee == null)
                {
                    TempData["Err"] = "Employee not found or inactive.";
                    return RedirectToAction(nameof(DeliveryManagement));
                }

                // Ensure not busy RIGHT NOW (use same window rule as GET)
                var now = DateTime.Now;
                var isBusy = await _context.Deliveries
                    .AnyAsync(d => d.EmployeeId == employee.EmployeeId
                                   && ActiveStatuses.Contains(d.DeliveryStatus)
                                   && d.AssignedOn > now.AddMinutes(-PartnerBusyWindowMinutes));
                if (isBusy)
                {
                    TempData["Err"] = $"{employee.FullName} is currently busy. Please try another partner.";
                    return RedirectToAction(nameof(DeliveryManagement));
                }

                // Choose address (default first, else most recent)
                var addressId = await _context.DeliveryAddresses
                    .Where(a => a.UserId == order.UserId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedOn)
                    // ⚠️ Pick the correct PK property for your DeliveryAddress entity:
                    .Select(a => (int?)a.AddressId) // change to a.AddressId if that's your PK
                    .FirstOrDefaultAsync();

                if (addressId is null)
                {
                    TempData["Err"] = $"No delivery address found for Order #{order.OrderId}.";
                    return RedirectToAction(nameof(DeliveryManagement));
                }

                // Time: expect 30 mins from order timestamp (with small grace if already past)
                var expected = order.OrderDate.AddMinutes(30);
                if (expected < now) expected = now.AddMinutes(10);

                // Create Delivery
                var delivery = new Delivery
                {
                    OrderId = order.OrderId,
                    EmployeeId = employee.EmployeeId,
                    AddressId = addressId.Value,
                    AssignedOn = now,
                    ExpectedOn = expected,
                    DeliveryStatus = "Assigned"
                };
                _context.Deliveries.Add(delivery);

                // Mark order assigned
                order.IsAssigned = true;
                _context.orders.Update(order);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Msg"] = $"Order #{order.OrderId} assigned to {employee.FullName}.";
                return RedirectToAction(nameof(DeliveryManagement));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Err"] = ex.InnerException?.Message ?? ex.Message;
                return RedirectToAction(nameof(DeliveryManagement));
            }
        }

    }
    }

