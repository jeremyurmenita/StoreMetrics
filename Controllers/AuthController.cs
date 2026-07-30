using Microsoft.AspNetCore.Mvc;
using StoreMetrics.Services;
using StoreMetrics.Models;
using StoreMetrics.ViewModels;

namespace StoreMetrics.Controllers
{
    public class AuthController : Controller
    {
        private readonly MongoDbService _db;
        private readonly EmailSender _email;

        public AuthController(MongoDbService db, EmailSender email)
        {
            _db = db;
            _email = email;
        }

        // ---------------- LOGIN ----------------
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _db.GetUserByUsernameAsync(vm.Username);

            if (user == null || user.Password != vm.Password)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(vm);
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserId", user.Id!);

            return RedirectToAction("Index", "Stores");
        }

        // ---------------- REGISTER ----------------
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var emailExists = await _db.GetUserByEmailAsync(vm.Email);
            if (emailExists != null)
            {
                ModelState.AddModelError("Email", "This email is already in use.");
                return View(vm);
            }

            var existing = await _db.GetUserByUsernameAsync(vm.Username);
            if (existing != null)
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(vm);
            }

            var user = new User
            {
                FirstName = vm.FirstName,
                MiddleName = vm.MiddleName,
                LastName = vm.LastName,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                Username = vm.Username,
                Password = vm.Password
            };

            await _db.CreateUserAsync(user);

            ViewBag.RegisterSuccess = true;
            ModelState.Clear();
            return View(new RegisterVm());
        }

        // ---------------- FORGOT PASSWORD ----------------
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string username)
        {
            var user = await _db.GetUserByUsernameAsync(username);
            if (user == null)
            {
                ViewBag.Error = "Username not found.";
                return View();
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Save to session (expires after 10 minutes)
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("OTP_UserId", user.Id!);
            HttpContext.Session.SetString("OTP_Expire", DateTime.UtcNow.AddMinutes(10).ToString());

            // Send email
            await _email.SendEmailAsync(
                user.Email,
                "Your StoreMetrics Password Reset OTP",
                $"<h2>Your OTP Code</h2><p>Your OTP is: <strong>{otp}</strong></p><p>This code will expire in 10 minutes.</p>"
            );

            TempData["Message"] = $"An OTP has been sent to {user.Email}.";
            return RedirectToAction("EnterOtp");
        }

        // ---------------- ENTER OTP ----------------
        [HttpGet]
        public IActionResult EnterOtp() => View();

        [HttpPost]
        public IActionResult EnterOtp(string otp)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var expireTime = HttpContext.Session.GetString("OTP_Expire");

            if (sessionOtp == null || expireTime == null)
            {
                ViewBag.Error = "OTP session expired. Please try again.";
                return View();
            }

            if (DateTime.UtcNow > DateTime.Parse(expireTime))
            {
                ViewBag.Error = "OTP has expired.";
                return View();
            }

            if (otp != sessionOtp)
            {
                ViewBag.Error = "Incorrect OTP.";
                return View();
            }

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public async Task<IActionResult> ResendOtp()
        {
            var userId = HttpContext.Session.GetString("OTP_UserId");

            if (userId == null)
            {
                TempData["Message"] = "Session expired. Please start again.";
                return RedirectToAction("ForgotPassword");
            }

            var user = await _db.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("ForgotPassword");
            }

            // Generate new OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Update session OTP
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("OTP_Expire", DateTime.UtcNow.AddMinutes(10).ToString());

            // Send email
            await _email.SendEmailAsync(
                user.Email,
                "Your StoreMetrics OTP (Resent)",
                $"<h2>Your new OTP Code</h2><p>Your OTP is: <strong>{otp}</strong></p><p>This code will expire in 10 minutes.</p>"
            );

            TempData["Message"] = $"A new OTP has been sent to {user.Email}.";

            return RedirectToAction("EnterOtp");
        }

        // ---------------- RESET PASSWORD ----------------
        [HttpGet]
        public IActionResult ResetPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            // --- Password requirements: must meet strong rules ---
            bool valid = true;

            if (newPassword.Length < 8 ||
                !newPassword.Any(char.IsUpper) ||
                !newPassword.Any(char.IsDigit) ||
                !newPassword.Any(ch => "!@#$%^&*()_-+=[]{}|;:'\",.<>?/`~".Contains(ch)))
            {
                ViewBag.Error = "Password must be at least 8 characters long and include an uppercase letter, number, and symbol.";
                valid = false;
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                valid = false;
            }

            if (!valid)
            {
                return View(); // stay on the same page so error + UI works
            }

            // --- Verify session ID ---
            var userId = HttpContext.Session.GetString("OTP_UserId");
            if (userId == null)
            {
                ViewBag.Error = "Session expired.";
                return View();
            }

            // --- Save password ---
            await _db.UpdateUserPasswordAsync(userId, newPassword);

            // --- Clear session ---
            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("OTP_UserId");
            HttpContext.Session.Remove("OTP_Expire");

            // --- Tell the view to show popup ---
            TempData["ResetSuccess"] = true;

            // Return the view so popup can appear
            return View();
        }

        // ---------------- LOGOUT ----------------
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
