using System.Text.Json;

namespace AuthService.Api.Middlewares;

public class SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
{
    private static readonly string[] StandardMethods = ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log solo eventos de seguridad importantes
            if (ShouldLogRequest(context))
            {
                LogSecurityEvent(context, stopwatch.ElapsedMilliseconds);
            }

            // Restaurar el stream original y copiar la respuesta
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldLogRequest(HttpContext context)
    {
        // Log eventos de autenticación
        if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            return true;
        }

        // Log requests fallidos
        if (context.Response.StatusCode >= 400)
        {
            return true;
        }

        // Log requests con métodos no estándar
        if (!StandardMethods.Contains(context.Request.Method))
        {
            return true;
        }

        // Log requests con headers sospechosos
        if (context.Request.Headers.ContainsKey("X-Forwarded-For") ||
            context.Request.Headers.ContainsKey("X-Real-IP"))
        {
            return true;
        }

        return false;
    }

    private void LogSecurityEvent(HttpContext context, long elapsedMs)
    {
        var clientIp = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userId = context.User?.Identity?.Name ?? "Anonymous";

        var logData = new
        {
            Timestamp = DateTime.UtcNow,
            ClientIP = clientIp,
            UserAgent = userAgent,
            UserId = userId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            StatusCode = context.Response.StatusCode,
            ElapsedMs = elapsedMs,
            RequestHeaders = GetSafeHeaders(context.Request.Headers),
            ResponseHeaders = GetSafeHeaders(context.Response.Headers)
        };

        var logLevel = GetLogLevel(context.Response.StatusCode);
        var logMessage = JsonSerializer.Serialize(logData, JsonOptions);

        switch (logLevel)
        {
            case LogLevel.Warning:
                logger.LogWarning("Security Event: {LogData}", logMessage);
                break;
            case LogLevel.Error:
                logger.LogError("Security Event: {LogData}", logMessage);
                break;
            case LogLevel.Critical:
                logger.LogCritical("Security Event: {LogData}", logMessage);
                break;
            default:
                logger.LogInformation("Security Event: {LogData}", logMessage);
                break;
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-API-Key", "X-Auth-Token" };

        foreach (var header in headers)
        {
            if (sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                safeHeaders[header.Key] = "[REDACTED]";
            }
            else
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }

    private static LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            401 or 403 => LogLevel.Error,
            >= 500 => LogLevel.Critical,
            >= 400 and < 500 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
