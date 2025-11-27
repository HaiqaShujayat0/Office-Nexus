using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OfficeNexus.Data;
using System.Security.Claims;

namespace OfficeNexus.Controllers
{
    public class AuthController : Controller
    {
        private readonly OfficeDbContext _context;

        public AuthController(OfficeDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            // Create User Session
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString())
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
                : RedirectToAction("Index", "Employee");
        }
    }
}