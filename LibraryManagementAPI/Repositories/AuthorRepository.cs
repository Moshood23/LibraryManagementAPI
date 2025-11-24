using System.Linq.Expressions;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.Model;
using LibraryManagementAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repositories
{ }
public class AuthorRepository : IGenericRepository<Author>, IAuthorRepository
{
    private readonly LibraryContext _context;
    private readonly DbSet<Author> _authors;
    public AuthorRepository(LibraryContext context)
    {
        _authors = context.Set<Author>();
    }
    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _authors.Include(a => a.Books).ToListAsync();
    }

    public async Task<Author?> GetByIdAsync(Guid id)
    {
        return await _authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Author>> SearchAsync(string term)
    {
        return await _authors
            .Where(a => a.FirstName.Contains(term) ||
                        a.LastName.Contains(term) ||
                        a.Bio.Contains(term))
            .ToListAsync();
    }

   
    public async Task<Author> AddAsync(Author author)
    {
        await _authors.AddAsync(author);
        await _context.SaveChangesAsync();
        return author;
    }

    public async Task<Author> UpdateAsync(Author author)
    {
        _authors.Update(author);
        await _context.SaveChangesAsync();
        return author;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var author = await _authors.FindAsync(id);
        if (author == null) return false;

        _authors.Remove(author);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<Author?> GetAuthorWithBooksAsync(Guid authorId)
    {
        throw new NotImplementedException();
    }

    public Task SoftDeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Author>> GetAuthorsWithBooksAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Author>> FindAsync(Expression<Func<Author, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    Task IGenericRepository<Author>.UpdateAsync(Author entity)
    {
        return UpdateAsync(entity);
    }

    public Task DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task SoftDeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Author>> GetAllWithBooksAsync()
    {
        throw new NotImplementedException();
    }

    public Task GetByIdWithBooksAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task GetByIdWithBooksAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Author>> SearchAuthorsAsync(string searchTerm)
    {
        throw new NotImplementedException();
    }

    Task IGenericRepository<Author>.DeleteAsync(Guid id)
    {
        return DeleteAsync(id);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Author?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}