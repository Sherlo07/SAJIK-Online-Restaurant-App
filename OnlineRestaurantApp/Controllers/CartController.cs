using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;
using OnlineRestaurantApp.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineRestaurantApp.Controllers
{
    [Route("cart")]
    [IgnoreAntiforgeryToken] // remove this if you add antiforgery header from the client
    public class CartController : Controller
    {
        private readonly OnlineRestaurantDbContext _context;

        public CartController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        // ====================== SESSION-BASED AUTH CHECKS ======================
        private bool IsLoggedInBySession()
            => !string.IsNullOrEmpty(HttpContext.Session.GetString("loggedinuser"));

        private IActionResult UnauthorizedJsonToLogin()
        {
            // Prefer the page the user was on (GET page), not the API path.
            var referer = Request.Headers["Referer"].ToString();
            string safeReturn = "/FoodItems/Browse";

            // Extract only path+query from referer (avoid full absolute URL)
            if (!string.IsNullOrWhiteSpace(referer))
            {
                try
                {
                    var uri = new Uri(referer);
                    var pathQuery = uri.PathAndQuery;
                    if (Url.IsLocalUrl(pathQuery))
                        safeReturn = pathQuery;
                }
                catch
                {
                    // ignore malformed referer, keep fallback
                }
            }

            var loginUrl = Url.Action("Login", "User", new { returnUrl = safeReturn }) ?? "/User/Login";
            return Unauthorized(new { ok = false, needLogin = true, loginUrl });
        }

        // ====================== CART STORAGE KEYS (PER USER) ======================
        private string GetCartKey()
        {
            // Your app sets "loggedinuser" in session when the user logs in
            var user = HttpContext.Session.GetString("loggedinuser");
            return string.IsNullOrEmpty(user) ? "CART_ANON" : $"CART_{user}";
        }

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(GetCartKey()) ?? new List<CartItem>();

        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetObject(GetCartKey(), cart);

        // ====================== ENDPOINTS ======================

        // Total count for FAB (kept open so UI can render even for anonymous)
        [HttpGet("count")]
        public IActionResult Count()
        {
            var cart = GetCart();
            return Json(new { ok = true, count = cart.Sum(c => c.Quantity) });
        }

        // Prefill UI quantities on page load (kept open so browsing works)
        [HttpGet("summary")]
        public IActionResult Summary()
        {
            var cart = GetCart();
            var items = cart.Select(c => new
            {
                foodItemId = c.FoodItemId,
                name = c.Name,
                price = c.Price,
                quantity = c.Quantity,
                imagePath = c.ImagePath
            }).ToList();

            var total = cart.Sum(c => c.Price * c.Quantity);
            return Json(new { ok = true, items, total });
        }

        // Add 1 (or given qty) for an item — requires login via Session
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddOrUpdateDto dto)
        {
            // ⛔ Block anonymous by session
            if (!IsLoggedInBySession())
                return UnauthorizedJsonToLogin();

            if (dto == null || dto.ItemId <= 0)
                return BadRequest(new { ok = false });

            var item = await _context.foodItems.AsNoTracking()
                .Where(f => f.ItemId == dto.ItemId && f.IsAvailable)
                .Select(f => new
                {
                    f.ItemId,
                    f.ItemName,
                    f.ItemImagePath,
                    Price = f.SellingPrice > 0 ? f.SellingPrice : f.ActualPrice
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { ok = false });

            var cart = GetCart();
            var line = cart.FirstOrDefault(c => c.FoodItemId == item.ItemId);
            int addBy = dto.Qty <= 0 ? 1 : dto.Qty;

            if (line == null)
            {
                cart.Add(new CartItem
                {
                    FoodItemId = item.ItemId,
                    Name = item.ItemName ?? "",
                    Price = item.Price,
                    Quantity = addBy,
                    ImagePath = item.ItemImagePath
                });
            }
            else
            {
                line.Quantity += addBy;
            }

            cart = cart.Where(c => c.Quantity > 0).ToList();
            SaveCart(cart);

            var totalCount = cart.Sum(c => c.Quantity);
            var lineQty = cart.First(c => c.FoodItemId == item.ItemId).Quantity;

            return Json(new { ok = true, count = totalCount, lineQty });
        }

        // Set exact quantity (0 removes) — requires login via Session
        [HttpPost("update")]
        public IActionResult Update([FromBody] AddOrUpdateDto dto)
        {
            // ⛔ Block anonymous by session
            if (!IsLoggedInBySession())
                return UnauthorizedJsonToLogin();

            if (dto == null || dto.ItemId <= 0)
                return BadRequest(new { ok = false });

            var cart = GetCart();
            var line = cart.FirstOrDefault(c => c.FoodItemId == dto.ItemId);
            if (line == null)
                return NotFound(new { ok = false });

            if (dto.Qty <= 0)
                cart.RemoveAll(c => c.FoodItemId == dto.ItemId);
            else
                line.Quantity = dto.Qty;

            cart = cart.Where(c => c.Quantity > 0).ToList();
            SaveCart(cart);

            var totalCount = cart.Sum(c => c.Quantity);
            var lineQty = cart.FirstOrDefault(c => c.FoodItemId == dto.ItemId)?.Quantity ?? 0;

            return Json(new { ok = true, count = totalCount, lineQty });
        }
        [HttpGet("items")]
        public async Task<IActionResult> Items()
        {
            if (!IsLoggedInBySession())
                return RedirectToAction("Login", "User", new { returnUrl = "/cart/items" });

            var cart = GetCart();
            if (cart == null || cart.Count == 0)
                return View(new List<FoodItem>());

            var ids = cart.Select(c => c.FoodItemId).Distinct().ToList();

            var foodItems = await _context.foodItems
                .Where(f => ids.Contains(f.ItemId))
                .ToListAsync();

            ViewBag.CartQty = cart.ToDictionary(c => c.FoodItemId, c => c.Quantity);

            return View(foodItems);
        }

        // ✅ NEW: CHECKOUT PAGE (same style summary + Place Order button later)
        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout()
        {
            if (!IsLoggedInBySession())
                return RedirectToAction("Login", "User", new { returnUrl = "/cart/checkout" });

            var cart = GetCart();
            if (cart == null || cart.Count == 0)
                return RedirectToAction("Items");

            var ids = cart.Select(c => c.FoodItemId).Distinct().ToList();

            var foodItems = await _context.foodItems
                .Where(f => ids.Contains(f.ItemId))
                .ToListAsync();

            ViewBag.CartQty = cart.ToDictionary(c => c.FoodItemId, c => c.Quantity);

            return View(foodItems);
        }

        [HttpPost("placeorder")]
        public async Task<IActionResult> PlaceOrder()
        {
            if (!IsLoggedInBySession())
                return UnauthorizedJsonToLogin();

            var cart = GetCart();
            if (cart == null || cart.Count == 0)
                return BadRequest(new { ok = false, message = "Cart is empty" });

            var email = HttpContext.Session.GetString("loggedinuser");
            if (string.IsNullOrWhiteSpace(email))
                return UnauthorizedJsonToLogin();

            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return UnauthorizedJsonToLogin();

            var ids = cart.Select(c => c.FoodItemId).Distinct().ToList();
            var foodItems = await _context.foodItems.Where(f => ids.Contains(f.ItemId)).ToListAsync();

            if (!foodItems.Any())
                return BadRequest(new { ok = false, message = "No valid items found in DB" });

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = user.UserId,
                    OrderDate = DateTime.Now,
                    OrderAmount = 0m,
                    // Status = "PendingPayment"  // if you have this column
                };

                _context.orders.Add(order);
                await _context.SaveChangesAsync(); // OrderId

                var details = new List<OrderItemDetails>();
                decimal total = 0m;

                foreach (var c in cart)
                {
                    var f = foodItems.FirstOrDefault(x => x.ItemId == c.FoodItemId);
                    if (f == null) continue;
                    var qty = c.Quantity;
                    if (qty <= 0) continue;

                    decimal price = f.SellingPrice > 0 ? f.SellingPrice : f.ActualPrice;
                    decimal line = price * qty;
                    total += line;

                    details.Add(new OrderItemDetails
                    {
                        OrderId = order.OrderId,
                        ItemId = f.ItemId,
                        ItemName = f.ItemName ?? "",
                        Quantity = qty,
                        Price = price,
                        ItemAmount = line
                    });
                }

                if (details.Count == 0)
                    return BadRequest(new { ok = false, message = "No valid cart items to save." });

                _context.orderItemDetails.AddRange(details);
                await _context.SaveChangesAsync();

                order.OrderAmount = total;
                _context.orders.Update(order);
                await _context.SaveChangesAsync();

                await trx.CommitAsync();

                // Redirect to Payment/Start (server redirect)
                return RedirectToAction("Start", "Payment", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                // You can return a view with error or just an error result
                return StatusCode(500, new
                {
                    ok = false,
                    message = "Order failed",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var email = HttpContext.Session.GetString("loggedinuser");
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login", "User", new { returnUrl = $"/Orders/OrderSuccess/{orderId}" });

            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login", "User");

            var order = await _context.orders
                .Include(o => o.orderDetailsItems)
                .Include(o => o.Delivery).ThenInclude(d => d.Employee)
                .Include(o => o.Delivery).ThenInclude(d => d.Address)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

            if (order == null) return NotFound();

            // ----------------------------
            // Lazy-deliver if ExpectedOn <= now and status is still active
            // ----------------------------
            var now = DateTime.Now; // keep local time consistent across the app
            var activeStatuses = new[] { "Assigned", "InTransit", "OutForDelivery" };

            if (order.Delivery != null &&
                order.Delivery.ExpectedOn.HasValue &&
                order.Delivery.ExpectedOn.Value <= now &&
                activeStatuses.Contains(order.Delivery.DeliveryStatus))
            {
                order.Delivery.DeliveryStatus = "Delivered";
                await _context.SaveChangesAsync();
            }

            // ----------------------------
            // Prepare view hints (Delivered? MinutesRemaining?)
            // ----------------------------
            bool isDelivered = order.Delivery != null && order.Delivery.DeliveryStatus == "Delivered";
            double? minutesRemaining = null;
            if (!isDelivered && order.Delivery?.ExpectedOn != null)
            {
                var remaining = order.Delivery.ExpectedOn.Value - now;
                minutesRemaining = Math.Ceiling(remaining.TotalMinutes);
                if (minutesRemaining < 0) minutesRemaining = 0; // just in case
            }

            ViewBag.IsDelivered = isDelivered;
            ViewBag.MinutesRemaining = minutesRemaining;

            // Optional: also pass AssignedOn/ExpectedOn formatted if you want to show in the view
            ViewBag.AssignedOn = order.Delivery?.AssignedOn.ToString("dd-MM-yyyy HH:mm:ss");
            ViewBag.ExpectedOn = order.Delivery?.ExpectedOn?.ToString("dd-MM-yyyy HH:mm:ss");

            return View("OrderSuccess", order); // Views/Orders/OrderSuccess.cshtml
        }


        // DTO for add/update
        public class AddOrUpdateDto
        {
            public int ItemId { get; set; }
            public int Qty { get; set; }
        }
    }
}