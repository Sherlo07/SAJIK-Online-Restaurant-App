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
    public class EmployeesController : Controller
    {

        private readonly OnlineRestaurantDbContext _context;
        private const int PageSize = 10;

        public EmployeesController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index(
            string? sortOrder,
            string? currentFilter,
            string? searchString,
            string? activeOnly, // "true" | "false" | null
            int? pageNumber)
        {
            // Sort params for headings
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["RoleSort"] = sortOrder == "role" ? "role_desc" : "role";
            ViewData["StatusSort"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["CreatedSort"] = sortOrder == "created" ? "created_desc" : "created";

            // Search persistence
            if (searchString != null)
                pageNumber = 1;
            else
                searchString = currentFilter;

            ViewData["CurrentFilter"] = searchString;
            ViewData["ActiveOnly"] = activeOnly;

            // Base query
            var query = _context.Employees.AsNoTracking().AsQueryable();

            // Filter: ActiveOnly
            if (!string.IsNullOrWhiteSpace(activeOnly))
            {
                if (bool.TryParse(activeOnly, out var activeFlag))
                {
                    query = query.Where(e => e.IsActive == activeFlag);
                }
            }

            // Filter: Search (name/email/phone)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                query = query.Where(e =>
                    (e.FullName != null && e.FullName.Contains(term)) ||
                    (e.Email != null && e.Email.Contains(term)) ||
                    (e.Phone != null && e.Phone.Contains(term))
                );
            }

            // Tally counts for header
            var overallTotal = await _context.Employees.CountAsync();
            var filteredTotal = await query.CountAsync();
            ViewData["OverallTotal"] = overallTotal;
            ViewData["FilteredTotal"] = filteredTotal;

            // Sorting
            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(e => e.FullName),
                "role" => query.OrderBy(e => e.Role),
                "role_desc" => query.OrderByDescending(e => e.Role),
                "status" => query.OrderBy(e => e.IsActive),
                "status_desc" => query.OrderByDescending(e => e.IsActive),
                "created" => query.OrderBy(e => e.CreatedOn),
                "created_desc" => query.OrderByDescending(e => e.CreatedOn),
                _ => query.OrderBy(e => e.FullName)
            };

            // Paging
            var pageIndex = pageNumber ?? 1;
            var paged = await PaginatedList<Employee>.CreateAsync(query, pageIndex, PageSize);

            return View(paged);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,FullName,Email,Phone,Role,IsActive,CreatedOn")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,FullName,Email,Phone,Role,IsActive,CreatedOn")] Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
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
            return View(employee);
        }
        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return RedirectToAction(nameof(Index));

            // Soft delete: deactivate instead of physical delete
            employee.IsActive = false;
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            TempData["Info"] = $"Employee '{employee.FullName}' has been deactivated.";
            return RedirectToAction(nameof(Index));
        }


        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}
