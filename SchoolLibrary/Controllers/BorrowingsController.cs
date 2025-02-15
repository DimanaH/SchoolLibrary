using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;


namespace SchoolLibrary.Controllers
{
    public class BorrowingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<LibraryUser> _userManager; // Добавяме UserManager

        public BorrowingsController(ApplicationDbContext context, UserManager<LibraryUser> userManager)
        {
            _context = context;
            _userManager = userManager; // Инициализираме UserManager
        }

        // GET: Borrowings
        [Authorize]
        public async Task<IActionResult> Index(string searchString)
        {
            var userId = _userManager.GetUserId(User); // ID на текущия потребител
            var isAdmin = User.IsInRole("Admin"); // Проверка дали е администратор

            IQueryable<Borrowing> borrowingsQuery = _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.LibraryUser);

            if (!isAdmin) // Ако не е администратор, вижда само своите заемки
            {
                borrowingsQuery = borrowingsQuery.Where(b => b.LibraryUserId == userId);
            }

            var borrowingsList = await borrowingsQuery.ToListAsync(); //Зареждаме от базата преди да правим ToString()

            if (!string.IsNullOrEmpty(searchString))
            {
                borrowingsList = borrowingsList
                .Where(b =>
                    (b.Book != null && (
                        b.Book.Title?.Contains(searchString) == true ||
                        b.Book.Author?.Contains(searchString) == true ||
                        b.Book.InventoryNumber?.Contains(searchString) == true
                    )) ||
                    (b.LibraryUser != null && (
                        b.LibraryUser.FirstName?.Contains(searchString) == true ||
                        b.LibraryUser.LastName?.Contains(searchString) == true ||
                        b.LibraryUser.UserName?.Contains(searchString) == true
                    )) ||

                    //Проверяваме дали датите не са null преди да използваме ToString()
                    (b.BorrowDate != null && (
                        b.BorrowDate.ToString("d.M").Contains(searchString) ||
                        b.BorrowDate.ToString("dd.MM").Contains(searchString) ||
                        b.BorrowDate.ToString("dd.MM.yyyy").Contains(searchString)
                    )) ||
                    (b.DueDate != null && (
                        b.DueDate.ToString("d.M").Contains(searchString) ||
                        b.DueDate.ToString("dd.MM").Contains(searchString) ||
                        b.DueDate.ToString("dd.MM.yyyy").Contains(searchString)
                    )) ||
                    (b.ReturnDate.HasValue && (
                        b.ReturnDate.Value.ToString("d.M").Contains(searchString) ||
                        b.ReturnDate.Value.ToString("dd.MM").Contains(searchString) ||
                        b.ReturnDate.Value.ToString("dd.MM.yyyy").Contains(searchString)
                    ))
                )
                .ToList();
            }

