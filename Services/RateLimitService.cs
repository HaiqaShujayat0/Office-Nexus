using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;

namespace OfficeNexus.Services
{
    /// <summary>
    /// Interface for rate limiting service to prevent brute force attacks
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks if an account is temporarily locked due to failed login attempts
        /// </summary>
        Task<bool> IsAccountLockedAsync(string email);
        
        /// <summary>
        /// Records a login attempt (success or failure) for audit and rate limiting
        /// </summary>
        Task RecordLoginAttemptAsync(string email, bool success, string? ipAddress = null, string? failureReason = null);
        
        /// <summary>
        /// Gets the count of recent failed attempts for an email
        /// </summary>
        Task<int> GetFailedAttemptsCountAsync(string email);
        
        /// <summary>
        /// Gets the time remaining until account unlock
        /// </summary>
        Task<TimeSpan?> GetLockoutTimeRemainingAsync(string email);
    }

    /// <summary>
    /// Service for implementing rate limiting and brute force protection
    /// Implements Defense in Depth security principle
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly OfficeDbContext _context;
        
        // Security Configuration Constants
        private const int MaxFailedAttempts = 5;        // Maximum failed attempts before lockout
        private const int LockoutMinutes = 15;          // Lockout duration in minutes
        private const int CleanupDays = 30;             // Keep login attempts for 30 days

        public RateLimitService(OfficeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Checks if account is locked based on recent failed attempts
        /// </summary>
        public async Task<bool> IsAccountLockedAsync(string email)
        {
            var lockoutThreshold = DateTime.Now.AddMinutes(-LockoutMinutes);
            
            var recentFailedAttempts = await _context.LoginAttempts
                .Where(a => a.Email.ToLower() == email.ToLower() && 
                           !a.WasSuccessful && 
                           a.AttemptTime > lockoutThreshold)
                .CountAsync();

            return recentFailedAttempts >= MaxFailedAttempts;
        }

        /// <summary>
        /// Records login attempt for security audit trail
        /// </summary>
        public async Task RecordLoginAttemptAsync(string email, bool success, string? ipAddress = null, string? failureReason = null)
        {
            var attempt = new LoginAttempt
            {
                Email = email,
                WasSuccessful = success,
                IpAddress = ipAddress,
                FailureReason = failureReason,
                AttemptTime = DateTime.Now
            };

            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            
            // Cleanup old records (performance optimization)
            await CleanupOldAttemptsAsync();
        }

        /// <summary>
        /// Gets count of failed attempts within lockout window
        /// </summary>
        public async Task<int> GetFailedAttemptsCountAsync(string email)
        {
            var lockoutThreshold = DateTime.Now.AddMinutes(-LockoutMinutes);
            
            return await _context.LoginAttempts
                .Where(a => a.Email.ToLower() == email.ToLower() && 
                           !a.WasSuccessful && 
                           a.AttemptTime > lockoutThreshold)
                .CountAsync();
        }

        /// <summary>
        /// Calculates remaining lockout time
        /// </summary>
        public async Task<TimeSpan?> GetLockoutTimeRemainingAsync(string email)
        {
            var lockoutThreshold = DateTime.Now.AddMinutes(-LockoutMinutes);
            
            var oldestFailedAttempt = await _context.LoginAttempts
                .Where(a => a.Email.ToLower() == email.ToLower() && 
                           !a.WasSuccessful && 
                           a.AttemptTime > lockoutThreshold)
                .OrderBy(a => a.AttemptTime)
                .FirstOrDefaultAsync();

            if (oldestFailedAttempt == null)
                return null;

            var unlockTime = oldestFailedAttempt.AttemptTime.AddMinutes(LockoutMinutes);
            var remaining = unlockTime - DateTime.Now;

            return remaining > TimeSpan.Zero ? remaining : null;
        }

        /// <summary>
        /// Cleanup old login attempts to prevent database bloat
        /// Runs automatically after each login attempt
        /// </summary>
        private async Task CleanupOldAttemptsAsync()
        {
            // Only cleanup occasionally (1% chance per login)
            if (Random.Shared.Next(100) != 0)
                return;

            var cleanupThreshold = DateTime.Now.AddDays(-CleanupDays);
            
            var oldAttempts = await _context.LoginAttempts
                .Where(a => a.AttemptTime < cleanupThreshold)
                .ToListAsync();

            if (oldAttempts.Any())
            {
                _context.LoginAttempts.RemoveRange(oldAttempts);
                await _context.SaveChangesAsync();
            }
        }
    }
}
