# OfficeNexus - Office Automation System

A modern, full-featured Office Automation System built with ASP.NET Core MVC (.NET 9), featuring visitor management, employee management, enhanced authentication, and more.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

---

## 📋 Table of Contents

- [Features](#-features)
- [Prerequisites](#-prerequisites)
- [Installation & Setup](#-installation--setup)
- [Running the Application](#-running-the-application)
- [Default Credentials](#-default-credentials)
- [Project Structure](#-project-structure)
- [Technologies Used](#-technologies-used)
- [Database Migrations](#-database-migrations)
- [Troubleshooting](#-troubleshooting)

---

## ✨ Features

### Current Features (Module 1 Complete)

- **Enhanced Authentication System**
  - Two-factor authentication for Admin users (Password + Security Code)
  - Standard password authentication for Employees
  - Role-based access control (Admin/Employee)

- **Employee Management** (Admin Only)
  - Add, view, and manage employees
  - Track employee details: Job Title, Department, Basic Salary
  - Search and filter employees

- **Visitor Management** (Admin Only)
  - Log visitors with employee assignment
  - Track visitor types (Outsider, Internal, Interview, Delivery, Contractor)
  - Check-in/Check-out functionality
  - Real-time visitor status tracking
  - Search and filter visitors

- **Dashboard Analytics**
  - Admin dashboard with system-wide statistics
  - Employee dashboard with personal metrics
  - Visual charts using Chart.js

- **Modern UI/UX**
  - Responsive design with Tailwind CSS
  - Interactive components with Alpine.js
  - Gradient themes and smooth animations
  - Mobile-friendly interface

---

## 🔧 Prerequisites

Before you begin, ensure you have the following installed on your system:

### Required Software

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (or later)
   - Download and install from Microsoft's official website
   - Verify installation:
     ```bash
     dotnet --version
     ```
     Should output: `9.0.x` or higher

2. **Git** (for cloning the repository)
   - Download from [git-scm.com](https://git-scm.com/)
   - Verify installation:
     ```bash
     git --version
     ```

3. **Code Editor** (Choose one)
   - [Visual Studio 2022](https://visualstudio.microsoft.com/) (Recommended for Windows)
   - [Visual Studio Code](https://code.visualstudio.com/) (Cross-platform)
   - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Optional Tools

- **SQLite Browser** - To view/edit the database directly
  - Download from [sqlitebrowser.org](https://sqlitebrowser.org/)

---

## 📥 Installation & Setup

### Step 1: Clone the Repository

```bash
# Clone the repository
git clone <your-repository-url>

# Navigate to the project directory
cd OfficeNexus
```

**OR** if you downloaded a ZIP file:

1. Extract the ZIP file to your desired location
2. Open a terminal/command prompt
3. Navigate to the extracted folder:
   ```bash
   cd path/to/OfficeNexus
   ```

### Step 2: Restore Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

This will download all required packages:
- ASP.NET Core MVC
- Entity Framework Core
- SQLite Provider
- BCrypt.Net (for password hashing)

### Step 3: Build the Project

```bash
# Build the project
dotnet build
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

If you see any errors, check the [Troubleshooting](#-troubleshooting) section.

### Step 4: Apply Database Migrations

The project uses Entity Framework Core with SQLite. The database will be created automatically on first run, but you can also create it manually:

```bash
# Apply migrations to create the database
dotnet ef database update
```

This will:
- Create `office_nexus.db` in the project root
- Create all necessary tables (Users, VisitorLogs)
- Seed the default Admin account

**Note:** If you don't have `dotnet ef` tools installed:
```bash
dotnet tool install --global dotnet-ef
```

---

## 🚀 Running the Application

### Option 1: Using Command Line

```bash
# Run the application
dotnet run
```

**Expected Output:**
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Option 2: Using Visual Studio

1. Open `OfficeNexus.sln` in Visual Studio
2. Press `F5` or click the "Run" button
3. The application will open in your default browser

### Option 3: Using Visual Studio Code

1. Open the project folder in VS Code
2. Press `F5` or use the "Run and Debug" panel
3. Select ".NET Core Launch (web)"

### Accessing the Application

Once running, open your browser and navigate to:
- **HTTPS:** `https://localhost:5001`
- **HTTP:** `http://localhost:5000`

You should see the **Login Page**.

---

## 🔐 Default Credentials

### Admin Account

```
Email:         admin@officenexus.com
Password:      admin123
Security Code: ADMIN2024
```

**Login Flow for Admin:**
1. Enter email and password
2. Click "Sign in"
3. A Security Code field will appear
4. Enter the security code
5. Click "Sign in" again

### Employee Account

No default employee accounts exist. You must create them through the Admin portal:

1. Login as Admin
2. Navigate to "Employee Management"
3. Click "Add Employee"
4. Fill in the details:
   - Full Name
   - Email
   - Job Title
   - Department
   - Basic Salary
   - Password

**Employee Login:**
- Employees only need email and password (no security code)

---

## 📁 Project Structure

```
OfficeNexus/
├── Controllers/
│   ├── AdminController.cs       # Admin-specific actions
│   ├── AuthController.cs        # Authentication logic
│   └── EmployeeController.cs    # Employee-specific actions
├── Data/
│   └── OfficeDbContext.cs       # Database context & models
├── Views/
│   ├── Admin/                   # Admin portal views
│   │   ├── Index.cshtml         # Admin dashboard
│   │   ├── EmployeeManagement.cshtml
│   │   ├── VisitorManagement.cshtml
│   │   └── VisitorLogs.cshtml
│   ├── Employee/                # Employee portal views
│   │   ├── Dashboard.cshtml     # Employee dashboard
│   │   └── Profile.cshtml
│   ├── Auth/
│   │   └── Login.cshtml         # Login page
│   └── Shared/
│       ├── _AdminLayout.cshtml  # Admin layout template
│       └── _EmployeeLayout.cshtml # Employee layout template
├── Migrations/                  # EF Core migrations
├── wwwroot/                     # Static files (if any)
├── Program.cs                   # Application entry point
├── appsettings.json            # Configuration
└── office_nexus.db             # SQLite database (created on first run)
```

---

## 🛠 Technologies Used

### Backend
- **ASP.NET Core MVC 9.0** - Web framework
- **Entity Framework Core** - ORM for database operations
- **SQLite** - Lightweight database
- **BCrypt.Net** - Password hashing

### Frontend
- **Tailwind CSS** - Utility-first CSS framework (via CDN)
- **Alpine.js** - Lightweight JavaScript framework (via CDN)
- **Chart.js** - Data visualization (via CDN)
- **Heroicons** - Icon library

### Architecture
- **MVC Pattern** - Model-View-Controller
- **Repository Pattern** - Data access through DbContext
- **Cookie-based Authentication** - Session management

---

## 🗄 Database Migrations

### Understanding Migrations

The project uses Entity Framework Core migrations to manage database schema changes.

### Current Migration

- **EnhancedAuthenticationModule** - Adds BasicSalary, SecurityCode, JobTitle, Department to User model

### Creating New Migrations

If you make changes to the models in `OfficeDbContext.cs`:

```bash
# Create a new migration
dotnet ef migrations add YourMigrationName

# Apply the migration
dotnet ef database update
```

### Rolling Back Migrations

```bash
# Remove the last migration (if not applied)
dotnet ef migrations remove

# Revert to a specific migration
dotnet ef database update PreviousMigrationName
```

### Resetting the Database

If you want to start fresh:

```bash
# Delete the database file
rm office_nexus.db
rm office_nexus.db-shm
rm office_nexus.db-wal

# Recreate the database
dotnet ef database update
```

**Note:** This will delete all data!

---

## 🐛 Troubleshooting

### Issue: "dotnet: command not found"

**Solution:** .NET SDK is not installed or not in PATH
- Download and install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Restart your terminal after installation

### Issue: "Build failed" errors

**Solution:** Check for missing dependencies
```bash
dotnet restore
dotnet build
```

### Issue: "dotnet ef: command not found"

**Solution:** Install EF Core tools globally
```bash
dotnet tool install --global dotnet-ef
```

### Issue: Database errors on startup

**Solution:** Delete and recreate the database
```bash
rm office_nexus.db
dotnet ef database update
```

### Issue: Port already in use

**Solution:** Change the port in `Properties/launchSettings.json` or kill the process using the port

**Windows:**
```bash
netstat -ano | findstr :5001
taskkill /PID <process_id> /F
```

**Linux/Mac:**
```bash
lsof -ti:5001 | xargs kill -9
```

### Issue: "Cannot access a disposed object" error

**Solution:** This usually happens with DbContext. Restart the application:
```bash
# Stop the app (Ctrl+C)
# Run again
dotnet run
```

### Issue: Login not working

**Checklist:**
1. ✅ Database created? Check if `office_nexus.db` exists
2. ✅ Using correct credentials? See [Default Credentials](#-default-credentials)
3. ✅ Admin users: Did you enter the Security Code?
4. ✅ Check browser console for JavaScript errors

### Issue: Visitor Management not showing

**Solution:** This feature is Admin-only. Make sure you're logged in as Admin, not Employee.

---

## 📝 Additional Notes

### Security Considerations

⚠️ **Important for Production:**

1. **Change Default Credentials** - Update the admin password and security code
2. **Use HTTPS** - Enable SSL certificates for production
3. **Environment Variables** - Store sensitive data in environment variables
4. **Database** - Consider using SQL Server or PostgreSQL for production
5. **Password Policy** - Implement stronger password requirements

### Future Modules (Planned)

- ✅ Module 1: Enhanced Authentication (Complete)
- 🔄 Module 2: Task Management System
- 🔄 Module 3: Leave Management
- 🔄 Module 4: Attendance & Payroll System
- 🔄 Module 5: Complaint Box

---

## 📄 License

This project is licensed under the MIT License.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## 📧 Support

If you encounter any issues not covered in this guide, please:
1. Check the [Troubleshooting](#-troubleshooting) section
2. Review the code comments in the project
3. Open an issue on the repository

---

## 🎉 Quick Start Summary

```bash
# 1. Clone the repository
git clone <your-repo-url>
cd OfficeNexus

# 2. Restore dependencies
dotnet restore

# 3. Build the project
dotnet build

# 4. Run the application
dotnet run

# 5. Open browser to https://localhost:5001

# 6. Login with:
#    Email: admin@officenexus.com
#    Password: admin123
#    Security Code: ADMIN2024
```

**That's it! You're ready to use OfficeNexus! 🚀**

---

*Last Updated: November 2025*
