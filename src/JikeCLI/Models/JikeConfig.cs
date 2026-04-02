namespace JikeCLI.Models;

public sealed class JikeConfig
{
    public string? Username { get; set; }

    public string? Tenant { get; set; }

    public string? Token { get; set; }

    public string? TokenType { get; set; }

    public string? RefreshToken { get; set; }

    public int ExpiresIn { get; set; }

    public string? BaseUrl { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
