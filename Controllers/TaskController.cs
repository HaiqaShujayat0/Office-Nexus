using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Models;
using System.Security.Claims;

namespace OfficeNexus.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly OfficeDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TaskController(OfficeDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==================== ADMIN ACTIONS ====================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByAdmin)
                .Include(t => t.Comments)
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            var employees = await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Employees = employees;
            return View(tasks);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var employees = await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Employees = employees;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem task, IFormFile? attachmentFile)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var adminId = int.Parse(userIdStr);
            task.CreatedByAdminId = adminId;
            task.CreatedAt = DateTime.Now;

            // Tier 3: Handle File Upload
            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                // Validate file size (max 10MB)
                if (attachmentFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 10MB";
                    var employees = await _context.Users
                        .Where(u => u.Role == UserRole.Employee)
                        .OrderBy(u => u.FullName)
                        .ToListAsync();
                    ViewBag.Employees = employees;
                    return View(task);
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachmentFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachmentFile.CopyToAsync(stream);
                }

                // Save path to database
                task.AttachmentPath = "/uploads/" + fileName;
                task.AttachmentFileName = attachmentFile.FileName;
            }

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Task '{task.Title}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var employees = await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Employees = employees;
            return View(task);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem task, IFormFile? attachmentFile)
        {
            if (id != task.Id)
            {
                return NotFound();
            }

            var existingTask = await _context.TaskItems.FindAsync(id);
            if (existingTask == null)
            {
                return NotFound();
            }

            // Update properties
            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.AssignedToUserId = task.AssignedToUserId;
            existingTask.Priority = task.Priority;
            existingTask.Status = task.Status;
            existingTask.DueDate = task.DueDate;

            // Mark as completed if status is Done
            if (task.Status == TaskWorkflowStatus.Done && existingTask.CompletedAt == null)
            {
                existingTask.CompletedAt = DateTime.Now;
            }
            else if (task.Status != TaskWorkflowStatus.Done)
            {
                existingTask.CompletedAt = null;
            }

            // Handle new file upload
            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                // Validate file size
                if (attachmentFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 10MB";
                    var employees = await _context.Users
                        .Where(u => u.Role == UserRole.Employee)
                        .OrderBy(u => u.FullName)
                        .ToListAsync();
                    ViewBag.Employees = employees;
                    return View(existingTask);
                }

                // Delete old file if exists
                if (!string.IsNullOrEmpty(existingTask.AttachmentPath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingTask.AttachmentPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new file
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachmentFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachmentFile.CopyToAsync(stream);
                }

                existingTask.AttachmentPath = "/uploads/" + fileName;
                existingTask.AttachmentFileName = attachmentFile.FileName;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            // Delete attachment file if exists
            if (!string.IsNullOrEmpty(task.AttachmentPath))
            {
                var filePath = Path.Combine(_environment.WebRootPath, task.AttachmentPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ==================== EMPLOYEE ACTIONS ====================

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyTasks()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);

            var tasks = await _context.TaskItems
                .Include(t => t.CreatedByAdmin)
                .Include(t => t.Comments)
                .Where(t => t.AssignedToUserId == userId)
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return View(tasks);
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, TaskWorkflowStatus status)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var task = await _context.TaskItems.FindAsync(id);

            if (task == null || task.AssignedToUserId != userId)
            {
                return NotFound();
            }

            // Employees cannot mark tasks as Done (only admins can)
            if (status == TaskWorkflowStatus.Done)
            {
                TempData["Error"] = "Only administrators can mark tasks as Done.";
                return RedirectToAction(nameof(MyTasks));
            }

            task.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Task status updated to {status}!";
            return RedirectToAction(nameof(MyTasks));
        }

        // ==================== SHARED ACTIONS ====================

        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByAdmin)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Check authorization
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var isAdmin = User.IsInRole("Admin");

            // Employees can only view their own tasks
            if (!isAdmin && task.AssignedToUserId != userId)
            {
                return Forbid();
            }

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int taskId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);

            var comment = new TaskComment
            {
                TaskItemId = taskId,
                Message = message.Trim(),
                UserId = userId,
                PostedAt = DateTime.Now
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comment added successfully!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null || string.IsNullOrEmpty(task.AttachmentPath))
            {
                return NotFound();
            }

            // Check authorization
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var isAdmin = User.IsInRole("Admin");

            // Employees can only download attachments from their own tasks
            if (!isAdmin && task.AssignedToUserId != userId)
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, task.AttachmentPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = "application/octet-stream";
            var fileName = task.AttachmentFileName ?? Path.GetFileName(filePath);

            return File(memory, contentType, fileName);
        }
    }
}
