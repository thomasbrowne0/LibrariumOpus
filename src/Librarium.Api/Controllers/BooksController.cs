using Librarium.Api.Data;
using Librarium.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly LibrariumDbContext _context;

    public BooksController(LibrariumDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all books
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
    {
        var books = await _context.Books
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .Select(b => new BookDto
            {
                BookId = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                PublicationYear = b.PublicationYear,
                RetiredAt = b.RetiredAt,
                Authors = b.BookAuthors
                    .OrderBy(ba => ba.OrderIndex)
                    .Select(ba => new AuthorDto
                    {
                        AuthorId = ba.Author.Id,
                        FirstName = ba.Author.FirstName,
                        LastName = ba.Author.LastName,
                        Biography = ba.Author.Biography
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(books);
    }

    /// <summary>
    /// Soft delete a book (retire it)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RetireBook(int id)
    {
        // Use IgnoreQueryFilters to find even retired books
        var book = await _context.Books
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
        {
            return NotFound($"Book with ID {id} not found");
        }

        if (book.RetiredAt != null)
        {
            return BadRequest($"Book with ID {id} is already retired");
        }

        // Soft delete by setting RetiredAt timestamp
        book.RetiredAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get all books including retired ones
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooksIncludingRetired()
    {
        var books = await _context.Books
            .IgnoreQueryFilters()  // Bypass soft delete filter
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .Select(b => new BookDto
            {
                BookId = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                PublicationYear = b.PublicationYear,
                RetiredAt = b.RetiredAt,
                Authors = b.BookAuthors
                    .OrderBy(ba => ba.OrderIndex)
                    .Select(ba => new AuthorDto
                    {
                        AuthorId = ba.Author.Id,
                        FirstName = ba.Author.FirstName,
                        LastName = ba.Author.LastName,
                        Biography = ba.Author.Biography
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(books);
    }
}
