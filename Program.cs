using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Services;

// 1. CREATE BUILDER (This line must be first!)
var builder = WebApplication.CreateBuilder(args);

// 2. ADD MVC CONTROLLERS
builder.Services.AddControllersWithViews();

// 3. REGISTER DATABASE
builder.Services.AddDbContext<OfficeDbContext>(options =>
    options.UseSqlite("Data Source=office_nexus.db"));

// 4. REGISTER YOUR CUSTOM SERVICES
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 5. ADD AUTHENTICATION (The Secure Cookie Logic with SecurityStamp Validation)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
        
        // Cookie Security Configuration
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "OfficeNexus.Auth";

        // 🔒 SECURITY: Global Session Invalidation via SecurityStamp Validation
        // This runs on EVERY REQUEST to check if the session is still valid
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userId = context.Principal?.FindFirst("UserId")?.Value;
                var cookieStamp = context.Principal?.FindFirst("SecurityStamp")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(cookieStamp))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                // Resolve DB Context inside the event
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<OfficeDbContext>();
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

                // If user deleted OR stamp changed (password/email update) -> Logout
                if (user == null || user.SecurityStamp != cookieStamp)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

// ===========================================
// BUILD THE APP (Nothing 'builder' after this)
// ===========================================
var app = builder.Build();

// 6. DATABASE SEEDING (Run on startup to ensure Admin exists)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfficeDbContext>();
    db.Database.EnsureCreated(); // Creates DB if not exists

    if (!db.Users.Any(u => u.Role == UserRole.Admin))
    {
        // CREATE DEFAULT ADMIN
        // Password is "admin123", Security Code is "ADMIN2024"
        string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        string securityCodeHash = BCrypt.Net.BCrypt.HashPassword("ADMIN2024");
        
        db.Users.Add(new User 
        { 
            FullName = "System Administrator", 
            Email = "admin@officenexus.com", 
            PasswordHash = passwordHash, 
            Role = UserRole.Admin,
            SecurityCodeHash = securityCodeHash,
            JobTitle = "System Administrator",
            Department = "IT",
            BasicSalary = 50000,
            PhoneNumber = "+1-555-0100",
            HomeAddress = "123 Admin Street, HQ Building",
            SecurityStamp = Guid.NewGuid().ToString(),
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();
    }
    
    // 🔧 DATA MIGRATION: Hash existing plain text security codes
    var adminsWithPlainTextCodes = db.Users
        .Where(u => u.Role == UserRole.Admin && 
                   u.SecurityCodeHash != null && 
                   !u.SecurityCodeHash.StartsWith("$2"))
        .ToList();
    
    if (adminsWithPlainTextCodes.Any())
    {
        foreach (var admin in adminsWithPlainTextCodes)
        {
            var plainTextCode = admin.SecurityCodeHash;
            admin.SecurityCodeHash = BCrypt.Net.BCrypt.HashPassword(plainTextCode);
        }
        db.SaveChanges();
        Console.WriteLine($"✅ Migrated {adminsWithPlainTextCodes.Count} admin security code(s) to BCrypt hash");
    }
}

// 7. CONFIGURE PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 8. TURN ON AUTH
app.UseAuthentication(); // Enable Auth (includes SecurityStamp validation via OnValidatePrincipal)
app.UseAuthorization();

// 9. MAP ROUTES
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();