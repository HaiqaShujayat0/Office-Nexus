using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using System.Security.Claims;

namespace OfficeNexus.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly OfficeDbContext _context;

        public EmployeeController(OfficeDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            
            var stats = new
            {
                VisitorsToday = await _context.VisitorLogs.CountAsync(v => v.EmployeeId == userId && v.TimeIn.Date == DateTime.Today),
                VisitorsWeek = await _context.VisitorLogs.CountAsync(v => v.EmployeeId == userId && v.TimeIn >= startOfWeek),
                TotalVisitors = await _context.VisitorLogs.CountAsync(v => v.EmployeeId == userId),
                RecentLogs = await _context.VisitorLogs
                    .Where(v => v.EmployeeId == userId)
                    .OrderByDescending(v => v.TimeIn)
                    .Take(5)
                    .ToListAsync()
            };
            
            return View(stats);
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            
            // Get logs handled by this employee, ordered by most recent
            var myLogs = await _context.VisitorLogs
                .Where(v => v.EmployeeId == userId)
                .OrderByDescending(v => v.TimeIn)
                .Take(50)
                .ToListAsync();

            return View(myLogs);
        }

        public IActionResult Profile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LogVisitor(VisitorLog model)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            
            model.EmployeeId = userId;
            model.TimeIn = DateTime.Now;
            
            _context.VisitorLogs.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Visitor logged successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int id)
        {
             var log = await _context.VisitorLogs.FindAsync(id);
             if(log != null)
             {
                 log.TimeOut = DateTime.Now;
                 await _context.SaveChangesAsync();
                 TempData["Success"] = "Visitor checked out successfully!";
             }
             return RedirectToAction("Index");
        }
    }
}