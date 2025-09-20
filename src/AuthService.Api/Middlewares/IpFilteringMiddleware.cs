using System.Net;

namespace AuthService.Api.Middlewares;

public class IpFilteringMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IpFilteringMiddleware> logger)
{
    private static readonly string[] LocalhostAddresses = ["127.0.0.1", "::1", "localhost"];

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        
        // Verificar lista negra de IPs
        if (IsBlacklisted(clientIp))
        {
            logger.LogWarning("Blocked request from blacklisted IP: {IpAddress}", clientIp);
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Access denied");
            return;
        }

        // Verificar lista blanca si está configurada (para endpoints administrativos)
        if (IsRestrictedEndpoint(context.Request.Path) && !IsWhitelisted(clientIp))
        {
            logger.LogWarning("Blocked request to restricted endpoint from non-whitelisted IP: {IpAddress} to {Path}", 
                clientIp, context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync("Access denied");
            return;
        }

        await next(context);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Verificar headers de proxy (X-Forwarded-For, X-Real-IP)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            var ips = xForwardedFor.Split(',');
            return ips[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsBlacklisted(string ipAddress)
    {
        var blacklistedIps = configuration.GetSection("Security:BlacklistedIPs").Get<string[]>() ?? [];
        return blacklistedIps.Contains(ipAddress);
    }

    private bool IsWhitelisted(string ipAddress)
    {
        var whitelistedIps = configuration.GetSection("Security:WhitelistedIPs").Get<string[]>() ?? [];

        // Si no hay lista blanca configurada, permitir acceso
        if (whitelistedIps.Length == 0)
        {
            return true;
        }
            
            
        return whitelistedIps.Contains(ipAddress) || IsLocalhost(ipAddress);
    }

    private bool IsRestrictedEndpoint(string path)
    {
        var restrictedPaths = configuration.GetSection("Security:RestrictedPaths").Get<string[]>() ?? [];
        return restrictedPaths.Any(restrictedPath => path.StartsWith(restrictedPath, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLocalhost(string ipAddress)
    {
        return LocalhostAddresses.Contains(ipAddress);
    }
}