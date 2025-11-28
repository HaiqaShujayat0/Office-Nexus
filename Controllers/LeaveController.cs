using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Models;
using System.Security.Claims;

namespace OfficeNexus.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly OfficeDbContext _context;

        public LeaveController(OfficeDbContext context)
        {
            _context = context;
        }

        // ==================== EMPLOYEE ACTIONS ====================

        /// <summary>
        /// GET: Show leave application form
        /// </summary>
        [Authorize(Roles = "Employee")]
        public IActionResult Apply()
        {
            return View();
        }

        /// <summary>
        /// POST: Submit leave application
        /// Validation: Only checks if dates are valid (Start < End)
        /// No blocking based on previous leave history
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequest model)
        {
            // CRITICAL FIX: Remove Employee/EmployeeId from ModelState
            // These fields are not sent by the form - they're set programmatically below
            // Without this, ModelState.IsValid will always be false
            ModelState.Remove("Employee");
            ModelState.Remove("EmployeeId");

            // Get current employee ID
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Custom validation: End date must be >= Start date
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date cannot be before start date");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Set employee ID and default values
            model.EmployeeId = int.Parse(userIdStr);
            model.Status = LeaveStatus.Pending;
            model.IsUnpaid = false;
            model.RequestedOn = DateTime.Now;

            _context.LeaveRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request submitted successfully!";
            return RedirectToAction(nameof(MyLeaves));
        }

        /// <summary>
        /// GET: Show employee's leave history
        /// </summary>
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyLeaves()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);

            var leaves = await _context.LeaveRequests
                .Where(l => l.EmployeeId == userId)
                .OrderByDescending(l => l.RequestedOn)
                .ToListAsync();

            return View(leaves);
        }

        // ==================== ADMIN ACTIONS ====================

        /// <summary>
        /// GET: Show all pending leave requests with quota calculation
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageRequests()
        {
            var pendingRequests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.Status == LeaveStatus.Pending)
                .OrderBy(l => l.RequestedOn)
                .ToListAsync();

            // Calculate quota for each request
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            ViewBag.QuotaData = new Dictionary<int, int>();

            foreach (var request in pendingRequests)
            {
                var approvedCount = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == request.EmployeeId
                                && l.Status == LeaveStatus.Approved
                                && l.StartDate.Month == currentMonth
                                && l.StartDate.Year == currentYear)
                    .CountAsync();

                ViewBag.QuotaData[request.Id] = approvedCount;
            }

            // Get processed requests for history (Approved or Rejected)
            var processedRequests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.Status != LeaveStatus.Pending)
                .OrderByDescending(l => l.RequestedOn)
                .Take(50) // Limit to last 50 processed requests
                .ToListAsync();

            ViewBag.ProcessedRequests = processedRequests;

            return View(pendingRequests);
        }

        /// <summary>
        /// POST: Process leave request (Approve Paid / Approve Unpaid / Reject)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessLeave(int id, string decision, string? remarks)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);

            if (leaveRequest == null)
            {
                TempData["Error"] = "Leave request not found";
                return RedirectToAction(nameof(ManageRequests));
            }

            // Process based on decision
            switch (decision)
            {
                case "Paid":
                    leaveRequest.Status = LeaveStatus.Approved;
                    leaveRequest.IsUnpaid = false;
                    TempData["Success"] = "Leave request approved (Paid)";
                    break;

                case "Unpaid":
                    leaveRequest.Status = LeaveStatus.Approved;
                    leaveRequest.IsUnpaid = true;
                    TempData["Success"] = "Leave request approved (Unpaid)";
                    break;

                case "Reject":
                    leaveRequest.Status = LeaveStatus.Rejected;
                    TempData["Success"] = "Leave request rejected";
                    break;

                default:
                    TempData["Error"] = "Invalid decision";
                    return RedirectToAction(nameof(ManageRequests));
            }

            // Save admin remarks
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                leaveRequest.AdminRemarks = remarks;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageRequests));
        }

        /// <summary>
        /// POST: Clear all processed leave requests history
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearHistory()
        {
            var processedRequests = await _context.LeaveRequests
                .Where(l => l.Status != LeaveStatus.Pending)
                .ToListAsync();

            _context.LeaveRequests.RemoveRange(processedRequests);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cleared {processedRequests.Count} processed leave request(s) from history";
            return RedirectToAction(nameof(ManageRequests));
        }
    }
}
