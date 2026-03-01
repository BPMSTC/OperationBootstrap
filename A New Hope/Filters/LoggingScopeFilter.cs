using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

public class LoggingScopeFilter : IActionFilter
{
    private readonly ILogger<LoggingScopeFilter> _logger;
    private IDisposable? _scope;

    public LoggingScopeFilter(ILogger<LoggingScopeFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"];
        var action = context.RouteData.Values["action"];

        _scope = _logger.BeginScope(
            "Controller={Controller}, Action={Action}",
            controller,
            action
        );

        _logger.LogInformation("Action started");
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception == null)
        {
            _logger.LogInformation("Action completed successfully");
        }
        else
        {
            _logger.LogError(
                context.Exception,
                "Action failed with exception"
            );
        }

        _scope?.Dispose();
    }
}