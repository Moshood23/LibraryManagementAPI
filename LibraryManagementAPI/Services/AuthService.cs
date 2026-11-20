using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.DTOs;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagementAPI.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password";
    private const string InvalidRefreshToken = "Invalid or expired refresh token";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly LibraryContext _context;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _time;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        LibraryContext context,
        IConfiguration configuration,
        TimeProvider time,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
        _time = time;
        _logger = logger;
    }

    private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    public async Task<AuthResult> RegisterAsync(RegisterDto model)
    {
        if (await _userManager.FindByEmailAsync(model.Email) is not null)
            return AuthResult.Failure("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, Roles.User);
        return AuthResult.Success(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> LoginAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
            return AuthResult.Failure(InvalidCredentials);

        // Counts failed attempts and locks the account after too many (see Lockout options in Program.cs).
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return AuthResult.Failure("This account is locked. Try again later or contact an administrator.");
        if (!result.Succeeded)
            return AuthResult.Failure(InvalidCredentials);

        return AuthResult.Success(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var stored = await FindAsync(refreshToken);
        if (stored is null)
            return AuthResult.Failure(InvalidRefreshToken);

        if (stored.RevokedAt is not null)
        {
            // A used token being presented again means it was probably stolen: cut off every session of this user.
            _logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all their tokens.", stored.UserId);
            await RevokeAllAsync(stored.UserId);
            return AuthResult.Failure(InvalidRefreshToken);
        }

        if (stored.ExpiresAt <= UtcNow)
            return AuthResult.Failure(InvalidRefreshToken);

        if (await _userManager.IsLockedOutAsync(stored.User))
            return AuthResult.Failure("This account is locked. Try again later or contact an administrator.");

        var replacement = NewRefreshToken(stored.UserId, out var rawReplacement);
        stored.RevokedAt = UtcNow;
        stored.ReplacedByTokenId = replacement.Id;
        _context.RefreshTokens.Add(replacement);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The same token was used twice at the same moment; only the first one wins.
            return AuthResult.Failure(InvalidRefreshToken);
        }

        return AuthResult.Success(await BuildResponseAsync(stored.User, rawReplacement, replacement.ExpiresAt));
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var stored = await FindAsync(refreshToken);
        if (stored is null || stored.RevokedAt is not null)
            return false;

        stored.RevokedAt = UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RevokeAllAsync(string userId)
    {
        var now = UtcNow;
        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in active)
            token.RevokedAt = now;
        await _context.SaveChangesAsync();
    }

    private async Task<RefreshToken?> FindAsync(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        var hash = Hash(rawToken);
        return await _context.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash);
    }

    private async Task<AuthResponseDto> IssueTokensAsync(ApplicationUser user)
    {
        var refresh = NewRefreshToken(user.Id, out var rawRefresh);
        _context.RefreshTokens.Add(refresh);
        await _context.SaveChangesAsync();
        return await BuildResponseAsync(user, rawRefresh, refresh.ExpiresAt);
    }

    private RefreshToken NewRefreshToken(string userId, out string rawToken)
    {
        rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var now = UtcNow;
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_configuration.GetValue("JWT:RefreshTokenDays", 7))
        };
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private async Task<AuthResponseDto> BuildResponseAsync(ApplicationUser user, string rawRefresh, DateTime refreshExpiresAt)
    {
        // Roles are read fresh every time, so a promotion or demotion applies from the next refresh.
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));
        var expires = DateTime.UtcNow.AddHours(_configuration.GetValue("JWT:ExpiryHours", 3));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            RefreshToken = rawRefresh,
            RefreshTokenExpiresAt = refreshExpiresAt,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }
}
