using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineRestaurantApp.Dal;
using OnlineRestaurantApp.Components;

namespace OnlineRestaurantApp.Controllers
{
    public class SlidersController : Controller
    {
        private readonly OnlineRestaurantDbContext _context;

        public SlidersController(OnlineRestaurantDbContext context)
        {
            _context = context;
        }

        // GET: Sliders
        public async Task<IActionResult> Index()
        {
            return View(await _context.sliders.ToListAsync());
        }

        // GET: Sliders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.sliders
                .FirstOrDefaultAsync(m => m.SliderId == id);
            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        // GET: Sliders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sliders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SliderId,SliderText,SliderTextLink,SliderOrderNo,CreatedOn,Status,SliderImage")] Slider slider)
        {
            if (slider.SliderImage == null || slider.SliderImage.Length == 0 ||
                slider.SliderImage.Length > 1048576 ||
                Path.GetExtension(slider.SliderImage.FileName) != ".jpg")
            {
                ModelState.AddModelError("SliderImage", "Invalid Image");
                return View(slider); // Create.cshtml with slider object
            }

            // image save logic
            string fileName = slider.SliderImage.FileName;
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/sliders", fileName);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await slider.SliderImage.CopyToAsync(fs);
                slider.SliderImagePath = "/images/sliders/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(slider);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(slider);
        }

        // GET: Sliders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }
            return View(slider);
        }

        // POST: Sliders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SliderId,SliderText,SliderTextLink,SliderOrderNo,CreatedOn,Status,SliderImagePath,SliderImage")] Slider slider)
        {
            
            if (id != slider.SliderId)
            {
                return NotFound();
            }

            if (slider.SliderImage != null)
            {
                if (slider.SliderImage.Length > 1048576 || Path.GetExtension(slider.SliderImage.FileName) != ".jpg")
                {
                    ModelState.AddModelError("SliderImage", "Add valid size");
                    return View(slider);
                }
                else
                {
                    string fileName = slider.SliderImage.FileName;
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/sliders/", fileName);
                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await slider.SliderImage.CopyToAsync(fs);
                        slider.SliderImagePath = "/images/sliders/" + fileName;
                    }

                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(slider);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderExists(slider.SliderId))
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
            return View(slider);
        }

        // GET: Sliders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.sliders
                .FirstOrDefaultAsync(m => m.SliderId == id);
            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        // POST: Sliders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var slider = await _context.sliders.FindAsync(id);
            if (slider != null)
            {
                _context.sliders.Remove(slider);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SliderExists(int id)
        {
            return _context.sliders.Any(e => e.SliderId == id);
        }
    }
}
