using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddControllersWithViews();

// Database Connection (SQLite for easy demo, change connection string for SQL Server)
builder.Services.AddDbContext<OfficeDbContext>(options =>
    options.UseSqlite("Data Source=office_nexus.db"));

// Authentication with Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

var app = builder.Build();

// 2. Database Seeding (Ensure Admin Exists)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OfficeDbContext>();
    db.Database.EnsureCreated(); // Creates DB if not exists

    if (!db.Users.Any(u => u.Role == UserRole.Admin))
    {
        // CREATE DEFAULT ADMIN
        // Password is "admin123"
        string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        
        db.Users.Add(new User 
        { 
            FullName = "System Administrator", 
            Email = "admin@officenexus.com", 
            PasswordHash = passwordHash, 
            Role = UserRole.Admin 
        });
        db.SaveChanges();
    }
}

// 3. Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Enable Auth
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();