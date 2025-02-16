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
        public async Task<IActionResult> Index(string searchString, string statusFilter = "all")
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            IQueryable<Borrowing> borrowingsQuery = _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.LibraryUser);

            if (!isAdmin)
            {
                borrowingsQuery = borrowingsQuery.Where(b => b.LibraryUserId == userId);
            }

            // 📌 Филтриране по статус (върнати / невърнати)
            if (statusFilter == "returned")
            {
                borrowingsQuery = borrowingsQuery.Where(b => b.ReturnDate != null);
            }
            else if (statusFilter == "notReturned")
            {
                borrowingsQuery = borrowingsQuery.Where(b => b.ReturnDate == null);
            }

            var borrowingsList = await borrowingsQuery.ToListAsync();

            // 📌 Търсене в резултатите
            if (!string.IsNullOrEmpty(searchString))
            {
                borrowingsList = borrowingsList
                    .Where(b =>
                        (b.Book != null && (
                            b.Book.Title?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                            b.Book.Author?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                            b.Book.InventoryNumber?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                        )) ||
                        (b.LibraryUser != null && (
                            b.LibraryUser.FirstName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                            b.LibraryUser.LastName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true ||
                            b.LibraryUser.UserName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                        )) ||
                        (b.BorrowDate != null && b.BorrowDate.ToString("dd.MM.yyyy").Contains(searchString)) ||
                        (b.DueDate != null && b.DueDate.ToString("dd.MM.yyyy").Contains(searchString)) ||
                        (b.ReturnDate.HasValue && b.ReturnDate.Value.ToString("dd.MM.yyyy").Contains(searchString))
                    )
                    .ToList();
            }

            // 📌 Запазваме избраните стойности за филтрите
            ViewData["IsAdmin"] = isAdmin;
            ViewData["SearchString"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

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
            ModelState.Remove("Book");
            ModelState.Remove("LibraryUser");

            // Проверяваме дали книгата съществува
            var book = await _context.Books.FindAsync(borrowing.BookId);
            if (book == null)
            {
                ModelState.AddModelError("BookId", "Избраната книга не съществува.");
            }
            else if (!book.IsAvailable)
            {
                ModelState.AddModelError("BookId", "Тази книга вече е заета.");
            }

            // Проверяваме дали потребителят съществува
            var user = await _context.Users.FindAsync(borrowing.LibraryUserId);
            if (user == null)
            {
                ModelState.AddModelError("LibraryUserId", "Избраният потребител не съществува.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Books"] = new SelectList(_context.Books
                    .Where(b => b.IsAvailable)
                    .Select(b => new { b.Id, DisplayText = $"{b.Title} ({b.Author} {b.InventoryNumber})" })
                    .ToList(), "Id", "DisplayText", borrowing.BookId); // Избира последно въведената книга

                ViewData["Users"] = new SelectList(_context.Users
                    .Select(u => new { u.Id, DisplayText = $"{u.FirstName} {u.LastName} ({u.Email})" })
                    .ToList(), "Id", "DisplayText", borrowing.LibraryUserId); // Избира последния въведен потребител

                ViewData["EnteredBook"] = _context.Books.Where(b => b.Id == borrowing.BookId)
                    .Select(b => $"{b.Title} ({b.Author} {b.InventoryNumber})").FirstOrDefault();

                ViewData["EnteredUser"] = _context.Users.Where(u => u.Id == borrowing.LibraryUserId)
                    .Select(u => $"{u.FirstName} {u.LastName} ({u.Email})").FirstOrDefault();

                return View(borrowing);
            }


            // Маркираме книгата като заета
            book.IsAvailable = false;
            _context.Update(book);

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

            // Запазване на текущо избраните текстове за книгата и потребителя
            ViewData["SelectedBookText"] = borrowing.Book != null ? $"{borrowing.Book.Title} ({borrowing.Book.Author}, {borrowing.Book.InventoryNumber})" : "";
            ViewData["SelectedUserText"] = borrowing.LibraryUser != null ? $"{borrowing.LibraryUser.FirstName} {borrowing.LibraryUser.LastName} ({borrowing.LibraryUser.Email})" : "";

            ViewData["Books"] = _context.Books
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.Title} ({b.Author}, {b.InventoryNumber})"
                }).ToList();

            ViewData["Users"] = _context.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                }).ToList();

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
                ViewData["SelectedBookText"] = _context.Books
                    .Where(b => b.Id == borrowing.BookId)
                    .Select(b => $"{b.Title} ({b.Author}, {b.InventoryNumber})").FirstOrDefault();

                ViewData["SelectedUserText"] = _context.Users
                    .Where(u => u.Id == borrowing.LibraryUserId)
                    .Select(u => $"{u.FirstName} {u.LastName} ({u.Email})").FirstOrDefault();

                ViewData["Books"] = _context.Books
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = $"{b.Title} ({b.Author}, {b.InventoryNumber})"
                    }).ToList();

                ViewData["Users"] = _context.Users
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                    }).ToList();

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
