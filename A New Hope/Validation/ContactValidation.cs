using System.Text.RegularExpressions;

namespace A_New_Hope.Validation
{
    /// <summary>
    /// Provides shared validation helpers for contact-related fields.
    /// </summary>
    public static partial class ContactValidation
    {
        [GeneratedRegex(@"^\+?[0-9()\-\s]+$")]
        private static partial Regex AllowedPhoneCharactersRegex();

        [GeneratedRegex(@"^[A-Za-z0-9._+\-]+$")]
        private static partial Regex EmailLocalPartRegex();

        [GeneratedRegex(@"^[A-Za-z0-9.\-]+$")]
        private static partial Regex EmailDomainPartRegex();

        /// <summary>
        /// Returns true when the phone number matches the allowed US format rules.
        /// </summary>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (!AllowedPhoneCharactersRegex().IsMatch(phoneNumber))
            {
                return false;
            }

            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length == 10)
            {
                return true;
            }

            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true when the email matches the allowed format rules.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (email.Contains(' '))
            {
                return false;
            }

            if (email.Count(c => c == '@') != 1)
            {
                return false;
            }

            if (email.Contains(".."))
            {
                return false;
            }

            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            var localPart = parts[0];
            var domainPart = parts[1];

            if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
            {
                return false;
            }

            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
            {
                return false;
            }

            if (domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                return false;
            }

            if (!domainPart.Contains('.'))
            {
                return false;
            }

            var domainLabels = domainPart.Split('.');
            if (domainLabels.Any(label => string.IsNullOrWhiteSpace(label)))
            {
                return false;
            }

            return EmailLocalPartRegex().IsMatch(localPart)
                && EmailDomainPartRegex().IsMatch(domainPart);
        }
    }
}