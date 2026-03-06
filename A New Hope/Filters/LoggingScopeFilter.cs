using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// LoggingScopeFilter
/// ------------------
/// This is an ASP.NET Core MVC action filter that adds a *logging scope* around every controller action
/// (for any controllers where it is registered).
///
/// What a "logging scope" does:
/// - A scope attaches contextual key/value data to *all log messages written within that scope*.
/// - When your logger provider supports scopes (Console/Debug/etc. usually do),
///   every log line written during the action can include the scope values.
///
/// In this filter, the scope includes:
/// - Controller name
/// - Action name
///
/// So instead of seeing plain logs like:
///   "Action started"
/// you can see logs (depending on formatting/provider) like:
///   "Action started => Controller=Users, Action=Edit"
///
/// Why this is helpful:
/// - Makes it easier to trace logs back to the MVC endpoint that generated them.
/// - Makes log search/filtering much easier in terminals, log aggregators, and Event Viewer.
/// - Provides consistent structure without repeating controller/action in every single log call.
///
/// Lifetime:
/// - OnActionExecuting: creates the scope (BeginScope) and logs "Action started"
/// - OnActionExecuted: logs success or error and disposes the scope
///
/// Important notes:
/// - This filter does not modify request handling or controller logic.
/// - It only adds logging.
/// - The IDisposable returned by BeginScope MUST be disposed, otherwise the scope can leak
///   into subsequent logs (especially in reused threads/async contexts).
/// </summary>
public class LoggingScopeFilter : IActionFilter
{
    /// <summary>
    /// Logger instance for this filter.
    /// Any logs written here will use the category name LoggingScopeFilter.
    /// </summary>
    private readonly ILogger<LoggingScopeFilter> _logger;

    /// <summary>
    /// Holds the logging scope created for the current request/action execution.
    /// - BeginScope returns an IDisposable.
    /// - Disposing it ends the scope and removes the contextual values.
    /// </summary>
    private IDisposable? _scope;

    /// <summary>
    /// Constructor with dependency injection.
    /// ASP.NET Core will provide an ILogger automatically when the filter is registered.
    /// </summary>
    public LoggingScopeFilter(ILogger<LoggingScopeFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs BEFORE the controller action method executes.
    ///
    /// Responsibilities here:
    /// 1) Determine the controller and action names from routing data.
    /// 2) Create a logging scope that includes those values.
    /// 3) Write a "start" log entry so you can see when the action begins.
    ///
    /// Notes:
    /// - context.RouteData.Values contains route tokens set by MVC routing.
    /// - The values are objects; logging can handle objects, but they are usually strings.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Extract route values so we can attach them to the logging scope.
        // These are typically set by MVC routing (e.g., controller=Users, action=Edit).
        var controller = context.RouteData.Values["controller"];
        var action = context.RouteData.Values["action"];

        // Create a logging scope.
        // Everything logged while this scope is active can include Controller/Action context.
        //
        // Template + values:
        // - "Controller={Controller}, Action={Action}" is the scope message format.
        // - controller/action are the values substituted into the scope.
        _scope = _logger.BeginScope(
            "Controller={Controller}, Action={Action}",
            controller,
            action
        );

        // Log a generic start message.
        // Because a scope is active, log output can include Controller/Action without repeating it here.
        _logger.LogInformation("Action started");
    }

    /// <summary>
    /// Runs AFTER the controller action method executes (and after the action result is produced).
    ///
    /// Responsibilities here:
    /// 1) Log whether the action completed successfully or failed.
    /// 2) If an exception occurred, log it (with the exception object for stack trace details).
    /// 3) Dispose the scope to ensure scope context does not leak into other logs.
    ///
    /// Notes:
    /// - context.Exception will be non-null if an unhandled exception occurred during the action pipeline.
    /// - Disposing the scope is critical for correctness, especially under high concurrency.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // If no exception occurred, log success.
        if (context.Exception == null)
        {
            _logger.LogInformation("Action completed successfully");
        }
        else
        {
            // If an exception occurred, log it at error level.
            // Passing context.Exception ensures stack trace + exception details are captured.
            _logger.LogError(
                context.Exception,
                "Action failed with exception"
            );
        }

        // End the scope for this action execution.
        // This prevents scope values from persisting into subsequent unrelated logs.
        _scope?.Dispose();
    }
}