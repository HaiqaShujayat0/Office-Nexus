using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Models;
using OfficeNexus.Services;
using OfficeNexus.ViewModels;
using System.Security.Claims;

namespace OfficeNexus.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly OfficeDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public EmployeeController(OfficeDbContext context, IWebHostEnvironment environment, IEmailService emailService)
        {
            _context = context;
            _environment = environment;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);

            // Get task counts for the dashboard using TaskItems (not TaskAssignments)
            ViewBag.TodoCount = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId && t.Status == TaskWorkflowStatus.ToDo && !t.IsArchived)
                .CountAsync();

            ViewBag.InProgressCount = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId && t.Status == TaskWorkflowStatus.InProgress && !t.IsArchived)
                .CountAsync();

            ViewBag.InReviewCount = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId && t.Status == TaskWorkflowStatus.InReview && !t.IsArchived)
                .CountAsync();

            ViewBag.CompletedCount = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId && t.Status == TaskWorkflowStatus.Done && !t.IsArchived)
                .CountAsync();

            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var viewModel = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                JobTitle = user.JobTitle,
                Department = user.Department,
                PhoneNumber = user.PhoneNumber,
                HomeAddress = user.HomeAddress,
                ProfilePicturePath = user.ProfilePicturePath,
                MemberSince = user.CreatedAt
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var userId = int.Parse(userIdStr);

            if (profilePicture == null || profilePicture.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            if (profilePicture.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size must be under 2MB" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Invalid file type. Only JPG and PNG are allowed." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            if (!string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, user.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var folderPath = Path.Combine(_environment.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            user.ProfilePicturePath = $"/images/profiles/{fileName}";
            await _context.SaveChangesAsync();

            return Json(new { success = true, newUrl = user.ProfilePicturePath, message = "Profile picture updated successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }

            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found";
                return RedirectToAction(nameof(Profile));
            }

            bool isCorrect = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);
            if (!isCorrect)
            {
                TempData["Error"] = "Current password is incorrect";
                return RedirectToAction(nameof(Profile));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();

            await _context.SaveChangesAsync();
            await HttpContext.SignOutAsync();

            TempData["Success"] = "Password changed successfully! All devices have been logged out. Please login with your new password.";
            return RedirectToAction("Login", "Auth");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEmailChange(UpdateEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }

            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found";
                return RedirectToAction(nameof(Profile));
            }

            bool isCorrect = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);
            if (!isCorrect)
            {
                TempData["Error"] = "Current password is incorrect";
                return RedirectToAction(nameof(Profile));
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == model.NewEmail && u.Id != userId);
            if (emailExists)
            {
                TempData["Error"] = "This email is already in use by another account";
                return RedirectToAction(nameof(Profile));
            }

            var token = Guid.NewGuid().ToString();
            user.NewEmailCandidate = model.NewEmail;
            user.EmailVerificationToken = token;
            await _context.SaveChangesAsync();

            try
            {
                var verificationUrl = Url.Action(
                    "ConfirmEmailChange",
                    "Employee",
                    new { token = token },
                    protocol: Request.Scheme
                );

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #2563eb;'>Email Verification - OfficeNexus</h2>
                            <p>Hello {user.FullName},</p>
                            <p>You requested to change your email address to <strong>{model.NewEmail}</strong>.</p>
                            <p>Please click the button below to confirm this change:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationUrl}' style='background-color: #2563eb; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Email Address</a>
                            </div>
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #666;'>{verificationUrl}</p>
                            <p style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px;'>
                                If you didn't request this change, please ignore this email or contact support.
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailService.SendEmailAsync(model.NewEmail, "Verify Your New Email Address - OfficeNexus", emailBody);
                TempData["Success"] = $"Verification email sent to {model.NewEmail}. Please check your inbox and click the verification link.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to send verification email: {ex.Message}";
            }

            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> ConfirmEmailChange(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid verification link";
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
            {
                TempData["Error"] = "Invalid or expired verification token";
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(user.NewEmailCandidate))
            {
                TempData["Error"] = "No pending email change found";
                return RedirectToAction("Login", "Auth");
            }

            var oldEmail = user.Email;
            user.Email = user.NewEmailCandidate;
            user.NewEmailCandidate = null;
            user.EmailVerificationToken = null;
            user.SecurityStamp = Guid.NewGuid().ToString();

            await _context.SaveChangesAsync();

            var currentUserIdStr = User.FindFirstValue("UserId");
            if (!string.IsNullOrEmpty(currentUserIdStr) && int.Parse(currentUserIdStr) == user.Id)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("SecurityStamp", user.SecurityStamp)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Success"] = $"✅ Email address updated successfully! Your new email is {user.Email}";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                TempData["Success"] = "Email address updated successfully! Please login with your new email address.";
                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePersonalInfo(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }

            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found";
                return RedirectToAction(nameof(Profile));
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.HomeAddress = model.HomeAddress;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Personal information updated successfully!";
            return RedirectToAction(nameof(Profile));
        }
    }
}