namespace AuthService.Api.Middlewares;

public class SecurityHeadersMiddleware(RequestDelegate next)
{

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Previene MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Previene clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Protección XSS (aunque moderno, algunos navegadores lo usan)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy: Controla información de referrer
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content-Security-Policy: Previene XSS y code injection
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");

        // Permissions-Policy: Controla APIs del navegador
        context.Response.Headers.Append("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=(), payment=()");

        // Cache-Control: Controla caché de respuestas sensibles
        if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            context.Response.Headers.Append("Pragma", "no-cache");
            context.Response.Headers.Append("Expires", "0");
        }

        // HSTS: Fuerza HTTPS (solo en producción)
        if (!context.Request.IsHttps && context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
        {
            context.Response.Headers.Append("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }

        // Remueve headers que revelan información del servidor
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        await next(context);
    }
}

