using LibraryManagementAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Data

{

    public class LibraryContext : DbContext 
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }
        public DbSet? Books { get; set; }
        public DbSet? Authors { get; set; }
        public DbSet? Genres { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Genre)
                .WithMany(g => g.Books)
                .HasForeignKey(b => b.GenreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Book>()
                .HasQueryFilter(b => !b.IsDeleted);
            modelBuilder.Entity<Author>()
                .HasQueryFilter(a => !a.IsDeleted);
            modelBuilder.Entity<Genre>()
                .HasQueryFilter(g => !g.IsDeleted);

           
            var genreId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var authorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            // Seed Genre
            modelBuilder.Entity<Genre>().HasData(
                new Genre
                {
                    Id = genreId,
                    Name = "Fiction",
                    Description = "Literary works",
                    CreatedAt = DateTime.UtcNow,
                    Title = "Title",
                }
            );

            // Seed Author
            modelBuilder.Entity<Author>().HasData(
                new Author
                {
                    Id = authorId,
                    FirstName = "Adebayo",
                    LastName = "Adeeyo",
                    Bio = "novelist",
                    DateOfBirth = new DateTime(1990, 6, 25),
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed Book
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "1984",
                    ISBN = "9780451524935",
                    PublicationYear = 2025,
                    AuthorId = authorId,
                    GenreId = genreId,
                    CreatedAt = DateTime.UtcNow,
                    Description = "Description"
                }
            );
        }
    }
}

    