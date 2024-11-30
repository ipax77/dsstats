using System.ComponentModel.DataAnnotations;
using System.Text;

namespace dsstats.shared.Auth;

public record LoginPayload
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
    public string? TowFactorRecoverCode { get; set; }
}

public record TokenInfo
{
    public string TokenType { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}

public record RegisterFormPayload
{
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [PasswordValidator]
    public string Password { get; set; } = string.Empty;
    [Compare(nameof(Email))]
    public string ConfirmEmail { get; set; } = string.Empty;
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
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
    public string Detail { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
    public Dictionary<string, string> Errors { get; init; } = [];
}

public record ResetPayload
{
    public string Email { get; set; } = string.Empty;
    public string ResetCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public record ResetFormPayload
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string ResetCode { get; set; } = string.Empty;
    [Required]
    [PasswordValidator]
    public string NewPassword { get; set; } = string.Empty;
    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
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

public class PasswordValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return new ValidationResult("Password cannot be null.");
        }

        StringBuilder sb = new();

        if (value is string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return new ValidationResult("Password cannot be empty.");
            }

            if (stringValue.Length < 8)
            {
                return new ValidationResult("Password must have at least 8 characters.");
            }

            if (!stringValue.Any(char.IsDigit))
            {
                sb.AppendLine("Password must contain at least one number");        
            }

            if (!stringValue.Any(char.IsUpper))
            {
                sb.AppendLine("Password must contain at least one upper case letter.");
            }

            if (!stringValue.Any(char.IsLower))
            {
                sb.AppendLine("Password must contain at least one lower case letter.");
            }

            if (stringValue.All(char.IsLetterOrDigit))
            {
                sb.AppendLine("Password must contain at least one non alphanumric character e.g. !#_.|$%&");
            }
        }
        else
        {
            throw new InvalidOperationException("Password validator can validate strings only.");
        }

        if (sb.Length > 0)
        {
            return new ValidationResult(sb.ToString());
        }
        else
        {
            return ValidationResult.Success;
        }
    }
}