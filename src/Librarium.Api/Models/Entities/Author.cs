namespace Librarium.Api.Models.Entities;

public class Author
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Biography { get; set; }

    // Navigation property
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
