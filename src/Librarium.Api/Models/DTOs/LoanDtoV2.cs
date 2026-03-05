using Librarium.Api.Models.Enums;

namespace Librarium.Api.Models.DTOs;

public class LoanDtoV2
{
    public int LoanId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }
}
