using System.ComponentModel.DataAnnotations;

namespace dsstats.shared.Auth;

public record LoginPayload
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode {  get; set; }
    public string? TowFactorRecoverCode { get; set; }
}

public record TokenInfo
{
    public string TokenType { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}

public record RegisterPayload
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record ErrorResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Details { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
    public Dictionary<string, string> Errors { get; init; } = [];
}

public record ResetPaylaod
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string ResetCode { get; set; } = string.Empty;
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

public record ManageInfoPayload
{
    public string NewEmail { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string OldPassword { get; set; } = string.Empty;
}

public record ManageInfoResponse
{
    public string Email { get; set; } = string.Empty;
    public bool IsEmailConfirmed { get; set; }
}