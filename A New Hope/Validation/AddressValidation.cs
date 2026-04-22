using System.Text.RegularExpressions;

namespace A_New_Hope.Validation
{
    /// <summary>
    /// Provides shared validation helpers for address and location-related fields.
    /// </summary>
    public static partial class AddressValidation
    {
        // Store the allowed 2-letter US state codes for validation.
        private static readonly HashSet<string> ValidUsStateCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA",
            "HI","ID","IL","IN","IA","KS","KY","LA","ME","MD",
            "MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ",
            "NM","NY","NC","ND","OH","OK","OR","PA","RI","SC",
            "SD","TN","TX","UT","VT","VA","WA","WV","WI","WY","DC"
        };

        [GeneratedRegex(@"^[A-Za-z][A-Za-z\s'.-]*$")]
        private static partial Regex CityRegex();

        [GeneratedRegex(@"^\d{5}(-\d{4})?$")]
        private static partial Regex UsPostalCodeRegex();

        /// <summary>
        /// Returns true when the value contains at least one letter or digit.
        /// </summary>
        public static bool ContainsLetterOrDigit(string value)
        {
            return value.Any(char.IsLetterOrDigit);
        }

        /// <summary>
        /// Returns true when the city value matches the allowed character rules.
        /// </summary>
        public static bool IsValidCity(string city)
        {
            return CityRegex().IsMatch(city);
        }

        /// <summary>
        /// Returns true when the state value is a valid 2-letter US state code.
        /// </summary>
        public static bool IsValidUsStateCode(string state)
        {
            return state.Length == 2 && ValidUsStateCodes.Contains(state);
        }

        /// <summary>
        /// Returns true when the postal code matches US ZIP or ZIP+4 format.
        /// </summary>
        public static bool IsValidUsPostalCode(string postalCode)
        {
            return UsPostalCodeRegex().IsMatch(postalCode);
        }
    }
}