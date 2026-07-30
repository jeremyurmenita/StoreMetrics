using Microsoft.AspNetCore.Mvc;
using StoreMetrics.Repositories;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Controllers
{
    public class StoresController : Controller
    {
        private readonly IStoreRepository _repo;

        public StoresController(IStoreRepository repo) => _repo = repo;

        // ---------- LIST ----------
        public async Task<IActionResult> Index()
        {
            var items = await _repo.GetAllAsync();
            return View(items);
        }

        // ---------- CREATE ----------
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(StoreVm vm)
        {
            // ⭐ FIX: enforce required AuditDate
            if (vm.AuditDate == null)
                ModelState.AddModelError("AuditDate", "The Audit Schedule field is required.");

            if (!ModelState.IsValid)
                return View(vm);

            // ⭐ convert nullable to normal date before saving
            vm.AuditDate = vm.AuditDate.Value.Date;

            try
            {
                await _repo.CreateAsync(vm);
                TempData["Ok"] = true;
                return RedirectToAction(nameof(Create));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("StoreName", ex.Message);
                return View(vm);
            }
        }

        // ---------- EDIT ----------
        // EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var item = await _repo.GetAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(StoreVm vm)
        {
            if (vm.AuditDate == null)
                ModelState.AddModelError("AuditDate", "The Audit Schedule field is required.");

            if (!ModelState.IsValid)
                return View(vm);

            vm.AuditDate = vm.AuditDate.Value.Date;

            try
            {
                await _repo.UpdateAsync(vm);
                TempData["EditOk"] = "Store information successfully updated!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("StoreName", ex.Message);
                return View(vm);
            }
        }

        // ---------- TOGGLE STATUS ----------
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            await _repo.ToggleStatusAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
