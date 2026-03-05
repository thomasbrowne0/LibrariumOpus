namespace Librarium.Api.Models.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublicationYear { get; set; }

    // Navigation properties
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
