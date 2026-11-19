using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Data;

public class LibraryContext : IdentityDbContext<ApplicationUser>
{
    public static readonly Guid SeedGenreId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeedAuthorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SeedBookId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(b =>
        {
            b.HasOne(x => x.Author).WithMany(a => a.Books).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Genre).WithMany(g => g.Books).HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict);
            // Unique among live books; a soft-deleted book doesn't block re-adding the same ISBN.
            b.HasIndex(x => x.ISBN).IsUnique().HasFilter("[IsDeleted] = 0");
            b.HasQueryFilter(x => !x.IsDeleted);

            // Two people borrowing the last copy at the same moment: the second save fails
            // with a concurrency error instead of driving the count below zero.
            b.Property(x => x.AvailableCopies).IsConcurrencyToken();
            b.ToTable(t => t.HasCheckConstraint(
                "CK_Books_Copies", "[TotalCopies] >= 0 AND [AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]"));
        });

        modelBuilder.Entity<Loan>(l =>
        {
            l.HasOne(x => x.Book).WithMany(b => b.Loans).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
            l.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            l.HasIndex(x => new { x.UserId, x.ReturnedAt });
            l.HasIndex(x => new { x.BookId, x.ReturnedAt });
            l.HasIndex(x => x.DueAt);
            l.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Author>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Genre>(g =>
        {
            g.HasIndex(x => x.Name).IsUnique().HasFilter("[IsDeleted] = 0");
            g.HasQueryFilter(x => !x.IsDeleted);
        });

        // Seed data must use fixed values so EF doesn't see a model change on every build.
        var seededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Genre>().HasData(new Genre
        {
            Id = SeedGenreId,
            Name = "Fiction",
            Description = "Literary works",
            CreatedAt = seededAt
        });

        modelBuilder.Entity<Author>().HasData(new Author
        {
            Id = SeedAuthorId,
            FirstName = "Adebayo",
            LastName = "Adeeyo",
            Bio = "Novelist",
            DateOfBirth = new DateTime(1990, 6, 25, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = seededAt
        });

        modelBuilder.Entity<Book>().HasData(new Book
        {
            Id = SeedBookId,
            Title = "1984",
            ISBN = "9780451524935",
            PublicationYear = 1949,
            Description = "A dystopian novel.",
            AuthorId = SeedAuthorId,
            GenreId = SeedGenreId,
            TotalCopies = 3,
            AvailableCopies = 3,
            CreatedAt = seededAt
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
