using Microsoft.AspNetCore.Authorization;
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
    public class FoodItemsController : Controller
    {
        private readonly OnlineRestaurantDbContext _context;

        public FoodItemsController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        // GET: FoodItems
        /* public async Task<IActionResult> Index()
         {
             var onlineRestaurantDbContext = _context.foodItems.Include(f => f.Category).Include(f => f.ItemType);
             return View(await onlineRestaurantDbContext.ToListAsync());
         }*/
        [AllowAnonymous]
        public async Task<IActionResult> Browse(
            int? categoryId,
            string? search,
            string? sort,
            string? priceSort,
            string? ratingSort,
            string? discountSort,
            string? itemType,
            string? diet,
            int page = 1,
            int pageSize = 12,
            int? clear = null)
                {
                    if (page < 1) page = 1;
                    if (pageSize <= 0 || pageSize > 96) pageSize = 12;

                    // Handle Clear flag (optional block from previous step)
                    if (clear == 1)
                    {
                        sort = null;
                        priceSort = ratingSort = discountSort = null;
                        itemType = null;
                        // search = null; // uncomment if you want to clear search too
                    }

                    if (string.IsNullOrWhiteSpace(itemType) && !string.IsNullOrWhiteSpace(diet))
                        itemType = diet;

                    const int VEG_TYPE_ID = 301;
                    const int NONVEG_TYPE_ID = 302;

                    // Base query: Only available items and ONLY from ACTIVE categories
                    var query = _context.foodItems
                                        .Include(f => f.Category)
                                        .Include(f => f.ItemType)
                                        .AsNoTracking()
                                        .Where(f => f.IsAvailable && f.Category != null && f.Category.CategoryStatus == true);

                    // If a categoryId is provided, ensure the category exists and is Active
                    if (categoryId.HasValue && categoryId.Value > 0)
                    {
                        // Load category + status
                        var cat = await _context.categories
                                                .AsNoTracking()
                                                .Where(c => c.CategoryId == categoryId.Value)
                                                .Select(c => new { c.CategoryId, c.CategoryName, c.CategoryStatus })
                                                .FirstOrDefaultAsync();

                        if (cat == null)
                        {
                            // Category not found – treat as empty results
                            ViewBag.SelectedCategoryId = categoryId.Value;
                            ViewBag.SelectedCategoryName = "(Unknown Category)";
                            ViewBag.NoItemsReason = "Category not found.";
                            return View(Enumerable.Empty<FoodItem>());
                        }

                        ViewBag.SelectedCategoryId = cat.CategoryId;
                        ViewBag.SelectedCategoryName = cat.CategoryName;

                        if (cat.CategoryStatus != true)
                        {
                            ViewBag.SelectedCategoryId = cat.CategoryId;
                            ViewBag.SelectedCategoryName = cat.CategoryName;

                            // Make counts explicit for the view
                            ViewBag.FilteredTotal = 0;
                            ViewBag.OverallCategoryTotal = 0;
                            ViewBag.NoItemsReason = $"No items found. '{cat.CategoryName}' is currently inactive.";

                            return View(Enumerable.Empty<FoodItem>());
                        }

                // Category is active → filter items
                query = query.Where(f => f.CategoryId == cat.CategoryId);
                    }

                    // Text search → match active categories only (base query already ensures active category)
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var s = search.Trim();

                        // If the search exactly matches a category name that is inactive, short-circuit to "no items".
                        var inactiveCatNameMatch = await _context.categories
                            .AsNoTracking()
                            .Where(c => c.CategoryStatus != true && EF.Functions.Like(c.CategoryName, $"%{s}%"))
                            .Select(c => c.CategoryName)
                            .FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(inactiveCatNameMatch))
                        {
                            ViewBag.NoItemsReason = $"No items found. '{inactiveCatNameMatch}' is currently inactive.";
                            ViewBag.FilteredTotal = 0;
                            ViewBag.OverallCategoryTotal = 0;
                            if (categoryId == null) ViewBag.SelectedCategoryName = inactiveCatNameMatch;
                            return View(Enumerable.Empty<FoodItem>());
                        }

                // Otherwise, search across item name and ACTIVE category name
                        query = query.Where(f =>
                                    EF.Functions.Like(f.ItemName, $"%{s}%") ||
                                    (f.Category != null && EF.Functions.Like(f.Category.CategoryName, $"%{s}%"))
                        );

                                ViewBag.Search = s;
                    }

                    // Item Type filter (Veg/Non‑Veg by ItemTypeId)
                    if (!string.IsNullOrWhiteSpace(itemType))
                    {
                        var t = itemType.Trim().ToLowerInvariant();
                        if (t == "veg") query = query.Where(f => f.ItemTypeId == VEG_TYPE_ID);
                        else if (t == "nonveg") query = query.Where(f => f.ItemTypeId == NONVEG_TYPE_ID);
                    }
                    ViewBag.ItemType = itemType;

                    // Map dropdowns → unified sort 
                    if (string.IsNullOrWhiteSpace(sort))
                    {
                        string norm(string? v) => (v ?? "").Trim().ToLowerInvariant();
                        var p = norm(priceSort);
                        var r = norm(ratingSort);
                        var d = norm(discountSort);

                        if (p == "asc") sort = "price_asc";
                        else if (p == "desc") sort = "price_desc";
                        else if (r == "asc") sort = "rating_asc";
                        else if (r == "desc") sort = "rating_desc";
                        else if (d == "asc") sort = "disc_asce";
                        else if (d == "desc") sort = "disc_desc";
                    }

                    ViewBag.Sort = sort;

                    // Sorting
                    query = sort switch
                    {
                        "price_asc" => query.OrderBy(f => f.SellingPrice > 0 ? f.SellingPrice : f.ActualPrice),
                        "price_desc" => query.OrderByDescending(f => f.SellingPrice > 0 ? f.SellingPrice : f.ActualPrice),
                        "rating_asc" => query.OrderBy(f => f.Rating),
                        "rating_desc" => query.OrderByDescending(f => f.Rating),
                        "disc_asce" => query.OrderBy(f => f.DiscountPer),
                        "disc_desc" => query.OrderByDescending(f => f.DiscountPer),
                        _ => query.OrderBy(f => f.ItemName),
                    };

            // ... all your filters above ...
            // ...after sorting (your switch) and before returning the view:

            // Compute totals BEFORE paging (you already have this):
            var filteredTotal = await query.CountAsync();

            int overallCategoryTotal = 0;
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                overallCategoryTotal = await _context.foodItems
                    .AsNoTracking()
                    .Where(f => f.IsAvailable
                                && f.CategoryId == categoryId.Value
                                && f.Category != null
                                && f.Category.CategoryStatus == true)
                    .CountAsync();
            }

            // Expose to view
            ViewBag.FilteredTotal = filteredTotal;
            ViewBag.OverallCategoryTotal = overallCategoryTotal;

            // Apply paging and build PaginatedList<T> **manually** (to avoid a second Count)
            var pageItems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paged = new PaginatedList<FoodItem>(pageItems, filteredTotal, page, pageSize);

            // Default empty message if needed
            if (pageItems.Count == 0 && string.IsNullOrWhiteSpace(ViewBag.NoItemsReason))
            {
                ViewBag.NoItemsReason = "No items found.";
            }

            // Keep these two (the view uses them to compute ranges)
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(paged);
        }


        public async Task<IActionResult> Index(
           string sortOrder,
           string currentFilter,
           string searchString,
           int? pageNumber,
           int? categoryId)
        {
            string loggedInUser = HttpContext.Session.GetString("loggedinuser");
            string loggedinuserRole = HttpContext.Session.GetString("loggedinuserRole");

            if (loggedInUser != null && loggedinuserRole == "Admin")
            {
                ViewBag.loggedInUserId = loggedInUser;

                // Populate category dropdown for the filter
                ViewBag.CategoryList = new SelectList(_context.categories, "CategoryId", "CategoryName");
                ViewData["CurrentSort"] = sortOrder;

                // Read from session if categoryId is null
                if (categoryId == null)
                {
                    categoryId = HttpContext.Session.GetInt32("SelectedCategoryId");
                }
                else
                {
                    // User selected a new category — save in session
                    HttpContext.Session.SetInt32("SelectedCategoryId", categoryId.Value);
                }

                ViewData["CurrentCategory"] = categoryId;
                ViewData["NameSort"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                ViewData["DiscountSort"] = sortOrder == "disc_asce" ? "disc_desc" : "disc_asce";
                // NEW: price sort keys for toggle
                ViewData["PriceSort"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";

                if (searchString != null)
                {
                    pageNumber = 1;
                }
                else
                {
                    searchString = currentFilter;
                }

                var foodItems = _context.foodItems
                                        .Include(f => f.Category)
                                        .Include(f => f.ItemType)
                                        .AsQueryable();

                //  Filter by search text
                if (!string.IsNullOrEmpty(searchString))
                {
                    foodItems = foodItems.Where(i => i.ItemName.Contains(searchString));
                    ViewData["CurrentFilter"] = searchString;
                }

                //  Filter by Category
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    foodItems = foodItems.Where(f => f.CategoryId == categoryId.Value);
                }

                //  Sorting
                switch (sortOrder)
                {
                    case "name_desc":
                        foodItems = foodItems.OrderByDescending(i => i.ItemName);
                        break;

                    case "disc_desc":
                        foodItems = foodItems.OrderByDescending(i => i.DiscountPer);
                        break;

                    case "disc_asce":
                        foodItems = foodItems.OrderBy(i => i.DiscountPer);
                        break;

                    case "price_desc":
                        foodItems = foodItems.OrderByDescending(i =>
                            i.SellingPrice > 0 ? i.SellingPrice : i.ActualPrice);
                        break;

                    case "price_asc":
                        foodItems = foodItems.OrderBy(i =>
                            i.SellingPrice > 0 ? i.SellingPrice : i.ActualPrice);
                        break;

                    default:
                        foodItems = foodItems.OrderBy(i => i.ItemName);
                        break;
                }
                var filteredTotal = await foodItems.CountAsync();                // after filter
                var overallTotal = await _context.foodItems.CountAsync();       // all rows

                ViewData["FilteredTotal"] = filteredTotal;
                ViewData["OverallTotal"] = overallTotal;
                int pageSize = 3;
                return View(await PaginatedList<FoodItem>.CreateAsync(foodItems.AsNoTracking(),
                                                    pageNumber ?? 1, pageSize));
            }

            return RedirectToAction("Login", "User");
        }
        // GET: FoodItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var foodItem = await _context.foodItems
                .Include(f => f.Category)
                .Include(f => f.ItemType)
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (foodItem == null)
            {
                return NotFound();
            }

            return View(foodItem);
        }

        // GET: FoodItems/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.categories, "CategoryId", "CategoryName");
            ViewData["ItemTypeId"] = new SelectList(_context.itemTypes, "ItemTypeId", "ItemTypeName");
            return View();
        }

        // POST: FoodItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,CategoryId,ItemTypeId,ItemName,ActualPrice,Rating,RatingBy,ItemImage,ItemDescription,IsAvailable,IsBestSeller,IsBreakFast,IsLunch,IsSnack,IsDinner,DiscountPer,SellingPrice,IsFastMoving,AvailableType")] FoodItem foodItem)
        {
            if (foodItem.ItemImage == null || foodItem.ItemImage.Length == 0 || foodItem.ItemImage.Length > 524288 || Path.GetExtension(foodItem.ItemImage.FileName) != ".jpg")
            {
                ModelState.AddModelError("ItemImage", "Invalid Image Size");
                return View(foodItem);
            }
            string fileName = foodItem.ItemImage.FileName; //biryani.jpg
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/foodItems/", fileName); //wwwroot/images/categories/biryani.jpg
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await foodItem.ItemImage.CopyToAsync(fs);
                foodItem.ItemImagePath = "/images/foodItems/" + fileName;
            }
            if (ModelState.IsValid)
            {
                _context.Add(foodItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.categories, "CategoryId", "CategoryName", foodItem.CategoryId);
            ViewData["ItemTypeId"] = new SelectList(_context.itemTypes, "ItemTypeId", "ItemTypeName", foodItem.ItemTypeId);
            return View(foodItem);
        }

        // GET: FoodItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var foodItem = await _context.foodItems.FindAsync(id);
            if (foodItem == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.categories, "CategoryId", "CategoryName", foodItem.CategoryId);
            ViewData["ItemTypeId"] = new SelectList(_context.itemTypes, "ItemTypeId", "ItemTypeName", foodItem.ItemTypeId);
            return View(foodItem);
        }
        // POST: FoodItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ItemId,CategoryId,ItemTypeId,ItemName,ActualPrice,Rating,RatingBy,ItemImagePath,ItemImage,ItemDescription,IsAvailable,IsBestSeller,IsBreakFast,IsLunch,IsSnack,IsDinner,DiscountPer,SellingPrice,IsFastMoving")] FoodItem foodItem)
        {
            if (id != foodItem.ItemId)
            {
                return NotFound();
            }

            if (foodItem.ItemImage != null)
            {
                if (foodItem.ItemImage.Length > 524588 || Path.GetExtension(foodItem.ItemImage.FileName) != ".jpg")
                {
                    ModelState.AddModelError("foodItemImage", "Add valid size");
                    return View(foodItem);
                }
                else
                {
                    string fileName = foodItem.ItemImage.FileName;
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/foodItems/", fileName);
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await foodItem.ItemImage.CopyToAsync(fs);
                        foodItem.ItemImagePath = "/images/foodItems/" + fileName;
                    }

                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(foodItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FoodItemExists(foodItem.ItemId))
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
            ViewData["CategoryId"] = new SelectList(_context.categories, "CategoryId", "CategoryName", foodItem.CategoryId);
            ViewData["ItemTypeId"] = new SelectList(_context.itemTypes, "ItemTypeId", "ItemTypeName", foodItem.ItemTypeId);
            return View(foodItem);
        }

        // GET: FoodItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var foodItem = await _context.foodItems
                .Include(f => f.Category)
                .Include(f => f.ItemType)
                .FirstOrDefaultAsync(m => m.ItemId == id);
            if (foodItem == null)
            {
                return NotFound();
            }

            return View(foodItem);
        }

        // POST: FoodItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var foodItem = await _context.foodItems.FindAsync(id);
            if (foodItem != null)
            {
                _context.foodItems.Remove(foodItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FoodItemExists(int id)
        {
            return _context.foodItems.Any(e => e.ItemId == id);
        }
    }
}