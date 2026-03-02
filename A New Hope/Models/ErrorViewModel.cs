namespace A_New_Hope.Models
{
    /// <summary>
    /// ErrorViewModel
    /// --------------
    /// Simple view model used by the Error view (typically Views/Shared/Error.cshtml).
    ///
    /// Purpose:
    /// - Provide a request identifier that can help correlate a user-facing error page
    ///   with server-side logs and diagnostics.
    ///
    /// Typical usage:
    /// - HomeController.Error() populates RequestId from:
    ///     - Activity.Current?.Id (when tracing is enabled)
    ///     - or HttpContext.TraceIdentifier as a fallback
    ///
    /// UI usage:
    /// - The view can show the RequestId only when it exists (ShowRequestId == true),
    ///   keeping the error page clean while still offering a useful troubleshooting hook.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Correlation identifier for the current request.
        /// This can be shown on the error page and used to find matching log entries.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Convenience property for the view:
        /// true when RequestId is present and should be displayed.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}