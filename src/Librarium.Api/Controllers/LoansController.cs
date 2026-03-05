using Librarium.Api.Data;
using Librarium.Api.Models.DTOs;
using Librarium.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly LibrariumDbContext _context;

    public LoansController(LibrariumDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new loan
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LoanDto>> CreateLoan([FromBody] CreateLoanRequest request)
    {
        // Validate that book exists
        var book = await _context.Books.FindAsync(request.BookId);
        if (book == null)
        {
            return NotFound($"Book with ID {request.BookId} not found");
        }

        // Validate that member exists
        var member = await _context.Members.FindAsync(request.MemberId);
        if (member == null)
        {
            return NotFound($"Member with ID {request.MemberId} not found");
        }

        var loan = new Loan
        {
            BookId = request.BookId,
            MemberId = request.MemberId,
            LoanDate = request.LoanDate ?? DateTime.UtcNow,
            ReturnDate = null
        };

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        var loanDto = new LoanDto
        {
            LoanId = loan.Id,
            BookTitle = book.Title,
            LoanDate = loan.LoanDate,
            ReturnDate = loan.ReturnDate
        };

        return CreatedAtAction(nameof(GetLoansByMember), new { memberId = loan.MemberId }, loanDto);
    }

    /// <summary>
    /// Get all loans for a member
    /// </summary>
    [HttpGet("{memberId}")]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoansByMember(int memberId)
    {
        // Validate that member exists
        var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);
        if (!memberExists)
        {
            return NotFound($"Member with ID {memberId} not found");
        }

        var loans = await _context.Loans
            .Include(l => l.Book)
            .Where(l => l.MemberId == memberId)
            .Select(l => new LoanDto
            {
                LoanId = l.Id,
                BookTitle = l.Book.Title,
                LoanDate = l.LoanDate,
                ReturnDate = l.ReturnDate
            })
            .ToListAsync();

        return Ok(loans);
    }
}
