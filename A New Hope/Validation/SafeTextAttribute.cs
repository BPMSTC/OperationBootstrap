using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace A_New_Hope.Validation
{
    /// <summary>
    /// Rejects input containing suspicious characters or patterns
    /// that are not allowed for simple text-entry fields.
    /// </summary>
    public sealed partial class SafeTextAttribute : ValidationAttribute
    {
        [GeneratedRegex(
            @"(--|;|/\*|\*/|\bDROP\b|\bSELECT\b|\bINSERT\b|\bDELETE\b|\bUNION\b)",
            RegexOptions.IgnoreCase)]
        private static partial Regex SuspiciousPatternRegex();

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Null or blank values are allowed here.
            // Use [Required] separately when a field must have a value.
            if (value is null)
                return ValidationResult.Success;

            if (value is not string text)
                return ValidationResult.Success;

            if (string.IsNullOrWhiteSpace(text))
                return ValidationResult.Success;

            if (SuspiciousPatternRegex().IsMatch(text))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"{validationContext.DisplayName} contains disallowed text.");
            }

            return ValidationResult.Success;
        }
    }
}