using Librarium.Api.Data;
using Librarium.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly LibrariumDbContext _context;

    public MembersController(LibrariumDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all members
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetMembers()
    {
        var members = await _context.Members
            .Select(m => new MemberDto
            {
                MemberId = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                PhoneNumber = m.PhoneNumber
            })
            .ToListAsync();

        return Ok(members);
    }
}
