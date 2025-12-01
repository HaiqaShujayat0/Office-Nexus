using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Models;
using OfficeNexus.Services;
using OfficeNexus.ViewModels;
using OfficeNexus.Helpers;
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
            // Additional server-side validation for password requirements
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (model.NewPassword.Length < 8)
                {
                    ModelState.AddModelError("NewPassword", "Password must be at least 8 characters long");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(model.NewPassword, @"\d"))
                {
                    ModelState.AddModelError("NewPassword", "Password must contain at least one digit");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = string.Join(", ", errors);
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

        [AllowAnonymous]
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

            // Check if new email is already taken by another user
            var emailExists = await _context.Users.AnyAsync(u => u.Email == user.NewEmailCandidate && u.Id != user.Id);
            if (emailExists)
            {
                // Clear the pending change
                user.NewEmailCandidate = null;
                user.EmailVerificationToken = null;
                await _context.SaveChangesAsync();
                
                TempData["Error"] = "This email is already in use by another account. Email change cancelled.";
                return RedirectToAction("Login", "Auth");
            }

            var oldEmail = user.Email;
            
            // Clear recent failed login attempts for the old email
            // Since user has verified the new email, we give them a fresh start
            // This prevents old failed attempts from locking the new email
            var lockoutThreshold = DateTime.Now.AddMinutes(-15); // Last 15 minutes
            var recentFailedAttempts = await _context.LoginAttempts
                .Where(a => a.Email.ToLower() == oldEmail.ToLower() && 
                           !a.WasSuccessful && 
                           a.AttemptTime > lockoutThreshold)
                .ToListAsync();
            
            if (recentFailedAttempts.Any())
            {
                _context.LoginAttempts.RemoveRange(recentFailedAttempts);
            }
            
            // Also clear any failed attempts that might have been made with the new email
            // (in case user tried logging in before confirming email change)
            var newEmailFailedAttempts = await _context.LoginAttempts
                .Where(a => a.Email.ToLower() == user.NewEmailCandidate.ToLower() && 
                           !a.WasSuccessful && 
                           a.AttemptTime > lockoutThreshold)
                .ToListAsync();
            
            if (newEmailFailedAttempts.Any())
            {
                _context.LoginAttempts.RemoveRange(newEmailFailedAttempts);
            }
            
            // Update user email
            user.Email = user.NewEmailCandidate;
            user.NewEmailCandidate = null;
            user.EmailVerificationToken = null;
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate all existing sessions

            await _context.SaveChangesAsync();

            // Check if user is currently logged in
            var currentUserIdStr = User.FindFirstValue("UserId");
            if (!string.IsNullOrEmpty(currentUserIdStr) && int.Parse(currentUserIdStr) == user.Id)
            {
                // User is logged in - update their session
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

                await HttpContext.SignOutAsync(); // Sign out first to clear old session
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["Success"] = $"✅ Email address updated successfully! Your new email is {user.Email}";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                // User is not logged in - redirect to login with success message
                TempData["Success"] = $"Email address updated successfully! Please login with your new email address: {user.Email}";
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

        [HttpGet]
        public async Task<IActionResult> BankDetails()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);
            var bankAccount = await _context.UserBankAccounts
                .FirstOrDefaultAsync(uba => uba.UserId == userId);

            var model = new BankDetailsViewModel();
            
            if (bankAccount != null)
            {
                model.Id = bankAccount.Id;
                model.BankName = bankAccount.BankName; // Not encrypted - not sensitive
                
                // Decrypt sensitive fields before displaying to user
                try
                {
                    model.AccountTitle = SecurityHelper.Decrypt(bankAccount.AccountTitle);
                    model.IBAN = SecurityHelper.Decrypt(bankAccount.IBAN);
                    model.CNIC = SecurityHelper.Decrypt(bankAccount.CNIC);
                    
                    // AccountNumber is optional, decrypt only if not null
                    if (!string.IsNullOrEmpty(bankAccount.AccountNumber))
                    {
                        model.AccountNumber = SecurityHelper.Decrypt(bankAccount.AccountNumber);
                    }
                }
                catch (Exception)
                {
                    // If decryption fails, it might be old unencrypted data or corrupted data
                    // Log the error and show a message to the user
                    TempData["Error"] = "Unable to retrieve bank details. Please contact support.";
                    // Optionally, you could try to show the raw data if it's not encrypted
                    // For now, we'll show empty fields
                    model.AccountTitle = string.Empty;
                    model.IBAN = string.Empty;
                    model.CNIC = string.Empty;
                    model.AccountNumber = null;
                }
                
                model.BranchCode = bankAccount.BranchCode; // Not encrypted - not sensitive
            }
            else
            {
                // Pre-fill Account Title with user's full name
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    model.AccountTitle = user.FullName.ToUpper();
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BankDetails(BankDetailsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdStr = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdStr);

            // Clean and format IBAN: Remove spaces, convert to uppercase
            var cleanedIban = model.IBAN?.Replace(" ", "").Replace("-", "").ToUpper() ?? "";
            
            // Validate IBAN format after cleaning
            if (string.IsNullOrWhiteSpace(cleanedIban))
            {
                ModelState.AddModelError("IBAN", "IBAN is required");
                return View(model);
            }
            
            if (cleanedIban.Length != 24)
            {
                ModelState.AddModelError("IBAN", "IBAN must be exactly 24 characters");
                return View(model);
            }
            
            if (!cleanedIban.StartsWith("PK"))
            {
                ModelState.AddModelError("IBAN", "IBAN must start with 'PK'");
                return View(model);
            }
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanedIban, @"^PK[0-9A-Z]{22}$"))
            {
                ModelState.AddModelError("IBAN", "IBAN must start with 'PK' and contain only uppercase letters and numbers");
                return View(model);
            }

            // Validate that Account Title matches user's CNIC name (if we have CNIC)
            // Note: In real scenario, you might want to verify CNIC matches user's actual CNIC
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found";
                return RedirectToAction("Login", "Auth");
            }

            // Check if bank account already exists
            var existingBankAccount = await _context.UserBankAccounts
                .FirstOrDefaultAsync(uba => uba.UserId == userId);

            // Encrypt sensitive fields before saving to database
            // Note: Validation is done on plain text, encryption happens just before persistence
            string encryptedAccountTitle = SecurityHelper.Encrypt(model.AccountTitle.Trim().ToUpper());
            string encryptedIban = SecurityHelper.Encrypt(cleanedIban);
            string encryptedCnic = SecurityHelper.Encrypt(model.CNIC.Trim());
            string? encryptedAccountNumber = string.IsNullOrWhiteSpace(model.AccountNumber) 
                ? null 
                : SecurityHelper.Encrypt(model.AccountNumber.Trim());

            if (existingBankAccount != null)
            {
                // Update existing
                existingBankAccount.BankName = model.BankName.Trim(); // Not encrypted
                existingBankAccount.AccountTitle = encryptedAccountTitle; // Encrypted
                existingBankAccount.IBAN = encryptedIban; // Encrypted
                existingBankAccount.AccountNumber = encryptedAccountNumber; // Encrypted (if provided)
                existingBankAccount.BranchCode = string.IsNullOrWhiteSpace(model.BranchCode) ? null : model.BranchCode.Trim(); // Not encrypted
                existingBankAccount.CNIC = encryptedCnic; // Encrypted
                existingBankAccount.UpdatedAt = DateTime.Now;
            }
            else
            {
                // Create new
                var bankAccount = new UserBankAccount
                {
                    UserId = userId,
                    BankName = model.BankName.Trim(), // Not encrypted
                    AccountTitle = encryptedAccountTitle, // Encrypted
                    IBAN = encryptedIban, // Encrypted
                    AccountNumber = encryptedAccountNumber, // Encrypted (if provided)
                    BranchCode = string.IsNullOrWhiteSpace(model.BranchCode) ? null : model.BranchCode.Trim(), // Not encrypted
                    CNIC = encryptedCnic, // Encrypted
                    CreatedAt = DateTime.Now
                };

                _context.UserBankAccounts.Add(bankAccount);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Bank details saved successfully!";
            return RedirectToAction(nameof(BankDetails));
        }
    }
}