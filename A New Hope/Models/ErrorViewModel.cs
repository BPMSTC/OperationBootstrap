namespace A_New_Hope.Models
{
    /// <summary>
    /// View model for the shared error page.
    /// Provides a request identifier for troubleshooting.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Correlation identifier for the current request.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indicates whether the request ID should be shown.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
    }
}