namespace AuthService.Api.Middlewares;

public class RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
{
    private static readonly string[] ValidContentTypes = 
    [
        "application/json",
        "multipart/form-data", 
        "application/x-www-form-urlencoded",
        "text/plain"
    ];

    private static readonly string[] SuspiciousHeaders = ["Host", "Authorization"];

    private static readonly string[] MaliciousPatterns = 
    [
        "<script", "</script>", "javascript:", "vbscript:",
        "onload=", "onerror=", "onclick=", "onmouseover=",
        "exec(", "eval(", "alert(", "confirm(",
        "SELECT ", "INSERT ", "UPDATE ", "DELETE ", "DROP ",
        "UNION ", "OR 1=1", "' OR '1'='1", "admin'--",
        "../", "..\\", "/etc/passwd", "cmd.exe"
    ];

    private static readonly string[] SuspiciousUserAgentPatterns = 
    [
        "sqlmap", "nikto", "nessus", "openvas", "nmap",
        "burp", "owasp", "zap", "w3af", "metasploit", 
        "curl", "wget", "python-requests", "bot", "crawler",
        "scanner", "exploit", "attack"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // Validar Content-Type para requests POST/PUT
        if (IsWriteRequest(context.Request.Method))
        {
            if (!IsValidContentType(context.Request.ContentType))
            {
                logger.LogWarning("Invalid Content-Type: {ContentType} from IP: {IpAddress}", 
                    context.Request.ContentType, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid Content-Type");
                return;
            }
        }

        // Validar tamaño de headers
        if (HasSuspiciousHeaders(context.Request))
        {
            logger.LogWarning("Suspicious headers detected from IP: {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid request headers");
            return;
        }

        // Validar caracteres maliciosos en query parameters
        if (HasMaliciousQueryParameters(context.Request.Query))
        {
            logger.LogWarning("Malicious query parameters detected from IP: {IpAddress}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid query parameters");
            return;
        }

        // Validar User-Agent
        if (HasSuspiciousUserAgent(context.Request))
        {
            logger.LogWarning("Suspicious User-Agent: {UserAgent} from IP: {IpAddress}", 
                context.Request.Headers.UserAgent, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid User-Agent");
            return;
        }

        await next(context);
    }

    private static bool IsWriteRequest(string method)
    {
        return method == "POST" || method == "PUT" || method == "PATCH";
    }

    private static bool IsValidContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }
            
        return ValidContentTypes.Any(valid => contentType.StartsWith(valid, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSuspiciousHeaders(HttpRequest request)
    {
        // Verificar headers excesivamente largos
        foreach (var header in request.Headers)
        {
            if (header.Key.Length > 100 || header.Value.ToString().Length > 1000)
            {
                return true;
            }
        }

        // Verificar headers duplicados sospechosos
        foreach (var suspiciousHeader in SuspiciousHeaders)
        {
            if (request.Headers[suspiciousHeader].Count > 1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMaliciousQueryParameters(IQueryCollection query)
    {
        foreach (var param in query)
        {
            var key = param.Key ?? "";
            var value = param.Value.ToString();

            foreach (var pattern in MaliciousPatterns)
            {
                if (key.Contains(pattern, StringComparison.OrdinalIgnoreCase) || 
                    value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasSuspiciousUserAgent(HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString();

        if (string.IsNullOrEmpty(userAgent))
        {
            return true;
        }

        return SuspiciousUserAgentPatterns.Any(pattern => userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

