using System.ComponentModel.DataAnnotations;

namespace Librarium.Api.Models.DTOs;

public class CreateLoanRequest
{
    [Required]
    public int BookId { get; set; }
    
    [Required]
    public int MemberId { get; set; }
    
    public DateTime? LoanDate { get; set; }
}
