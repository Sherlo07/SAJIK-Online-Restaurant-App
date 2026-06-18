using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;
using OnlineRestaurantApp.Utility;

namespace OnlineRestaurantApp.Controllers
{
    [Route("payment")]
    public class PaymentController : Controller
    {
        private readonly OnlineRestaurantDbContext _db;

        public PaymentController(OnlineRestaurantDbContext db) => _db = db;

        // -----------------------
        // Helpers
        // -----------------------
        private string? GetEmail() => HttpContext.Session.GetString("loggedinuser");

        private string GetCartKeyForEmail(string email)
            => string.IsNullOrEmpty(email) ? "CART_ANON" : $"CART_{email}";

        // -----------------------
        // GET: /payment/start?orderId=123
        // Renders the payment form (Views/Payment/Start.cshtml)
        // -----------------------
        //[HttpGet("start")]
        //public async Task<IActionResult> Start(int orderId)
        //{
        //    var email = GetEmail();
        //    if (string.IsNullOrWhiteSpace(email))
        //        return RedirectToAction("Login", "User", new { returnUrl = $"/payment/start?orderId={orderId}" });

        //    var user = await _db.users.FirstOrDefaultAsync(u => u.Email == email);
        //    if (user == null)
        //        return RedirectToAction("Login", "User");

        //    var order = await _db.orders
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

        //    if (order == null)
        //    {
        //        TempData["Err"] = "Order not found.";
        //        return RedirectToAction("Items", "Cart");
        //    }

        //    // If already paid, go straight to success page
        //    var alreadyPaid = await _db.payments.AnyAsync(p => p.OrderId == orderId && p.Status);
        //    if (alreadyPaid)
        //        return Redirect($"/payment/success/{orderId}");

        //    ViewData["OrderId"] = order.OrderId;
        //    ViewData["Amount"] = order.OrderAmount;
        //    return View("Start"); // Views/Payment/Start.cshtml
        //}

        [HttpGet("start")]

        public async Task<IActionResult> Start(int orderId)

        {

            var email = GetEmail();

            if (string.IsNullOrWhiteSpace(email))

                return RedirectToAction("Login", "User", new { returnUrl = $"/payment/start?orderId={orderId}" });

            var user = await _db.users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)

                return RedirectToAction("Login", "User");

            var order = await _db.orders

                .AsNoTracking()

                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

            if (order == null)

            {

                TempData["Err"] = "Order not found.";

                return RedirectToAction("Items", "Cart");

            }

            // Prefill user's default delivery address (if exists)

            var addr = await _db.DeliveryAddresses

                .AsNoTracking()

                .FirstOrDefaultAsync(a => a.UserId == user.UserId && a.IsDefault);

            if (addr != null)

            {

                ViewData["Addr1"] = addr.AddressLine1;

                ViewData["Addr2"] = addr.AddressLine2;

                ViewData["City"] = addr.City;

                ViewData["State"] = addr.State;

                ViewData["Pincode"] = addr.Pincode;

                ViewData["Landmark"] = addr.Landmark;

            }

            // If already paid, go straight to success page

            var alreadyPaid = await _db.payments.AnyAsync(p => p.OrderId == orderId && p.Status);

            if (alreadyPaid)

                return Redirect($"/payment/success/{orderId}");

            ViewData["OrderId"] = order.OrderId;

            ViewData["Amount"] = order.OrderAmount;

            return View("Start");

        }



        // -----------------------
        // POST: /payment/pay
        // Validates, ensures order+details, saves PaymentCard + Payment, clears cart, redirects to success
        //// -----------------------
        //[HttpPost("pay")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Pay(
        //    int orderId,
        //    string cardType,      // "Credit" or "Debit"
        //    string cardNumber,    // 16 digits (with or without spaces)
        //    string expiryMonth,   // "01".."12"
        //    string expiryYear,    // "2026"
        //    string cvv)
        //{
        //    var email = GetEmail();
        //    if (string.IsNullOrWhiteSpace(email))
        //        return RedirectToAction("Login", "User", new { returnUrl = $"/payment/start?orderId={orderId}" });

