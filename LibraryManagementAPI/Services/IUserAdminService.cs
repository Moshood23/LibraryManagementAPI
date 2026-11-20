using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Queries;

namespace LibraryManagementAPI.Services;

public interface IUserAdminService
{
    Task<PagedResult<UserReadDto>> GetUsersAsync(UserQueryParameters query);
    Task<ServiceResult<UserReadDto>> GetUserAsync(string userId);
    Task<ServiceResult<UserReadDto>> SetRolesAsync(string userId, IReadOnlyCollection<string> roles, string actingAdminId);
    Task<ServiceResult<UserReadDto>> LockAsync(string userId, string actingAdminId);
    Task<ServiceResult<UserReadDto>> UnlockAsync(string userId);
}
