using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OfficeNexus.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        public IActionResult Dashboard()
        {
            // Employee dashboard now shows tasks and profile info only
            // No visitor management data needed
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}