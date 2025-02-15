using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoginHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LoginHistory
        public async Task<IActionResult> Index()
        {
            var loginHistory = await _context.LoginHistories
                .Include(l => l.LibraryUser) // Зарежда информацията за потребителя
                .OrderByDescending(l => l.LoginTime)
                .ToListAsync();
            return View(loginHistory);
        }

        // GET: LoginHistory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loginHistory = await _context.LoginHistories
                .Include(m => m.LibraryUser) // Зарежда информацията за потребителя
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loginHistory == null)
            {
                return NotFound();
            }

            return View(loginHistory);
        }

        // GET: LoginHistory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loginHistory = await _context.LoginHistories
                .Include(l => l.LibraryUser) // Зарежда информацията за потребителя
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loginHistory == null)
            {
                return NotFound();
            }

            return View(loginHistory);
        }

        // POST: LoginHistory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loginHistory = await _context.LoginHistories.FindAsync(id);
            if (loginHistory != null)
            {
                _context.LoginHistories.Remove(loginHistory);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        // POST: LoginHistory/ClearHistory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearHistory()
        {
            _context.LoginHistories.RemoveRange(_context.LoginHistories);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
