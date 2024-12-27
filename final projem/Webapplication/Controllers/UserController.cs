using Webapplication.Models;
using Webapplication.ViewModel;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Webapplication.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly INotyfService _notyfService;

        public UserController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<AppRole> roleManager, INotyfService notyfService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _notyfService = notyfService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    
                    var role = await _userManager.GetRolesAsync(user);

                    if (result.Succeeded)
                    {
                        if(role.Contains("Admin"))
                        {
                            _notyfService.Success("Başarıyla giriş yaptınız!");
                            return RedirectToAction("Index", "Admin");
                        }

                        _notyfService.Success("Başarıyla giriş yaptınız!");
                        return RedirectToAction("Index", "Home");
                    }
                }
                _notyfService.Error("Email veya şifre hatalı!");
                ModelState.AddModelError("", "Email veya şifre hatalı!");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    StorageLimit = 5*1024,
                    UsedStorage = 0,
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Rol kontrolü ve atama
                    if (!await _roleManager.RoleExistsAsync("Kullanici"))
                    {
                        await _roleManager.CreateAsync(new AppRole { Name = "Kullanici" });
                    }
                    
                    await _userManager.AddToRoleAsync(user, "Kullanici");
                    
                    _notyfService.Information("Başarıyla kayıt oldunuz!");
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    _notyfService.Error(error.Description);
                }
            }
            return View(model);
        }
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _notyfService.Success("Başarıyla çıkış yaptınız!");
            return RedirectToAction("Login");
        }
    }
}
