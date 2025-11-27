using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;

namespace OfficeNexus.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly OfficeDbContext _context;

        public AdminController(OfficeDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalEmployees = await _context.Users.CountAsync(u => u.Role == UserRole.Employee),
                TotalVisitorsToday = await _context.VisitorLogs.CountAsync(v => v.TimeIn.Date == DateTime.Today),
                Employees = await _context.Users.Where(u => u.Role == UserRole.Employee).ToListAsync()
            };
            
            return View(stats);
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee(string fullName, string email, string password)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email already exists!";
                return RedirectToAction("Index");
            }

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Employee
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Employee added successfully.";
            return RedirectToAction("Index");
        }
        
        // Future: Add method here to rotate AES keys or manage encryption settings
    }
}