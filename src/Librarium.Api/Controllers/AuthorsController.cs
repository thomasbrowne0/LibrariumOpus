using Librarium.Api.Data;
using Librarium.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly LibrariumDbContext _context;

    public AuthorsController(LibrariumDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all authors
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors()
    {
        var authors = await _context.Authors
            .Where(a => a.Id != 1) // Exclude "Unknown" author from public listing
            .Select(a => new AuthorDto
            {
                AuthorId = a.Id,
                FirstName = a.FirstName,
                LastName = a.LastName,
                Biography = a.Biography
            })
            .ToListAsync();

        return Ok(authors);
    }

    /// <summary>
    /// Get a specific author by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AuthorDto>> GetAuthor(int id)
    {
        var author = await _context.Authors.FindAsync(id);

        if (author == null)
        {
            return NotFound($"Author with ID {id} not found");
        }

        var authorDto = new AuthorDto
        {
            AuthorId = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            Biography = author.Biography
        };

        return Ok(authorDto);
    }
}
