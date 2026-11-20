using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models;

/// <summary>Limits for the login, register and refresh endpoints, bound from "RateLimiting".</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public const string AuthPolicy = "auth";

    /// <summary>Requests allowed per client IP within one window.</summary>
    [Range(1, 100_000)]
    public int AuthPermitLimit { get; set; } = 10;

    [Range(1, 3600)]
    public int AuthWindowSeconds { get; set; } = 60;
}

/// <summary>Browser origins allowed to call the API, bound from "Cors".</summary>
public class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "frontend";

    /// <summary>For example "http://localhost:5173". Empty means no cross-origin browser access.</summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
