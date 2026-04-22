namespace A_New_Hope.Utilities
{
    /// <summary>
    /// Provides shared helpers for normalizing user-entered text.
    /// </summary>
    public static class InputNormalization
    {
        /// <summary>
        /// Returns null when the value is blank; otherwise returns the trimmed value.
        /// </summary>
        public static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}