        //    var user = await _db.users.FirstOrDefaultAsync(u => u.Email == email);
        //    if (user == null)
        //        return RedirectToAction("Login", "User");

        //    // -----------------------
        //    // Basic demo validations
        //    // -----------------------
        //    if (string.IsNullOrWhiteSpace(cardType) || !(cardType == "Credit" || cardType == "Debit"))
        //    {
        //        TempData["Err"] = "Select Credit or Debit.";
        //        return RedirectToAction("Start", new { orderId });
        //    }

        //    var normalized = (cardNumber ?? "").Replace(" ", "").Replace("-", "");
        //    if (normalized.Length != 16 || !normalized.All(char.IsDigit))
        //    {
        //        TempData["Err"] = "Enter a valid 16-digit card number.";
        //        return RedirectToAction("Start", new { orderId });
        //    }

        //    if (!int.TryParse(expiryMonth, out var mm) || mm < 1 || mm > 12)
        //    {
        //        TempData["Err"] = "Invalid expiry month.";
        //        return RedirectToAction("Start", new { orderId });
        //    }

        //    if (!int.TryParse(expiryYear, out var yyyy) || yyyy < DateTime.Now.Year || yyyy > DateTime.Now.Year + 20)
        //    {
        //        TempData["Err"] = "Invalid expiry year.";
        //        return RedirectToAction("Start", new { orderId });
        //    }

        //    try
        //    {
        //        var lastDay = DateTime.DaysInMonth(yyyy, mm);
        //        var exp = new DateTime(yyyy, mm, lastDay, 23, 59, 59);
        //        if (exp < DateTime.Now)
        //        {
        //            TempData["Err"] = "Card is expired.";
        //            return RedirectToAction("Start", new { orderId });
        //        }
        //    }
        //    catch
        //    {
        //        TempData["Err"] = "Invalid expiry date.";
        //        return RedirectToAction("Start", new { orderId });
        //    }

        //    if (string.IsNullOrWhiteSpace(cvv) || !(cvv.All(char.IsDigit) && (cvv.Length == 3 || cvv.Length == 4)))
        //    {
        //        TempData["Err"] = "Invalid CVV.";
        //        return RedirectToAction("Start", new { orderId });
        //    }

        //    // parse to model types
        //    if (!long.TryParse(normalized, out var cardNumberLong))
        //    {
        //        TempData["Err"] = "Card number format error.";
        //        return RedirectToAction("Start", new { orderId });
        //    }
        //    if (!int.TryParse(cvv, out var cvvInt))
        //    {
        //        TempData["Err"] = "CVV format error.";
        //        return RedirectToAction("Start", new { orderId });
        //    }
        //    var expiryDo = new DateOnly(yyyy, mm, 1); // month precision

        //    // Idempotency: avoid double charge
        //    var alreadyPaid = await _db.payments.AnyAsync(p => p.OrderId == orderId && p.Status);
        //    if (alreadyPaid)
        //        return Redirect($"/payment/success/{orderId}");

        //    using var tx = await _db.Database.BeginTransactionAsync();
        //    try
        //    {
        //        // -----------------------
        //        // Ensure Order + Details exist (reuse PlaceOrder result; fallback to Session cart if missing)
        //        // -----------------------
        //        var order = await _db.orders
        //            .Include(o => o.orderDetailsItems) // <-- matches your Order model
        //            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

        //        if (order == null || order.orderDetailsItems == null || order.orderDetailsItems.Count == 0)
        //        {
        //            // Build from Session cart (fallback path)
        //            var cartKey = GetCartKeyForEmail(email);
        //            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey) ?? new List<CartItem>();
        //            if (cart.Count == 0)
        //            {
        //                await tx.RollbackAsync();
        //                TempData["Err"] = "Cart is empty. Cannot create order.";
        //                return RedirectToAction("Items", "Cart");
        //            }

        //            if (order == null)
        //            {
        //                order = new Order
        //                {
        //                    UserId = user.UserId,
        //                    OrderDate = DateTime.Now,
        //                    OrderAmount = 0m,
        //                    orderDetailsItems = new List<OrderItemDetails>()
        //                };
        //                _db.orders.Add(order);
        //                await _db.SaveChangesAsync(); // generates OrderId
        //                orderId = order.OrderId;
        //            }
        //            else
        //            {
        //                order.orderDetailsItems = new List<OrderItemDetails>();
        //            }

