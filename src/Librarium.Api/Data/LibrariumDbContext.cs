using Librarium.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Librarium.Api.Data;

public class LibrariumDbContext : DbContext
{
    public LibrariumDbContext(DbContextOptions<LibrariumDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Loan> Loans { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<BookAuthor> BookAuthors { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Book entity
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ISBN).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PublicationYear).IsRequired();
        });

        // Configure Member entity
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            
            // Add unique index on Email
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Loan entity
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LoanDate).IsRequired();
            entity.Property(e => e.ReturnDate).IsRequired(false);

            // Configure relationships
            entity.HasOne(e => e.Book)
                .WithMany(b => b.Loans)
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Member)
                .WithMany(m => m.Loans)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Author entity
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Biography).HasMaxLength(2000);

            // Seed "Unknown Author" for existing books
            entity.HasData(new Author
            {
                Id = 1,
                FirstName = "Unknown",
                LastName = "",
                Biography = null
            });
        });

        // Configure BookAuthor entity (junction table)
        modelBuilder.Entity<BookAuthor>(entity =>
        {
            entity.HasKey(e => new { e.BookId, e.AuthorId });

            entity.Property(e => e.OrderIndex).HasDefaultValue(0);

            entity.HasOne(e => e.Book)
                .WithMany(b => b.BookAuthors)
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Author)
                .WithMany(a => a.BookAuthors)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
