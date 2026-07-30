using Microsoft.AspNetCore.Mvc;
using StoreMetrics.Services;
using StoreMetrics.ViewModels;
using System.Threading.Tasks;

namespace StoreMetrics.Controllers
{
    public class ProfileController : Controller
    {
        private readonly MongoDbService _db;

        public ProfileController(MongoDbService db)
        {
            _db = db;
        }

        // Gets the currently logged-in user's ID from the session
        private string? GetCurrentUserId() => HttpContext.Session.GetString("UserId");

        // --- VIEW PROFILE ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "My Profile";
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _db.GetUserByIdAsync(userId);
            if (user == null)
            {
                // User ID in session is invalid, clear it
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            // Map the User model to the ProfileVm
            var vm = new ProfileVm
            {
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Username = user.Username
            };

            return View(vm);
        }

        // --- EDIT PROFILE (GET) ---
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            ViewData["Title"] = "Edit Profile";
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _db.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Map the User model to the EditProfileVm
            var vm = new EditProfileVm
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(vm);
        }

        // --- EDIT PROFILE (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileVm vm)
        {
            ViewData["Title"] = "Edit Profile";
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId) || userId != vm.Id)
            {
                // User is not logged in or is trying to edit someone else's profile
                return BadRequest("Authorization error.");
            }

            // Check if the new email already exists (for a *different* user)
            var existingUserWithEmail = await _db.GetUserByEmailAsync(vm.Email);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != userId)
            {
                ModelState.AddModelError("Email", "This email address is already in use by another account.");
                return View(vm);
            }

            // Call the service to update the user in MongoDB
            await _db.UpdateUserProfileAsync(
                vm.Id,
                vm.FirstName,
                vm.MiddleName,
                vm.LastName,
                vm.Email,
                vm.PhoneNumber
            );

            TempData["ProfileSuccess"] = "Your profile has been updated successfully!";
            return RedirectToAction("Index");
        }
    }
}