        //            // Load items from DB to compute correct prices
        //            var ids = cart.Select(c => c.FoodItemId).Distinct().ToList();
        //            var foodItems = await _db.foodItems
        //                .Where(f => ids.Contains(f.ItemId))
        //                .ToListAsync();

        //            decimal total = 0m;
        //            foreach (var c in cart)
        //            {
        //                var f = foodItems.FirstOrDefault(x => x.ItemId == c.FoodItemId);
        //                if (f == null || c.Quantity <= 0) continue;

        //                decimal unit = f.SellingPrice > 0 ? f.SellingPrice : f.ActualPrice;
        //                decimal line = unit * c.Quantity;
        //                total += line;

        //                order.orderDetailsItems.Add(new OrderItemDetails
        //                {
        //                    OrderId = order.OrderId,
        //                    ItemId = f.ItemId,
        //                    ItemName = f.ItemName ?? "",
        //                    Quantity = c.Quantity,
        //                    Price = unit,
        //                    ItemAmount = line
        //                });
        //            }

        //            if (order.orderDetailsItems.Count == 0)
        //            {
        //                await tx.RollbackAsync();
        //                TempData["Err"] = "No valid items to create order.";
        //                return RedirectToAction("Items", "Cart");
        //            }

        //            _db.orderItemDetails.AddRange(order.orderDetailsItems);
        //            await _db.SaveChangesAsync();

        //            order.OrderAmount = total;
        //            _db.orders.Update(order);
        //            await _db.SaveChangesAsync();
        //        }
        //        else
        //        {
        //            // Recompute total to ensure consistency
        //            var recomputedTotal = order.orderDetailsItems.Sum(d => d.ItemAmount);
        //            if (order.OrderAmount != recomputedTotal)
        //            {
        //                order.OrderAmount = recomputedTotal;
        //                _db.orders.Update(order);
        //                await _db.SaveChangesAsync();
        //            }
        //        }

        //        // -----------------------
        //        // Upsert PaymentCard (with non-null CVV)
        //        // -----------------------
        //        var existingCard = await _db.paymentCards
        //            .FirstOrDefaultAsync(pc => pc.UserId == user.UserId
        //                                    && pc.CardNumber == cardNumberLong
        //                                    && pc.ExpiryDate == expiryDo);

        //        if (existingCard == null)
        //        {
        //            var pc = new PaymentCard
        //            {
        //                UserId = user.UserId,
        //                CardNumber = cardNumberLong,               // long (keep consistent with model)
        //                CardHolderName = user.UserFirstName,       // or null
        //                ExpiryDate = expiryDo,                     // DateOnly (converter required in DbContext)
        //                CVV = cvvInt,                              // ✅ non-null
        //                Status = true
        //            };
        //            _db.paymentCards.Add(pc);
        //            await _db.SaveChangesAsync();
        //        }
        //        else
        //        {
        //            // Refresh CVV/expiry/status on reuse
        //            existingCard.CVV = cvvInt;                    // ✅ non-null
        //            existingCard.ExpiryDate = expiryDo;
        //            existingCard.Status = true;
        //            _db.paymentCards.Update(existingCard);
        //            await _db.SaveChangesAsync();
        //        }

        //        // -----------------------
        //        // Save Payment
        //        // -----------------------
        //        int paymentTypeId = (cardType == "Credit") ? 2 : 1; // 1=Debit, 2=Credit (ensure rows exist)
        //        var payment = new Payment
        //        {
        //            OrderId = order.OrderId,
        //            UserId = user.UserId,
        //            PaymentTypeId = paymentTypeId,
        //            PaidOnDate = DateTime.Now,
        //            OrderAmount = order.OrderAmount,
        //            Status = true
        //        };
        //        _db.payments.Add(payment);
        //        await _db.SaveChangesAsync();

        //        // Commit all DB work
        //        await tx.CommitAsync();

