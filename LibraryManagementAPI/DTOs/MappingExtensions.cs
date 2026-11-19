using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.DTOs;

public static class MappingExtensions
{
    public static BookReadDto ToReadDto(this Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        ISBN = book.ISBN,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        AuthorId = book.AuthorId,
        AuthorName = book.Author is null ? string.Empty : $"{book.Author.FirstName} {book.Author.LastName}",
        GenreId = book.GenreId,
        GenreName = book.Genre?.Name ?? string.Empty,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        CreatedAt = book.CreatedAt,
        UpdatedAt = book.UpdatedAt
    };

    public static AuthorReadDto ToReadDto(this Author author) => FillAuthor(author, new AuthorReadDto());

    public static AuthorWithBooksDto ToWithBooksDto(this Author author)
    {
        var dto = FillAuthor(author, new AuthorWithBooksDto());
        dto.Books = author.Books.Select(b => b.ToReadDto()).ToList();
        return dto;
    }

    public static GenreReadDto ToReadDto(this Genre genre) => FillGenre(genre, new GenreReadDto());

    public static GenreWithBooksDto ToWithBooksDto(this Genre genre)
    {
        var dto = FillGenre(genre, new GenreWithBooksDto());
        dto.Books = genre.Books.Select(b => b.ToReadDto()).ToList();
        return dto;
    }

    private static T FillAuthor<T>(Author author, T dto) where T : AuthorReadDto
    {
        dto.Id = author.Id;
        dto.FirstName = author.FirstName;
        dto.LastName = author.LastName;
        dto.FullName = $"{author.FirstName} {author.LastName}";
        dto.Bio = author.Bio;
        dto.DateOfBirth = author.DateOfBirth;
        dto.BookCount = author.Books.Count;
        dto.CreatedAt = author.CreatedAt;
        dto.UpdatedAt = author.UpdatedAt;
        return dto;
    }

    private static T FillGenre<T>(Genre genre, T dto) where T : GenreReadDto
    {
        dto.Id = genre.Id;
        dto.Name = genre.Name;
        dto.Description = genre.Description;
        dto.BookCount = genre.Books.Count;
        dto.CreatedAt = genre.CreatedAt;
        dto.UpdatedAt = genre.UpdatedAt;
        return dto;
    }
}
