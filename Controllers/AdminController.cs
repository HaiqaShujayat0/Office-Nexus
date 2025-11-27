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
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            
            var stats = new
            {
                TotalEmployees = await _context.Users.CountAsync(u => u.Role == UserRole.Employee),
                TotalVisitorsToday = await _context.VisitorLogs.CountAsync(v => v.TimeIn.Date == DateTime.Today),
                ActiveVisitors = await _context.VisitorLogs.CountAsync(v => v.TimeOut == null),
                TotalVisitorsWeek = await _context.VisitorLogs.CountAsync(v => v.TimeIn >= startOfWeek),
                Employees = await _context.Users.Where(u => u.Role == UserRole.Employee).OrderByDescending(u => u.CreatedAt).ToListAsync()
            };
            
            return View(stats);
        }

        public async Task<IActionResult> EmployeeManagement()
        {
            var employees = await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            
            return View(employees);
        }

        public async Task<IActionResult> VisitorManagement()
        {
            var data = new
            {
                Visitors = await _context.VisitorLogs
                    .Include(v => v.Employee)
                    .OrderByDescending(v => v.TimeIn)
                    .Take(50)
                    .ToListAsync(),
                Employees = await _context.Users
                    .Where(u => u.Role == UserRole.Employee)
                    .OrderBy(u => u.FullName)
                    .ToListAsync()
            };
            
            return View(data);
        }

        public async Task<IActionResult> VisitorLogs()
        {
            var logs = await _context.VisitorLogs
                .Include(v => v.Employee)
                .OrderByDescending(v => v.TimeIn)
                .ToListAsync();
            
            return View(logs);
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee(string fullName, string email, string password, string jobTitle, string department, decimal basicSalary)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email already exists!";
                return RedirectToAction("EmployeeManagement");
            }

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Employee,
                JobTitle = jobTitle,
                Department = department,
                BasicSalary = basicSalary,
                SecurityCode = null // Employees don't have security codes
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Employee added successfully.";
            return RedirectToAction("EmployeeManagement");
        }

        [HttpPost]
        public async Task<IActionResult> LogVisitor(string visitorName, string purpose, string visitorType, int employeeId)
        {
            var visitor = new VisitorLog
            {
                VisitorName = visitorName,
                Purpose = purpose,
                VisitorType = visitorType,
                EmployeeId = employeeId,
                TimeIn = DateTime.Now
            };

            _context.VisitorLogs.Add(visitor);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Visitor '{visitorName}' logged successfully.";
            return RedirectToAction("VisitorManagement");
        }

        [HttpPost]
        public async Task<IActionResult> CheckOutVisitor(int id)
        {
            var visitor = await _context.VisitorLogs.FindAsync(id);
            if (visitor != null && visitor.TimeOut == null)
            {
                visitor.TimeOut = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Visitor checked out successfully.";
            }

            return RedirectToAction("VisitorManagement");
        }
    }
}