        //        // Clear cart AFTER commit
        //        HttpContext.Session.SetObject(GetCartKeyForEmail(email), new List<CartItem>());

        //        // Force attribute-route redirect to success
        //        return Redirect($"/payment/success/{order.OrderId}");
        //    }
        //    catch (Exception ex)
        //    {
        //        await tx.RollbackAsync();
        //        // Surface the real error while testing
        //        TempData["Err"] = ex.InnerException?.Message ?? ex.Message;
        //        return RedirectToAction("Start", new { orderId });
        //    }
        //}
        [HttpPost("pay")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(
    int orderId,
    string cardType,
    string cardNumber,
    string expiryMonth,
    string expiryYear,
    string cvv,
    string addressLine1,
    string? addressLine2,
    string city,
    string state,
    string pincode,
    string? landmark)
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login", "User", new { returnUrl = $"/payment/start?orderId={orderId}" });

            var user = await _db.users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login", "User");

            // -----------------------
            // CARD VALIDATIONS
            // -----------------------
            if (string.IsNullOrWhiteSpace(cardType) || !(cardType == "Credit" || cardType == "Debit"))
            {
                TempData["Err"] = "Select Credit or Debit.";
                return RedirectToAction("Start", new { orderId });
            }

            var normalized = (cardNumber ?? "").Replace(" ", "").Replace("-", "");
            if (normalized.Length != 16 || !normalized.All(char.IsDigit))
            {
                TempData["Err"] = "Enter a valid 16-digit card number.";
                return RedirectToAction("Start", new { orderId });
            }

            if (!int.TryParse(expiryMonth, out var mm) || mm < 1 || mm > 12)
            {
                TempData["Err"] = "Invalid expiry month.";
                return RedirectToAction("Start", new { orderId });
            }

            if (!int.TryParse(expiryYear, out var yyyy) || yyyy < DateTime.Now.Year || yyyy > DateTime.Now.Year + 20)
            {
                TempData["Err"] = "Invalid expiry year.";
                return RedirectToAction("Start", new { orderId });
            }

            try
            {
                var lastDay = DateTime.DaysInMonth(yyyy, mm);
                var exp = new DateTime(yyyy, mm, lastDay, 23, 59, 59);
                if (exp < DateTime.Now)
                {
                    TempData["Err"] = "Card is expired.";
                    return RedirectToAction("Start", new { orderId });
                }
            }
            catch
            {
                TempData["Err"] = "Invalid expiry date.";
                return RedirectToAction("Start", new { orderId });
            }

            if (string.IsNullOrWhiteSpace(cvv) || !(cvv.All(char.IsDigit) && (cvv.Length == 3 || cvv.Length == 4)))
            {
                TempData["Err"] = "Invalid CVV.";
                return RedirectToAction("Start", new { orderId });
            }

            if (!long.TryParse(normalized, out var cardNumberLong))
            {
                TempData["Err"] = "Card number format error.";
                return RedirectToAction("Start", new { orderId });
            }

            if (!int.TryParse(cvv, out var cvvInt))
            {
                TempData["Err"] = "CVV format error.";
                return RedirectToAction("Start", new { orderId });
            }

            var expiryDo = new DateOnly(yyyy, mm, 1);

            // -----------------------
            // ADDRESS VALIDATIONS (do BEFORE transaction work)
            // -----------------------
            if (string.IsNullOrWhiteSpace(addressLine1) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(state) ||
                string.IsNullOrWhiteSpace(pincode))
            {
                TempData["Err"] = "Please enter delivery address (Address, City, State, Pincode).";
                return RedirectToAction("Start", new { orderId });
            }

            var digitsOnlyPincode = new string((pincode ?? "").Where(char.IsDigit).ToArray());
            if (digitsOnlyPincode.Length < 5 || digitsOnlyPincode.Length > 10)
            {
                TempData["Err"] = "Please enter a valid pincode.";
                return RedirectToAction("Start", new { orderId });
            }

            // -----------------------
            // Idempotency: avoid double charge
            // -----------------------
            var alreadyPaid = await _db.payments.AnyAsync(p => p.OrderId == orderId && p.Status);
            if (alreadyPaid)
                return RedirectToAction("Success", "Payment", new { id = orderId });

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // -----------------------
                // Ensure Order + Details exist
                // -----------------------
                var order = await _db.orders
                    .Include(o => o.orderDetailsItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

                if (order == null || order.orderDetailsItems == null || order.orderDetailsItems.Count == 0)
                {
                    // Fallback: build from Session cart
                    var cartKey = GetCartKeyForEmail(email);
                    var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey) ?? new List<CartItem>();

                    if (cart.Count == 0)
                    {
                        await tx.RollbackAsync();
                        TempData["Err"] = "Cart is empty. Cannot create order.";
                        return RedirectToAction("Items", "Cart");
                    }

                    if (order == null)
                    {
                        order = new Order
                        {
                            UserId = user.UserId,
                            OrderDate = DateTime.Now,
                            OrderAmount = 0m,
                            IsAssigned = false, // <-- explicit on creation
                            orderDetailsItems = new List<OrderItemDetails>()
                        };
                        _db.orders.Add(order);
                        await _db.SaveChangesAsync(); // creates OrderId
                        orderId = order.OrderId;
                    }
                    else
                    {
                        order.orderDetailsItems = new List<OrderItemDetails>();
                        order.IsAssigned = false; // ensure unassigned
                    }

                    // Load items from DB for correct prices
                    var ids = cart.Select(c => c.FoodItemId).Distinct().ToList();
                    var foodItems = await _db.foodItems.Where(f => ids.Contains(f.ItemId)).ToListAsync();

                    decimal total = 0m;
                    foreach (var c in cart)
                    {
                        var f = foodItems.FirstOrDefault(x => x.ItemId == c.FoodItemId);
                        if (f == null || c.Quantity <= 0) continue;

                        decimal unit = f.SellingPrice > 0 ? f.SellingPrice : f.ActualPrice;
                        decimal line = unit * c.Quantity;
                        total += line;

                        order.orderDetailsItems.Add(new OrderItemDetails
                        {
                            OrderId = order.OrderId,
                            ItemId = f.ItemId,
                            ItemName = f.ItemName ?? "",
                            Quantity = c.Quantity,
                            Price = unit,
                            ItemAmount = line
                        });
                    }

                    if (order.orderDetailsItems.Count == 0)
                    {
                        await tx.RollbackAsync();
                        TempData["Err"] = "No valid items to create order.";
                        return RedirectToAction("Items", "Cart");
                    }

                    _db.orderItemDetails.AddRange(order.orderDetailsItems);
                    await _db.SaveChangesAsync();

                    order.OrderAmount = total;
                    _db.orders.Update(order);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // Recompute total to ensure consistency
                    var recomputedTotal = order.orderDetailsItems.Sum(d => d.ItemAmount);
                    if (order.OrderAmount != recomputedTotal)
                    {
                        order.OrderAmount = recomputedTotal;
                        _db.orders.Update(order);
                        await _db.SaveChangesAsync();
                    }

                    // ensure it's unassigned at pay time
                    if (order.IsAssigned != false)
                    {
                        order.IsAssigned = false;
                        _db.orders.Update(order);
                        await _db.SaveChangesAsync();
                    }
                }

                // -----------------------
                // Upsert PaymentCard
                // -----------------------
                var existingCard = await _db.paymentCards
                    .FirstOrDefaultAsync(pc =>
                        pc.UserId == user.UserId &&
                        pc.CardNumber == cardNumberLong &&
                        pc.ExpiryDate == expiryDo);

                if (existingCard == null)
                {
                    var pc = new PaymentCard
                    {
                        UserId = user.UserId,
                        CardNumber = cardNumberLong,
                        CardHolderName = user.UserFirstName,
                        ExpiryDate = expiryDo,
                        CVV = cvvInt,
                        Status = true
                    };
                    _db.paymentCards.Add(pc);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    existingCard.CVV = cvvInt;
                    existingCard.ExpiryDate = expiryDo;
                    existingCard.Status = true;
                    _db.paymentCards.Update(existingCard);
                    await _db.SaveChangesAsync();
                }

                // -----------------------
                // Save/Update Default Delivery Address (BEFORE any delivery logic)
                // -----------------------
                await SaveOrUpdateDefaultAddressAsync(
                    user.UserId,
                    addressLine1,
                    addressLine2,
                    city,
                    state,
                    digitsOnlyPincode,
                    landmark);

                // -----------------------
                // Save Payment
                // -----------------------
                int paymentTypeId = (cardType == "Credit") ? 2 : 1;

                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    UserId = user.UserId,
                    PaymentTypeId = paymentTypeId,
                    PaidOnDate = DateTime.Now,
                    OrderAmount = order.OrderAmount,
                    Status = true
                };
                _db.payments.Add(payment);
                await _db.SaveChangesAsync();

