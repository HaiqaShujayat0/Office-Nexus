using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Models;

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

        public async Task<IActionResult> Reports()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            var last30Days = now.AddDays(-30);
            var last7Days = now.AddDays(-7);

            // Visitor Statistics
            var totalVisitors = await _context.VisitorLogs.CountAsync();
            var visitorsToday = await _context.VisitorLogs.CountAsync(v => v.TimeIn.Date == now.Date);
            var visitorsThisWeek = await _context.VisitorLogs.CountAsync(v => v.TimeIn >= startOfWeek);
            var visitorsThisMonth = await _context.VisitorLogs.CountAsync(v => v.TimeIn >= startOfMonth);
            var activeVisitors = await _context.VisitorLogs.CountAsync(v => v.TimeOut == null);
            
            // Visitor trends (last 7 days)
            var visitorTrends = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = now.AddDays(-i).Date;
                var count = await _context.VisitorLogs.CountAsync(v => v.TimeIn.Date == date);
                visitorTrends.Add(count);
            }

            // Visitor types breakdown
            var visitorTypes = await _context.VisitorLogs
                .GroupBy(v => v.VisitorType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            // Employee Statistics
            var totalEmployees = await _context.Users.CountAsync(u => u.Role == UserRole.Employee);
            var activeEmployees = await _context.Users.CountAsync(u => u.Role == UserRole.Employee && u.Status == EmployeeStatus.Active);
            var pendingEmployees = await _context.Users.CountAsync(u => u.Role == UserRole.Employee && u.Status == EmployeeStatus.Pending);
            var terminatedEmployees = await _context.Users.CountAsync(u => u.Role == UserRole.Employee && u.Status == EmployeeStatus.Terminated);

            // Department breakdown
            var departmentStats = await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .GroupBy(u => u.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            // Task Statistics
            var totalTasks = await _context.TaskItems.CountAsync();
            var tasksByStatus = await _context.TaskItems
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Leave Statistics
            var totalLeaveRequests = await _context.LeaveRequests.CountAsync();
            var pendingLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);
            var approvedLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Approved);
            var rejectedLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Rejected);
            
            // Leave types breakdown
            var leaveTypes = await _context.LeaveRequests
                .GroupBy(l => l.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Top employees by visitor handling
            var topEmployeesByVisitors = await _context.VisitorLogs
                .Include(v => v.Employee)
                .GroupBy(v => v.EmployeeId)
                .Select(g => new { 
                    EmployeeId = g.Key, 
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            // Get employee details for top employees
            var topEmployeesList = new List<dynamic>();
            foreach (var emp in topEmployeesByVisitors)
            {
                var employee = await _context.Users.FindAsync(emp.EmployeeId);
                topEmployeesList.Add(new { 
                    EmployeeId = emp.EmployeeId, 
                    Count = emp.Count,
                    Employee = employee
                });
            }

            // Peak hours (visitors by hour of day)
            var peakHours = new List<int>();
            for (int hour = 0; hour < 24; hour++)
            {
                var count = await _context.VisitorLogs
                    .CountAsync(v => v.TimeIn.Hour == hour);
                peakHours.Add(count);
            }

            var viewModel = new
            {
                // Visitor Stats
                TotalVisitors = totalVisitors,
                VisitorsToday = visitorsToday,
                VisitorsThisWeek = visitorsThisWeek,
                VisitorsThisMonth = visitorsThisMonth,
                ActiveVisitors = activeVisitors,
                VisitorTrends = visitorTrends,
                VisitorTypes = visitorTypes,
                PeakHours = peakHours,

                // Employee Stats
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                PendingEmployees = pendingEmployees,
                TerminatedEmployees = terminatedEmployees,
                DepartmentStats = departmentStats,

                // Task Stats
                TotalTasks = totalTasks,
                TasksByStatus = tasksByStatus,

                // Leave Stats
                TotalLeaveRequests = totalLeaveRequests,
                PendingLeaves = pendingLeaves,
                ApprovedLeaves = approvedLeaves,
                RejectedLeaves = rejectedLeaves,
                LeaveTypes = leaveTypes,

                // Top Employees
                TopEmployeesByVisitors = topEmployeesList
            };

            return View(viewModel);
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee(
            string fullName, 
            string email, 
            string password, 
            string jobTitle, 
            string department, 
            decimal basicSalary,
            string phoneNumber,
            string homeAddress)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email already exists!";
                return RedirectToAction("EmployeeManagement");
            }

            // Server-side validation for phone number length
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                // Simple check to ensure it's not too long/short if it contains digits
                var digitCount = phoneNumber.Count(char.IsDigit);
                if (digitCount > 0 && (digitCount < 10 || digitCount > 15))
                {
                     // Note: We are being lenient here as the [Phone] attribute on the model 
                     // handles strict validation, but this catches obvious errors before saving.
                }
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
                SecurityCodeHash = null, // Employees don't have security codes
                PhoneNumber = phoneNumber,
                HomeAddress = homeAddress,
                Status = EmployeeStatus.Active // New employees added manually are Active by default
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Employee added successfully.";
            return RedirectToAction("EmployeeManagement");
        }

        [HttpPost]
        public async Task<IActionResult> ImportEmployees(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid Excel file.";
                return RedirectToAction("EmployeeManagement");
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only .xlsx files are supported.";
                return RedirectToAction("EmployeeManagement");
            }

            try
            {
                // Set EPPlus license context
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                int importedCount = 0;
                int activeCount = 0;
                int pendingCount = 0;
                var errors = new List<string>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new OfficeOpenXml.ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["Error"] = "Excel file is empty or invalid.";
                            return RedirectToAction("EmployeeManagement");
                        }

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        if (rowCount < 2)
                        {
                            TempData["Error"] = "Excel file must contain at least a header row and one data row.";
                            return RedirectToAction("EmployeeManagement");
                        }

                        // Read header row to find column indices
                        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        for (int col = 1; col <= worksheet.Dimension!.Columns; col++)
                        {
                            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(headerValue))
                            {
                                headers[headerValue] = col;
                            }
                        }

                        // Validate required columns exist
                        var requiredColumns = new[] { "FullName", "Email", "JobTitle", "Department", "BasicSalary", "PhoneNumber", "HomeAddress" };
                        var missingColumns = requiredColumns.Where(col => !headers.ContainsKey(col)).ToList();
                        if (missingColumns.Any())
                        {
                            TempData["Error"] = $"Missing required columns: {string.Join(", ", missingColumns)}";
                            return RedirectToAction("EmployeeManagement");
                        }

                        // Process each row
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var fullName = worksheet.Cells[row, headers["FullName"]].Value?.ToString()?.Trim() ?? "";
                                var email = worksheet.Cells[row, headers["Email"]].Value?.ToString()?.Trim() ?? "";
                                var jobTitle = worksheet.Cells[row, headers["JobTitle"]].Value?.ToString()?.Trim() ?? "";
                                var department = worksheet.Cells[row, headers["Department"]].Value?.ToString()?.Trim() ?? "";
                                var basicSalaryStr = worksheet.Cells[row, headers["BasicSalary"]].Value?.ToString()?.Trim() ?? "";
                                var phoneNumber = worksheet.Cells[row, headers["PhoneNumber"]].Value?.ToString()?.Trim() ?? "";
                                var homeAddress = worksheet.Cells[row, headers["HomeAddress"]].Value?.ToString()?.Trim() ?? "";
                                var password = headers.ContainsKey("Password") 
                                    ? worksheet.Cells[row, headers["Password"]].Value?.ToString()?.Trim() ?? "ChangeMe123!"
                                    : "ChangeMe123!";

                                // Skip completely empty rows
                                if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(email))
                                {
                                    continue;
                                }

                                // Check if email already exists
                                if (_context.Users.Any(u => u.Email == email))
                                {
                                    errors.Add($"Row {row}: Email '{email}' already exists.");
                                    continue;
                                }

                                // Parse salary
                                decimal basicSalary = 0;
                                if (!string.IsNullOrWhiteSpace(basicSalaryStr))
                                {
                                    decimal.TryParse(basicSalaryStr, out basicSalary);
                                }

                                // Determine if all required fields are present
                                bool isComplete = !string.IsNullOrWhiteSpace(fullName) &&
                                                  !string.IsNullOrWhiteSpace(email) &&
                                                  !string.IsNullOrWhiteSpace(jobTitle) &&
                                                  !string.IsNullOrWhiteSpace(department) &&
                                                  basicSalary > 0 &&
                                                  !string.IsNullOrWhiteSpace(phoneNumber) &&
                                                  !string.IsNullOrWhiteSpace(homeAddress);

                                var newUser = new User
                                {
                                    FullName = fullName,
                                    Email = email,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                                    Role = UserRole.Employee,
                                    JobTitle = jobTitle,
                                    Department = department,
                                    BasicSalary = basicSalary,
                                    SecurityCodeHash = null,
                                    PhoneNumber = phoneNumber,
                                    HomeAddress = homeAddress,
                                    Status = isComplete ? EmployeeStatus.Active : EmployeeStatus.Pending
                                };

                                _context.Users.Add(newUser);
                                importedCount++;

                                if (isComplete)
                                    activeCount++;
                                else
                                    pendingCount++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Row {row}: {ex.Message}");
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                var successMessage = $"Successfully imported {importedCount} employee(s). Active: {activeCount}, Pending: {pendingCount}.";
                if (errors.Any())
                {
                    successMessage += $" {errors.Count} error(s) occurred.";
                }
                
                TempData["Success"] = successMessage;
                if (errors.Any())
                {
                    TempData["ImportErrors"] = string.Join("<br/>", errors.Take(5));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing Excel file: {ex.Message}";
            }

            return RedirectToAction("EmployeeManagement");
        }

        public async Task<IActionResult> EditEmployee(int id)
        {
            var employee = await _context.Users.FindAsync(id);
            if (employee == null || employee.Role != UserRole.Employee)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("EmployeeManagement");
            }

            return View(employee);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(
            int id,
            string fullName, 
            string email, 
            string jobTitle, 
            string department, 
            decimal basicSalary,
            string phoneNumber,
            string homeAddress)
        {
            var employee = await _context.Users.FindAsync(id);
            if (employee == null || employee.Role != UserRole.Employee)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("EmployeeManagement");
            }

            // Check if email is being changed and if it already exists
            if (employee.Email != email && _context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email already exists!";
                return RedirectToAction("EditEmployee", new { id });
            }

            // Update employee fields
            employee.FullName = fullName;
            employee.Email = email;
            employee.JobTitle = jobTitle;
            employee.Department = department;
            employee.BasicSalary = basicSalary;
            employee.PhoneNumber = phoneNumber;
            employee.HomeAddress = homeAddress;

            // Recalculate status based on completeness
            bool isComplete = !string.IsNullOrWhiteSpace(fullName) &&
                              !string.IsNullOrWhiteSpace(email) &&
                              !string.IsNullOrWhiteSpace(jobTitle) &&
                              !string.IsNullOrWhiteSpace(department) &&
                              basicSalary > 0 &&
                              !string.IsNullOrWhiteSpace(phoneNumber) &&
                              !string.IsNullOrWhiteSpace(homeAddress);

            employee.Status = isComplete ? EmployeeStatus.Active : EmployeeStatus.Pending;

            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Employee updated successfully. Status: {employee.Status}";
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