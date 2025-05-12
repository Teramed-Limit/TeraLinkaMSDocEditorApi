namespace TeraLinkaMSDocEditorApi.Web.Extensions;

public static class LoggerExtensions
{
    public static void LogAuth(this ILogger logger, string message, params object[] args)
    {
        using (Serilog.Context.LogContext.PushProperty("LogCategory", "Auth"))
        {
            logger.LogInformation(message, args);
        }
    }
}
