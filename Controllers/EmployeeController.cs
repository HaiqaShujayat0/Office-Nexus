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

        public async Task<IActionResult> Index()
        {
            // FIXED: Added check for null using ?? "0"
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            
            // Get logs handled by this employee, ordered by most recent
            var myLogs = await _context.VisitorLogs
                .Where(v => v.EmployeeId == userId)
                .OrderByDescending(v => v.TimeIn)
                .Take(20)
                .ToListAsync();

            return View(myLogs);
        }

        [HttpPost]
        public async Task<IActionResult> LogVisitor(VisitorLog model)
        {
            // FIXED: Added check for null
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Auth");

            var userId = int.Parse(userIdStr);
            
            model.EmployeeId = userId;
            model.TimeIn = DateTime.Now;
            
            _context.VisitorLogs.Add(model);
            await _context.SaveChangesAsync();

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
             }
             return RedirectToAction("Index");
        }
    }
}