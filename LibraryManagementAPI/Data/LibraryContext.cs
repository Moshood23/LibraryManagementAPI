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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(b =>
        {
            b.HasOne(x => x.Author).WithMany(a => a.Books).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Genre).WithMany(g => g.Books).HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.ISBN);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Author>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Genre>().HasQueryFilter(g => !g.IsDeleted);

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
