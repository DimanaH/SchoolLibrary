using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolLibrary.Data;
using SchoolLibrary.Models;

namespace SchoolLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> Index(string searchString, string availabilityFilter)
        {
            IQueryable<Book> booksQuery = _context.Books;

            var booksList = await booksQuery.ToListAsync(); // Зареждаме всички книги от базата

            // Приложение на филтъра за наличност
            if (!string.IsNullOrEmpty(availabilityFilter))
            {
                switch (availabilityFilter)
                {
                    case "available":
                        booksList = booksList.Where(b => b.IsAvailable).ToList();
                        break;
                    case "borrowed":
                        booksList = booksList.Where(b => !b.IsAvailable).ToList();
                        break;
                }
            }

            // Филтриране по търсената дума
            if (!string.IsNullOrEmpty(searchString))
            {
                booksList = booksList
                    .Where(b =>
                        b.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        b.Author.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        b.ISBN.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        b.Genre.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        b.Publisher.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        b.InventoryNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        b.PublicationYear.ToString().Contains(searchString) ||
                        (b.DateAdded != null && (
                            b.DateAdded.ToString("d.M").Contains(searchString) ||
                            b.DateAdded.ToString("dd.MM").Contains(searchString) ||
                            b.DateAdded.ToString("dd.MM.yyyy").Contains(searchString)
                        )) ||
                        b.Price.ToString().Contains(searchString)
                    )
                    .ToList();
            }

            ViewData["SearchString"] = searchString;
            ViewData["AvailabilityFilter"] = availabilityFilter; // Запазваме избора на филтъра

            return View(booksList);
        }



        // GET: Books/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("InventoryNumber,DateAdded,Title,Author,ISBN,Genre,Publisher,PublicationYear,Price,IsAvailable")] Book book)
        {
            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InventoryNumber,DateAdded,Title,Author,ISBN,Genre,Publisher,PublicationYear,Price,IsAvailable")] Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
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
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }
    }
}
