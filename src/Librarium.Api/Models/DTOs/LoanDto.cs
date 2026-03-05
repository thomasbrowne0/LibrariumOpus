namespace Librarium.Api.Models.DTOs;

public class LoanDto
{
    public int LoanId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }
}