                // -----------------------
                // IMPORTANT: Do NOT auto-assign delivery here.
                // Ensure order remains unassigned at payment time.
                // -----------------------
                order.IsAssigned = false; // explicitly set
                _db.orders.Update(order);
                await _db.SaveChangesAsync();

                // Commit all DB work
                await tx.CommitAsync();

                // Clear cart AFTER commit
                HttpContext.Session.SetObject(GetCartKeyForEmail(email), new List<CartItem>());

                return RedirectToAction("Success", "Payment", new { id = orderId });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Err"] = ex.InnerException?.Message ?? ex.Message;
                return RedirectToAction("Start", new { orderId });
            }
        }


        // -----------------------
        // GET: /payment/success/{orderId}
        // Shows the order success page with items (Views/Payment/OrderSuccess.cshtml)
        // -----------------------
        // PaymentController.cs

        [HttpGet("success/{orderId:int}")]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var email = GetEmail();
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login", "User", new { returnUrl = $"/payment/success/{orderId}" });

            var user = await _db.users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login", "User");

            // Load order with details and delivery
            var order = await _db.orders
                .Include(o => o.orderDetailsItems)
                .Include(o => o.Delivery).ThenInclude(d => d.Employee)
                .Include(o => o.Delivery).ThenInclude(d => d.Address)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

            if (order == null) return NotFound();

            // ❗ Check if payment exists and is successful
            var isPaid = await _db.payments.AnyAsync(p => p.OrderId == orderId && p.Status);

            // If not paid, DO NOT show green success; show pending/failed UI
            ViewBag.Paid = isPaid;

            // For delivery status helpers (only meaningful if paid)
            var now = DateTime.Now;
            var activeStatuses = new[] { "Assigned", "InTransit", "OutForDelivery" };

            if (isPaid && order.Delivery != null &&
                order.Delivery.ExpectedOn.HasValue &&
                order.Delivery.ExpectedOn.Value <= now &&
                activeStatuses.Contains(order.Delivery.DeliveryStatus))
            {
                order.Delivery.DeliveryStatus = "Delivered";
                await _db.SaveChangesAsync();
            }

            bool isDelivered = isPaid && order.Delivery != null && order.Delivery.DeliveryStatus == "Delivered";
            double? minutesRemaining = null;
            if (isPaid && !isDelivered && order.Delivery?.ExpectedOn != null)
            {
                var remaining = order.Delivery.ExpectedOn.Value - now;
                minutesRemaining = Math.Ceiling(remaining.TotalMinutes);
                if (minutesRemaining < 0) minutesRemaining = 0;
            }

            ViewBag.IsDelivered = isDelivered;
            ViewBag.MinutesRemaining = minutesRemaining;
            ViewBag.AssignedOn = order.Delivery?.AssignedOn.ToString("dd-MM-yyyy HH:mm:ss");
            ViewBag.ExpectedOn = order.Delivery?.ExpectedOn?.ToString("dd-MM-yyyy HH:mm:ss");

            return View("OrderSuccess", order);
        }

        private async Task SaveOrUpdateDefaultAddressAsync(

                            int userId,

                            string addressLine1,

                            string? addressLine2,

                            string city,

                            string state,

                            string pincode,

                            string? landmark)

            {

            var existingDefault = await _db.DeliveryAddresses

                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

            if (existingDefault == null)

            {

                // Make sure no other address is marked default

                var all = await _db.DeliveryAddresses.Where(a => a.UserId == userId).ToListAsync();

                foreach (var a in all) a.IsDefault = false;

                var newAddr = new DeliveryAddress

                {

                    UserId = userId,

                    AddressLine1 = addressLine1.Trim(),

                    AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim(),

                    City = city.Trim(),

                    State = state.Trim(),

                    Pincode = pincode.Trim(),

                    Landmark = string.IsNullOrWhiteSpace(landmark) ? null : landmark.Trim(),

                    IsDefault = true,

                    CreatedOn = DateTime.UtcNow

                };

                _db.DeliveryAddresses.Add(newAddr);

                await _db.SaveChangesAsync();

            }

            else

            {

                existingDefault.AddressLine1 = addressLine1.Trim();

                existingDefault.AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();

                existingDefault.City = city.Trim();

                existingDefault.State = state.Trim();

                existingDefault.Pincode = pincode.Trim();

                existingDefault.Landmark = string.IsNullOrWhiteSpace(landmark) ? null : landmark.Trim();

                existingDefault.IsDefault = true;

                _db.DeliveryAddresses.Update(existingDefault);

                await _db.SaveChangesAsync();

            }

        }

        // -----------------------

        // Assign delivery partner dynamically

        // -----------------------

        private async Task<Delivery> AssignDeliveryAsync(int orderId, int userId)
        {
            // If already assigned, return it
            var existing = await _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId);
            if (existing != null) return existing;

            // 1) Load order (you were missing this earlier)
            var order = await _db.orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                throw new Exception($"Order {orderId} not found.");

            // 2) Load default address
            var address = await _db.DeliveryAddresses
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
            if (address == null)
                throw new Exception("Delivery address not found. Please add a delivery address.");

            // 3) Pick partner (same logic you had)
            var partner = await _db.Employees
                .Where(e => e.IsActive && e.Role == "Delivery")
                .Select(e => new
                {
                    Emp = e,
                    ActiveCount = _db.Deliveries.Count(d =>
                        d.EmployeeId == e.EmployeeId &&
                        d.DeliveryStatus != "Delivered" &&
                        d.DeliveryStatus != "Cancelled")
                })
                .OrderBy(x => x.ActiveCount)
                .ThenBy(x => x.Emp.EmployeeId)
                .Select(x => x.Emp)
                .FirstOrDefaultAsync();

            if (partner == null)
                throw new Exception("No delivery partners available right now.");

            // 4) Time calculation (LOCAL time consistently)
            var now = DateTime.Now;
            var expected = order.OrderDate.AddMinutes(30);

            // Optional: if the 30-min window is already past, provide a small grace buffer
            if (expected < now)
                expected = now.AddMinutes(10);

            // 5) Create Delivery
            var delivery = new Delivery
            {
                OrderId = orderId,
                EmployeeId = partner.EmployeeId,

                // ⚠️ Use the correct PK property for your DeliveryAddress:
                // If your model's PK is DeliveryAddressId:
                // AddressId = address.DeliveryAddressId,
                // If your model's PK is AddressId:
                // AddressId = address.AddressId,
                AddressId = address.AddressId, // <-- change if your PK is AddressId
                AssignedOn = now,
                ExpectedOn = expected,
                DeliveryStatus = "Assigned"
            };

            _db.Deliveries.Add(delivery);
            await _db.SaveChangesAsync();

            // (Optional) Mark the order as assigned if you're still using IsAssigned
            var trackedOrder = await _db.orders.FindAsync(orderId);
            if (trackedOrder != null && trackedOrder.IsAssigned != true)
            {
                trackedOrder.IsAssigned = true;
                _db.orders.Update(trackedOrder);
                await _db.SaveChangesAsync();
            }

            return delivery;
        }




        public async Task<IActionResult> Success(int id)
        {
            var order = await _db.orders
                .AsNoTracking()
                .Include(o => o.payment)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            ViewBag.OrderId = order.OrderId;
            ViewBag.Amount = order.OrderAmount;
            ViewBag.Paid = order.payment?.Status == true; // ✅

            return View("Success");
        }

    }

}
