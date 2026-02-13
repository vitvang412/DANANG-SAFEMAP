// TODO: Implement AccountController
using DaNangSafeMap.Models;
using DaNangSafeMap.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using DaNangSafeMap.Data;

namespace DaNangSafeMap.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;

        public AccountController(IAuthService authService, ApplicationDbContext context,
            IEmailService emailService, IOtpService otpService)
        {
            _authService = authService;
            _context = context;
            _emailService = emailService;
            _otpService = otpService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.ValidateUserAsync(model.Email, model.Password);
            if (user != null)
            {
                await SignInUserAsync(user, model.RememberMe);
                return RedirectToAction("Index", "Home");
            }

            // Check if user exists but password is wrong
            var userExists = await _authService.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                ModelState.AddModelError("", "Sai mật khẩu. Vui lòng thử lại.");
            }
            else
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
            }
            return View(model);
        }

        // ===== GOOGLE LOGIN (for existing users) =====
        [HttpGet]
        public async Task<IActionResult> GoogleLogin()
        {
            // Force logout to prevent session conflicts
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleLoginResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleLoginResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("Login");

            var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin từ Google. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            // Check if user exists by Email - ALLOW LOGIN
            var user = await _authService.FindByEmailAsync(email);
            if (user != null)
            {
                // User exists - LOGIN them
                await SignInUserAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            // User does not exist - redirect to register
            TempData["ErrorMessage"] = "Tài khoản chưa được đăng ký. Vui lòng đăng ký trước.";
            TempData["OAuthComplete"] = true;
            return RedirectToAction("Register");
        }

        // ===== GOOGLE REGISTER (for new users) =====
        [HttpGet]
        public async Task<IActionResult> GoogleRegister()
        {
            // Force logout to prevent session conflicts
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleRegisterResponse") };
            properties.Items["prompt"] = "select_account";
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleRegisterResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("Register");

            // Ensure no existing cookie auth session interferes
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin từ Google. Vui lòng thử lại.";
                TempData["OAuthComplete"] = true;
                return RedirectToAction("Register");
            }

            // Check if user exists by Email - BLOCK REGISTRATION
            var user = await _authService.FindByEmailAsync(email);
            if (user != null)
            {
                TempData["ErrorMessage"] = "Email này đã tồn tại. Vui lòng đăng nhập hoặc chọn tài khoản Google khác.";
                TempData["OAuthComplete"] = true;
                return RedirectToAction("Register");
            }

            // Check if email already exists (registered without Google)
            var existingUser = await _authService.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Email này đã được đăng ký bằng phương thức khác. Vui lòng đăng ký bằng tài khoản Google khác hoặc đăng nhập bằng email/mật khẩu.";
                TempData["OAuthComplete"] = true;
                return RedirectToAction("Register");
            }

            // Store Google info in TempData and redirect to CompleteProfile
            // This ensures antiforgery token is generated AFTER sign-out completes
            TempData["GoogleId"] = googleId;
            TempData["GoogleEmail"] = email;
            TempData["GoogleName"] = name ?? "";
            TempData["OAuthComplete"] = true;
            return RedirectToAction("CompleteProfile");
        }

        [HttpGet]
        public IActionResult CompleteProfile()
        {
            // Retrieve Google info from TempData
            var googleId = TempData["GoogleId"]?.ToString();
            var email = TempData["GoogleEmail"]?.ToString();
            var name = TempData["GoogleName"]?.ToString();

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Phiên đăng ký đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("Register");
            }

            var model = new RegisterWithGoogleViewModel
            {
                GoogleId = googleId,
                Email = email,
                FullName = name ?? ""
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteGoogleRegistration(RegisterWithGoogleViewModel model)
        {
            if (!ModelState.IsValid) return View("CompleteProfile", model);

            var result = await _authService.RegisterGoogleUserAsync(model);
            if (result.Success)
            {
                // Ensure user is signed out before redirecting to login
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", result.Message);
            return View("CompleteProfile", model);
        }

        // ===== METHOD 1B: REGISTER WITH EMAIL OTP (no Google) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterWithEmail(RegisterStandardViewModel model)
        {
            if (!ModelState.IsValid) return View("Register", model);

            // Check if email already exists
            var existingUser = await _authService.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View("Register", model);
            }

            // Generate and send OTP
            var otp = _otpService.GenerateOtp();
            _otpService.StoreOtp(model.Email, otp, model);

            var emailSent = await _emailService.SendOtpAsync(model.Email, otp);
            if (!emailSent)
            {
                TempData["ErrorMessage"] = "Không thể gửi email. Vui lòng thử lại.";
                return View("Register", model);
            }

            TempData["SuccessMessage"] = "Mã xác nhận đã được gửi đến email của bạn!";
            return RedirectToAction("VerifyEmail", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            return View(new VerifyEmailViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate OTP
            if (!_otpService.ValidateOtp(model.Email, model.Otp))
            {
                TempData["ErrorMessage"] = "Mã xác nhận không đúng hoặc đã hết hạn.";
                return View(model);
            }

            // Get registration data
            var registrationData = _otpService.GetRegistrationData(model.Email);
            if (registrationData == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại.";
                return RedirectToAction("Register");
            }

            // Create user
            var result = await _authService.RegisterStandardUserAsync(registrationData);
            if (result.Success)
            {
                _otpService.ClearOtp(model.Email);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = result.Message;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            var registrationData = _otpService.GetRegistrationData(email);
            if (registrationData == null)
            {
                TempData["ErrorMessage"] = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại.";
                return RedirectToAction("Register");
            }

            // Generate new OTP
            var otp = _otpService.GenerateOtp();
            _otpService.StoreOtp(email, otp, registrationData);

            var emailSent = await _emailService.SendOtpAsync(email, otp);
            if (emailSent)
            {
                TempData["SuccessMessage"] = "Mã xác nhận mới đã được gửi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể gửi email. Vui lòng thử lại.";
            }

            return RedirectToAction("VerifyEmail", new { email });
        }

        // Method 2: Standard fill first, then Link
        [HttpGet]
        public async Task<IActionResult> Register(bool fresh = false)
        {
            // Force logout to prevent stale session issues
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // Redirect to self with fresh=true to ensure antiforgery token is generated correctly
                return RedirectToAction("Register", new { fresh = true });
            }
            return View(new RegisterStandardViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.UserProfile == null)
            {
                return RedirectToAction("Profile");
            }

            var model = new EditProfileViewModel
            {
                FullName = user.UserProfile.FullName,
                PhoneNumber = user.UserProfile.PhoneNumber,
                Address = user.UserProfile.Address,
                DateOfBirth = user.UserProfile.DateOfBirth ?? DateTime.Now.AddYears(-18),
                Gender = user.UserProfile.Gender
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.UserProfile == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Profile");
            }

            user.UserProfile.FullName = model.FullName;
            user.UserProfile.PhoneNumber = model.PhoneNumber;
            user.UserProfile.Address = model.Address;
            user.UserProfile.DateOfBirth = model.DateOfBirth;
            user.UserProfile.Gender = model.Gender;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Verify current password
            var hashedCurrentPassword = _authService.HashPassword(model.CurrentPassword, user.Salt);
            if (hashedCurrentPassword != user.Password_Hash)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            // Update password
            user.Password_Hash = _authService.HashPassword(model.NewPassword, user.Salt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult DeleteAccount()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["ErrorMessage"] = "Không thể xác định tài khoản.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _authService.DeleteUserAsync(userId);
            if (result)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["SuccessMessage"] = "Tài khoản đã được xóa thành công.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Không thể xóa tài khoản. Vui lòng thử lại.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}