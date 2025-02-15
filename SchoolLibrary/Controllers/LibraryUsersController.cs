using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary.Controllers
{
    [Authorize]
    public class LibraryUsersController : Controller
    {
        private readonly UserManager<LibraryUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public LibraryUsersController(UserManager<LibraryUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString)
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            // Филтрираме след като вече сме събрали ролите
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();

                users = users.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchString)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchString)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchString)) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(searchString)) ||
                    (u.BirthDate != null && (
                        u.BirthDate.ToString("d.M").Contains(searchString) ||
                        u.BirthDate.ToString("dd.MM").Contains(searchString) ||
                        u.BirthDate.ToString("dd.MM.yyyy").Contains(searchString)
                    )) ||
                    (u.RegistrationDate != null && (
                        u.RegistrationDate.ToString("d.M").Contains(searchString) ||
                        u.RegistrationDate.ToString("dd.MM").Contains(searchString) ||
                        u.RegistrationDate.ToString("dd.MM.yyyy").Contains(searchString)
                    )) ||
                    (userRoles[u.Id].ToLower().Contains(searchString)) // Филтриране по роля
                ).ToList();
            }

            ViewData["UserRoles"] = userRoles;
            ViewData["SearchString"] = searchString;

            return View(users);
        }

        // GET: Users/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["UserRole"] = roles.FirstOrDefault() ?? "No Role";

            return View(user);
        }


        // GET: Users/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(LibraryUser user, string password, string confirmPassword, string role)
        {
            if (string.IsNullOrWhiteSpace(password) || password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(user);
            }

            // Вземаме правилата за пароли от настройките на ASP.NET Core Identity
            var passwordValidator = HttpContext.RequestServices.GetRequiredService<IPasswordValidator<LibraryUser>>();
            var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, user, password);

            if (!passwordValidationResult.Succeeded)
            {
                foreach (var error in passwordValidationResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(user);
            }

            if (ModelState.IsValid)
            {
                user.UserName = user.Email;

                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Email is already in use.");
                    return View(user);
                }

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(role) && await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(user);
        }


        // GET: Users/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["UserRole"] = roles.FirstOrDefault() ?? "No Role";

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["UserRole"] = roles.FirstOrDefault() ?? "Student"; // Запазваме текущата роля

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, LibraryUser model, string? password, string? confirmPassword, string role)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Актуализираме другите полета
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Phone = model.Phone;
            user.BirthDate = model.BirthDate;
            user.RegistrationDate = model.RegistrationDate;

            // Промяна на роля (ако е подадена)
            if (!string.IsNullOrEmpty(role) && await _roleManager.RoleExistsAsync(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles); // Премахваме старите роли
                await _userManager.AddToRoleAsync(user, role); // Добавяме новата роля
            }

            // Промяна на парола (ако е въведена)
            if (!string.IsNullOrWhiteSpace(password))
            {
                if (password != confirmPassword)
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    ViewData["UserRole"] = role; // Запазваме роля при грешка
                    return View(model);
                }

                var passwordValidator = HttpContext.RequestServices.GetRequiredService<IPasswordValidator<LibraryUser>>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, user, password);

                if (!passwordValidationResult.Succeeded)
                {
                    foreach (var error in passwordValidationResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewData["UserRole"] = role; // Запазваме роля при грешка
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, password);

                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewData["UserRole"] = role; // Запазваме роля при грешка
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewData["UserRole"] = role; // Запазваме роля при грешка
            return View(model);
        }

        [Authorize] // Само логнати потребители имат достъп
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return View(user); // Пращаме данните към изгледа
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string id, string? password, string? confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || id != user.Id)
            {
                return NotFound();
            }

            // Ако полетата за парола са празни – не променяме нищо
            if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(confirmPassword))
            {
                return RedirectToAction(nameof(Profile));
            }

            // Проверяваме дали паролите съвпадат
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(user);
            }

            // Проверяваме паролата по правилата на ASP.NET Identity
            var passwordValidator = HttpContext.RequestServices.GetRequiredService<IPasswordValidator<LibraryUser>>();
            var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, user, password);

            if (!passwordValidationResult.Succeeded)
            {
                foreach (var error in passwordValidationResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(user);
            }

            // Смяна на паролата
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, password);

            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(user);
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(Profile));
        }
    }
}

