using IdentityApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers
{
    public class RolesController : Controller
    {
        private RoleManager<AppRole> _roleManager; 
        private UserManager<AppUser> _userManager;
        public RolesController(UserManager<AppUser> userManager,RoleManager<AppRole> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View(_roleManager.Roles);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(AppRole role)
        {
            if (ModelState.IsValid)
            {
                var result =await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                foreach (IdentityError err in result.Errors)
                {
                    ModelState.AddModelError("",err.Description);
                }
            }
           
            return View(role);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var role =await _roleManager.FindByIdAsync(id);

            if (role != null && role.Name!=null)
            {
                ViewBag.Users=await _userManager.GetUsersInRoleAsync(role.Name);
                return View(role);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Edit(AppRole model)
        {
            if (ModelState.IsValid)
            {
                var role =await _roleManager.FindByIdAsync(model.Id);
                if (role!=null)
                {
                    role.Name=model.Name;
                    var result = await _roleManager.UpdateAsync(role);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    foreach (IdentityError err in result.Errors)
                    {
                        ModelState.AddModelError("",err.Description);
                    }
                    if (role.Name!=null)
                    {
                        ViewBag.Users=await _userManager.GetUsersInRoleAsync(role.Name);
                    }
                }

            }
            return View(model);
        }
    }
}