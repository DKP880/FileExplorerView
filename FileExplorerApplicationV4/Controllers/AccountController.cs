using FileExplorer.Models;
using FileExplorer.Services;
using Microsoft.AspNetCore.Mvc;
// No need for the BouncyCastle using statement
// using Org.BouncyCastle.Crypto.Generators; 

// The 'using' statement itself is correct
using BCrypt.Net;

namespace FileExplorer.Controllers
{
    public class AccountController : Controller
    {
        private readonly JsonDataService _dataService;

        public AccountController(JsonDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _dataService.GetUserByUsername(model.Username);
                if (user != null)
                {
                    // --- FIX IS ON THIS LINE ---
                    // The method call needs to be fully qualified like this:
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

                    if (isPasswordValid)
                    {
                        HttpContext.Session.SetString("Username", user.Username);
                        return RedirectToAction("Index", "Explorer");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

