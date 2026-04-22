using System.Text.RegularExpressions;

namespace A_New_Hope.Validation
{
    /// <summary>
    /// Provides shared validation helpers for person-related fields.
    /// </summary>
    public static partial class PersonValidation
    {
        [GeneratedRegex(@"^[A-Za-z][A-Za-z\s'.-]*$")]
        private static partial Regex PersonNameRegex();

        /// <summary>
        /// Returns true when the person name matches the allowed character rules.
        /// </summary>
        public static bool IsValidPersonName(string name)
        {
            return PersonNameRegex().IsMatch(name);
        }
    }
}