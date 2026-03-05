namespace Librarium.Api.Models.Entities;

public class Member
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
