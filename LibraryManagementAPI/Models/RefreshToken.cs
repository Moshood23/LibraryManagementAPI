namespace LibraryManagementAPI.Models;

/// <summary>
/// A long-lived token that can be swapped for a new access token. Only a SHA-256 hash is stored,
/// so a leaked database can't be used to sign in.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the token is used, logged out, or revoked. A token can only be used once.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>The token issued in exchange for this one, if any.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
