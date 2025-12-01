using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Services;
using System.Security.Claims;

namespace OfficeNexus.Controllers
{
    public class AuthController : Controller
    {
        private readonly OfficeDbContext _context;
        private readonly IRateLimitService _rateLimitService;

        public AuthController(OfficeDbContext context, IRateLimitService rateLimitService)
        {
            _context = context;
            _rateLimitService = rateLimitService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToRoleDashboard();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? securityCode)
        {
            // First, find the user by email (case-insensitive)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            // If user exists, check rate limiting using their current email
            // This ensures rate limiting works correctly even after email changes
            if (user != null)
            {
                // ✅ SECURITY UPGRADE 1: Rate Limiting (Brute Force Protection)
                // Check using the user's current email in database (handles email changes)
                if (await _rateLimitService.IsAccountLockedAsync(user.Email))
                {
                    var failedCount = await _rateLimitService.GetFailedAttemptsCountAsync(user.Email);
                    var timeRemaining = await _rateLimitService.GetLockoutTimeRemainingAsync(user.Email);
                    
                    ViewBag.Error = $"Account temporarily locked due to {failedCount} failed login attempts. " +
                                   $"Please try again in {timeRemaining?.Minutes ?? 15} minutes.";
                    ViewBag.Email = email; // Preserve the email they entered
                    return View();
                }
            }
            else
            {
                // User doesn't exist - check rate limiting for the entered email
                // This prevents brute force attacks on non-existent emails
                if (await _rateLimitService.IsAccountLockedAsync(email))
                {
                    var failedCount = await _rateLimitService.GetFailedAttemptsCountAsync(email);
                    var timeRemaining = await _rateLimitService.GetLockoutTimeRemainingAsync(email);
                    
                    ViewBag.Error = $"Too many failed login attempts for this email. " +
                                   $"Please try again in {timeRemaining?.Minutes ?? 15} minutes.";
                    return View();
                }
            }

            // Validate credentials
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                // Record failed attempt with IP address
                // Use the user's current email if found, otherwise use entered email
                var emailToRecord = user?.Email ?? email;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _rateLimitService.RecordLoginAttemptAsync(emailToRecord, false, ipAddress, user == null ? "UserNotFound" : "InvalidPassword");
                
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            // ✅ SECURITY UPGRADE 2: Enhanced Admin Security (Hashed Security Code)
            if (user.Role == UserRole.Admin)
            {
                if (string.IsNullOrEmpty(securityCode))
                {
                    ViewBag.Error = "Security Code is required for Admin login.";
                    ViewBag.ShowSecurityCode = true;
                    ViewBag.Email = email;
                    return View();
                }

                // Verify hashed security code using BCrypt
                if (string.IsNullOrEmpty(user.SecurityCodeHash) || 
                    !BCrypt.Net.BCrypt.Verify(securityCode, user.SecurityCodeHash))
                {
                    // Record failed attempt using user's current email
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _rateLimitService.RecordLoginAttemptAsync(user.Email, false, ipAddress, "InvalidSecurityCode");
                    
                    ViewBag.Error = "Invalid Security Code.";
                    ViewBag.ShowSecurityCode = true;
                    ViewBag.Email = user.Email; // Use user's current email
                    return View();
                }
            }

            // ✅ Record successful login using user's current email
            var successIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _rateLimitService.RecordLoginAttemptAsync(user.Email, true, successIp);

            // Create User Session with SecurityStamp for session invalidation
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("SecurityStamp", user.SecurityStamp) // For global session invalidation
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToRoleDashboard(user.Role);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToRoleDashboard(UserRole? role = null)
        {
            // If role not passed, check current claim
            if (role == null)
            {
                var roleString = User.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrEmpty(roleString)) return RedirectToAction("Login");
                role = Enum.Parse<UserRole>(roleString);
            }

            return role == UserRole.Admin 
                ? RedirectToAction("Index", "Admin") 
                : RedirectToAction("Dashboard", "Employee");
        }
    }
}