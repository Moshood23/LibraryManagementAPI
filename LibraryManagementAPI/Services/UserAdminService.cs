using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Queries;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Services;

public class UserAdminService : IUserAdminService
{
    private static readonly string[] KnownRoles = { Roles.Admin, Roles.User };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthService _auth;

    public UserAdminService(UserManager<ApplicationUser> userManager, IAuthService auth)
    {
        _userManager = userManager;
        _auth = auth;
    }

    public async Task<PagedResult<UserReadDto>> GetUsersAsync(UserQueryParameters query)
    {
        var users = _userManager.Users;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            users = users.Where(u => u.Email!.ToLower().Contains(term) || u.FullName.ToLower().Contains(term));
        }

        var (page, total) = await users.OrderBy(u => u.Email).ThenBy(u => u.Id).ToPageAsync(query);

        var items = new List<UserReadDto>();
        foreach (var user in page)
            items.Add(await ToDtoAsync(user));
        return PagedResult<UserReadDto>.Create(items, query.Page, query.PageSize, total);
    }

    public async Task<ServiceResult<UserReadDto>> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null
            ? ServiceResult<UserReadDto>.Fail(ServiceError.NotFound, "User not found")
            : ServiceResult<UserReadDto>.Ok(await ToDtoAsync(user));
    }

    public async Task<ServiceResult<UserReadDto>> SetRolesAsync(string userId, IReadOnlyCollection<string> roles, string actingAdminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserReadDto>.Fail(ServiceError.NotFound, "User not found");

        // Match role names case-insensitively, but store the canonical spelling.
        var wanted = new List<string>();
        foreach (var role in roles.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var known = KnownRoles.FirstOrDefault(k => string.Equals(k, role, StringComparison.OrdinalIgnoreCase));
            if (known is null)
                return ServiceResult<UserReadDto>.Fail(ServiceError.Validation,
                    $"Unknown role '{role}'. Valid roles: {string.Join(", ", KnownRoles)}.");
            wanted.Add(known);
        }

        var current = await _userManager.GetRolesAsync(user);
        var removingAdmin = current.Contains(Roles.Admin) && !wanted.Contains(Roles.Admin);
        if (removingAdmin)
        {
            if (userId == actingAdminId)
                return ServiceResult<UserReadDto>.Fail(ServiceError.Conflict, "You can't remove your own Admin role.");
            if ((await _userManager.GetUsersInRoleAsync(Roles.Admin)).Count <= 1)
                return ServiceResult<UserReadDto>.Fail(ServiceError.Conflict, "You can't remove the last Admin.");
        }

        var toRemove = current.Except(wanted).ToList();
        var toAdd = wanted.Except(current).ToList();
        if (toRemove.Count > 0)
        {
            var removed = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded) return IdentityFailure(removed);
        }
        if (toAdd.Count > 0)
        {
            var added = await _userManager.AddToRolesAsync(user, toAdd);
            if (!added.Succeeded) return IdentityFailure(added);
        }

        return ServiceResult<UserReadDto>.Ok(await ToDtoAsync(user), "Roles updated. They apply from the user's next login or token refresh.");
    }

    public async Task<ServiceResult<UserReadDto>> LockAsync(string userId, string actingAdminId)
    {
        if (userId == actingAdminId)
            return ServiceResult<UserReadDto>.Fail(ServiceError.Conflict, "You can't lock your own account.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserReadDto>.Fail(ServiceError.NotFound, "User not found");

        await _userManager.SetLockoutEnabledAsync(user, true);
        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded) return IdentityFailure(result);

        // Stop them refreshing; their current access token still works until it expires.
        await _auth.RevokeAllAsync(user.Id);
        return ServiceResult<UserReadDto>.Ok(await ToDtoAsync(user), "User locked");
    }

    public async Task<ServiceResult<UserReadDto>> UnlockAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserReadDto>.Fail(ServiceError.NotFound, "User not found");

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded) return IdentityFailure(result);
        await _userManager.ResetAccessFailedCountAsync(user);
        return ServiceResult<UserReadDto>.Ok(await ToDtoAsync(user), "User unlocked");
    }

    private async Task<UserReadDto> ToDtoAsync(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        Roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList(),
        IsLockedOut = await _userManager.IsLockedOutAsync(user),
        LockoutEnd = user.LockoutEnd
    };

    private static ServiceResult<UserReadDto> IdentityFailure(IdentityResult result) =>
        ServiceResult<UserReadDto>.Fail(ServiceError.Validation, string.Join(" ", result.Errors.Select(e => e.Description)));
}
