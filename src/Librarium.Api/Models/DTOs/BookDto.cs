namespace Librarium.Api.Models.DTOs;

public class BookDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public IEnumerable<AuthorDto> Authors { get; set; } = new List<AuthorDto>();
}