            ViewData["IsAdmin"] = isAdmin; // Предаваме тази стойност към изгледа
            ViewData["SearchString"] = searchString; // За да запазим въведената стойност в изгледа
            return View(borrowingsList);
        }

        // GET: Borrowings/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Списък с налични книги
            ViewData["Books"] = new SelectList(_context.Books
                .Where(b => b.IsAvailable)
                .Select(b => new { b.Id, DisplayText = $"{b.Title} ({b.Author} {b.InventoryNumber})" })
                .ToList(), "Id", "DisplayText");

            ViewData["Users"] = new SelectList(_context.Users
                .Select(u => new { u.Id, DisplayText = $"{u.FirstName} {u.LastName} ({u.Email})" })
                .ToList(), "Id", "DisplayText");

            var books = _context.Books.Where(b => b.IsAvailable).ToList();
            Console.WriteLine($"Books count: {books.Count}");

            var users = _context.Users.ToList();
            Console.WriteLine($"Users count: {users.Count}");

            return View();
        }

        // POST: Borrowings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Borrowing borrowing)
        {
            // Премахваме Book и LibraryUser от ModelState, за да спрем тяхната ненужна валидация
            ModelState.Remove("Book");
            ModelState.Remove("LibraryUser");

            if (!ModelState.IsValid)
            {
                ViewData["Books"] = new SelectList(_context.Books
                    .Where(b => b.IsAvailable)
                    .Select(b => new { b.Id, DisplayText = $"{b.Title} ({b.Author} {b.InventoryNumber})" })
                    .ToList(), "Id", "DisplayText");

                ViewData["Users"] = new SelectList(_context.Users
                    .Select(u => new { u.Id, DisplayText = $"{u.FirstName} {u.LastName} ({u.Email})" })
                    .ToList(), "Id", "DisplayText");

                return View(borrowing);
            }

            // Зареждаме навигационните свойства
            borrowing.Book = await _context.Books.FindAsync(borrowing.BookId);
            borrowing.LibraryUser = await _context.Users.FindAsync(borrowing.LibraryUserId);

            if (borrowing.Book == null || !borrowing.Book.IsAvailable)
            {
                ModelState.AddModelError("BookId", "Тази книга вече е заета.");
                return View(borrowing);
            }

            // Обновяваме състоянието на книгата
            borrowing.Book.IsAvailable = false;
            _context.Update(borrowing.Book);

            _context.Add(borrowing);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var borrowing = await _context.Borrowings
                .Include(b => b.Book) // Зареждаме книгата заедно със заемката
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            borrowing.ReturnDate = DateTime.Now; // Маркираме като върната
            borrowing.Book.IsAvailable = true;  // Правим книгата налична

            _context.Update(borrowing);
            _context.Update(borrowing.Book);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(m => m.LibraryUser) // Зарежда информацията за потребителя
                .Include(m => m.Book) // Зарежда информацията за книгата
                .FirstOrDefaultAsync(m => m.Id == id);
            if (borrowing == null)
            {
                return NotFound();
            }

            return View(borrowing);
        }

        // GET: Borrowings/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(m => m.LibraryUser) // Зарежда информацията за потребителя
                .Include(m => m.Book) // Зарежда информацията за книгата
                .FirstOrDefaultAsync(m => m.Id == id);
            if (borrowing == null)
            {
                return NotFound();
            }

            return View(borrowing);
        }
        // POST: Borrowings/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrowing = await _context.Borrowings.FindAsync(id);
            if (borrowing != null)
            {
                _context.Borrowings.Remove(borrowing);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BorrowingExists(int id)
        {
            return _context.Borrowings.Any(e => e.Id == id);
        }


        // GET: Borrowings/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.LibraryUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrowing == null)
            {
                return NotFound();
            }

            // Зареждаме списъка с книги (всички книги, дори заети)
            ViewData["Books"] = new SelectList(_context.Books
                .Select(b => new { b.Id, DisplayText = $"{b.Title} ({b.Author}, {b.InventoryNumber})" })
                .ToList(), "Id", "DisplayText", borrowing.BookId);

            // Зареждаме списъка с потребители
            ViewData["Users"] = new SelectList(_context.Users
                .Select(u => new { u.Id, DisplayText = $"{u.Email} ({u.FirstName} {u.LastName})" })
                .ToList(), "Id", "DisplayText", borrowing.LibraryUserId);

            return View(borrowing);
        }


        // POST: Borrowing/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Borrowing borrowing)
        {
            if (id != borrowing.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Book");
            ModelState.Remove("LibraryUser");

            if (!ModelState.IsValid)
            {
                // Презареждаме ViewData списъците, за да не загубим селекциите
                ViewData["Books"] = new SelectList(_context.Books
                    .Select(b => new { b.Id, DisplayText = $"{b.Title} ({b.Author}, {b.InventoryNumber})" })
                    .ToList(), "Id", "DisplayText", borrowing.BookId);

                ViewData["Users"] = new SelectList(_context.Users
                    .Select(u => new { u.Id, DisplayText = $"{u.Email} ({u.FirstName} {u.LastName})" })
                    .ToList(), "Id", "DisplayText", borrowing.LibraryUserId);

                return View(borrowing);
            }

            try
            {
                _context.Update(borrowing);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BorrowingExists(borrowing.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
