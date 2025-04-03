using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StockPortfolioApp.Models;
using StockPortfolioApp.Models.ViewModels;
using System.Threading.Tasks;

namespace StockPortfolioApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        //Triggered when user accesses login page and makes sure user is logged out when accessing login page
        public IActionResult Login(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        //Triggered when user logs in and checks credentials, adds any errors to model state
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            try
            {
                returnUrl ??= Url.Content("~/");
                ViewData["ReturnUrl"] = returnUrl;
                _logger.LogInformation("returnUrl: " + returnUrl);
            
                if (ModelState.IsValid)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email, 
                        model.Password, 
                        model.RememberMe, 
                        lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        return LocalRedirect(returnUrl);
                    }

                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex.ToString());
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Exception while login.");
            }

            return View(model);
        }

        [HttpGet]
        //Triggered when user tries to access manage page, transfers info to AccountManageViewModel
        public IActionResult Manage()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = _userManager.GetUserAsync(User).Result;
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new AccountManageViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

    }
}