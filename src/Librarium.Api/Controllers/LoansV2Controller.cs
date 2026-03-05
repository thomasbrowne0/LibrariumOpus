using Librarium.Api.Data;
using Librarium.Api.Models.DTOs;
using Librarium.Api.Models.Entities;
using Librarium.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/v2/loans")]
public class LoansV2Controller : ControllerBase
{
    private readonly LibrariumDbContext _context;

    public LoansV2Controller(LibrariumDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new loan (API v2)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LoanDtoV2>> CreateLoan([FromBody] CreateLoanRequest request)
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
            ReturnDate = null,
            Status = LoanStatus.Active
        };

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        var loanDto = new LoanDtoV2
        {
            LoanId = loan.Id,
            BookTitle = book.Title,
            LoanDate = loan.LoanDate,
            ReturnDate = loan.ReturnDate,
            Status = loan.Status
        };

        return CreatedAtAction(nameof(GetLoansByMember), new { memberId = loan.MemberId }, loanDto);
    }

    /// <summary>
    /// Get all loans for a member (API v2)
    /// </summary>
    [HttpGet("{memberId}")]
    public async Task<ActionResult<IEnumerable<LoanDtoV2>>> GetLoansByMember(int memberId)
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
            .Select(l => new LoanDtoV2
            {
                LoanId = l.Id,
                BookTitle = l.Book.Title,
                LoanDate = l.LoanDate,
                ReturnDate = l.ReturnDate,
                Status = l.Status
            })
            .ToListAsync();

        return Ok(loans);
    }
}
