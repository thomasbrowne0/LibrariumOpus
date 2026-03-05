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
