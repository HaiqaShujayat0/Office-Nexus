using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using System.Security.Claims;

namespace OfficeNexus.Middleware
{
    /// <summary>
    /// Middleware to validate SecurityStamp on each request.
    /// If the SecurityStamp in the cookie doesn't match the database, the user is logged out.
    /// This ensures that when a user changes their password or email, ALL sessions across ALL devices are invalidated.
    /// </summary>
    public class SecurityStampValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityStampValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, OfficeDbContext dbContext)
        {
            // Only validate for authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("UserId");
                var securityStampClaim = context.User.FindFirst("SecurityStamp");

                if (userIdClaim != null && securityStampClaim != null)
                {
                    var userId = int.Parse(userIdClaim.Value);
                    var claimSecurityStamp = securityStampClaim.Value;

                    // Get current SecurityStamp from database
                    var user = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    // If user doesn't exist or SecurityStamp doesn't match, log out
                    if (user == null || user.SecurityStamp != claimSecurityStamp)
                    {
                        // SecurityStamp mismatch - credentials were changed on another device
                        await context.SignOutAsync();
                        context.Response.Redirect("/Auth/Login");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method to register the middleware
    public static class SecurityStampValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityStampValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityStampValidationMiddleware>();
        }
    }
}
