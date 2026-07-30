using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StoreMetrics.Repositories;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Controllers
{
    public class EvaluationsController : Controller
    {
        private readonly IStoreRepository _stores;
        private readonly IEvaluationRepository _evals;
        private readonly MongoEvaluationRepository? _mongoRepo;

        public EvaluationsController(IStoreRepository stores, IEvaluationRepository evals, IServiceProvider provider)
        {
            _stores = stores;
            _evals = evals;
            _mongoRepo = provider.GetService<MongoEvaluationRepository>();
        }

        // --------- CREATE ----------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadStoresAsync();
            return View(new EvaluationVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EvaluationVm vm)
        {
            await LoadStoresAsync();

            var anyRating = new[]
            {
                vm.Cleanliness, vm.Condition, vm.CustomerEngagement,
                vm.PersonalGrooming, vm.Accuracy, vm.SpeedOfService, vm.ProductQuality
            }.Any(v => v >= 1);

            if (!anyRating)
                ModelState.AddModelError("", "Please rate at least one criterion.");

            if (!ModelState.IsValid)
                return View(vm);

            var store = (await _stores.GetAllAsync()).FirstOrDefault(s => s.Id == vm.StoreId);
            vm.StoreName = store?.StoreName ?? "";

            // ✅ Save to file-based repo
            await _evals.CreateAsync(vm);

            // ✅ Also save to MongoDB (ratings + performance)
            if (_mongoRepo != null)
            {
                try
                {
                    await _mongoRepo.CreateAsync(vm);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MongoDB Warning] Could not sync: {ex.Message}");
                }
            }

            TempData["EvalOk"] = true;
            return RedirectToAction(nameof(Create));
        }

        // --------- EDIT ----------
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var vm = await _evals.GetAsync(id);
            if (vm == null) return NotFound();

            await LoadStoresAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EvaluationVm vm)
        {
            await LoadStoresAsync();
            if (string.IsNullOrWhiteSpace(vm.Id)) return BadRequest();

            var store = (await _stores.GetAllAsync()).FirstOrDefault(s => s.Id == vm.StoreId);
            vm.StoreName = store?.StoreName ?? "";

            await _evals.UpdateAsync(vm);

            // Optionally, update Mongo record (for consistency)
            if (_mongoRepo != null)
            {
                try
                {
                    await _mongoRepo.CreateAsync(vm); // re-insert as new snapshot
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MongoDB Warning] Update sync failed: {ex.Message}");
                }
            }

            TempData["EvalSaved"] = true;
            return RedirectToAction("Overview", "Reports", new { storeId = vm.StoreId });
        }

        // --------- Helpers ----------
        private async Task LoadStoresAsync()
        {
            var allStores = await _stores.GetAllAsync();
            var allEvals = await _evals.GetAllAsync();

            // Show only stores NOT yet evaluated
            var evaluatedIds = allEvals.Select(e => e.StoreId).Distinct().ToHashSet();
            var availableStores = allStores.Where(s => !evaluatedIds.Contains(s.Id!)).ToList();

            // ★ Build display text = "Store Name — Full Address"
            var storeOptions = availableStores.Select(s => new
            {
                Value = s.Id,
                Text = $"{s.StoreName} — {s.BuildingNumber} {s.StreetName}, {s.Brgy}, {s.City}, {s.Province} {s.PostalCode}"
            })
            .OrderBy(s => s.Text)
            .ToList();

            ViewBag.StoreOptions = storeOptions;
        }

    }
}
