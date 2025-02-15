using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SchoolLibrary.Models;
using SchoolLibrary.Data;

namespace SchoolLibrary.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<LibraryUser> _signInManager;
        private readonly UserManager<LibraryUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<LibraryUser> signInManager,
                           UserManager<LibraryUser> userManager,
                           ApplicationDbContext context,
                           ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Намираме последния запис за логване на този потребител
                var loginHistory = _context.LoginHistories
                    .Where(l => l.LibraryUserId == user.Id && l.LogoutTime == null)
                    .OrderByDescending(l => l.LoginTime)
                    .FirstOrDefault();

                if (loginHistory != null)
                {
                    loginHistory.LogoutTime = DateTime.UtcNow;
                    _context.Update(loginHistory);
                    await _context.SaveChangesAsync();
                }
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
