using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AuthService.Application.Interfaces;

namespace AuthService.Application.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailVerificationAsync(string email, string username, string token)
    {
        var subject = "Verify your email address";
        var verificationUrl = $"{configuration["AppSettings:FrontendUrl"]}/verify-email?token={token}";
        
        var body = $@"
            <h2>Welcome {username}!</h2>
            <p>Please verify your email address by clicking the link below:</p>
            <a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                Verify Email
            </a>
            <p>If you cannot click the link, copy and paste this URL into your browser:</p>
            <p>{verificationUrl}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account, please ignore this email.</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string username, string token)
    {
        var subject = "Reset your password";
        var resetUrl = $"{configuration["AppSettings:FrontendUrl"]}/reset-password?token={token}";
        
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>Hello {username},</p>
            <p>You requested to reset your password. Click the link below to reset it:</p>
            <a href='{resetUrl}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                Reset Password
            </a>
            <p>If you cannot click the link, copy and paste this URL into your browser:</p>
            <p>{resetUrl}</p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request this, please ignore this email and your password will remain unchanged.</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        var subject = "Welcome to AuthDotnet!";
        
        var body = $@"
            <h2>Welcome to AuthDotnet, {username}!</h2>
            <p>Your account has been successfully verified and activated.</p>
            <p>You can now enjoy all the features of our platform.</p>
            <p>If you have any questions, feel free to contact our support team.</p>
            <p>Thank you for joining us!</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpSettings = configuration.GetSection("SmtpSettings");
        
        try
        {
            // Verificar si el email está habilitado
            var enabled = bool.Parse(smtpSettings["Enabled"] ?? "true");
            if (!enabled)
            {
                logger.LogInformation("Email disabled in configuration. Skipping email to {Email}", to);
                logger.LogInformation("Email content: {Subject}", subject);
                return;
            }
            
            // Validar configuración
            var host = smtpSettings["Host"];
            var portString = smtpSettings["Port"];
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                logger.LogError("SMTP settings are not properly configured");
                throw new InvalidOperationException("SMTP settings are not properly configured");
            }

            logger.LogInformation("Sending email to {Email} via {Host}:{Port}", to, host, portString);
            logger.LogInformation("Using username: {Username}", username);
            logger.LogInformation("From email: {FromEmail}", fromEmail);

            var port = int.Parse(portString ?? "587");
            
            using var client = new SmtpClient();
            
            // Configurar timeout
            var timeoutMs = int.Parse(smtpSettings["Timeout"] ?? "30000");
            client.Timeout = timeoutMs;
            logger.LogInformation("SMTP client timeout set to {Timeout}ms", timeoutMs);
            
            try 
            {
                // Verificar configuración de SSL implícito
                var useImplicitSsl = bool.Parse(smtpSettings["UseImplicitSsl"] ?? "false");
                
                // Configuración específica por puerto y SSL
                if (useImplicitSsl || port == 465)
                {
                    logger.LogInformation("Using port {Port} with implicit SSL (SslOnConnect)", port);
                    await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
                }
                else if (port == 587)
                {
                    logger.LogInformation("Using port 587 with StartTLS");
                    await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                }
                else
                {
                    logger.LogInformation("Using port {Port} with auto SSL detection", port);
                    await client.ConnectAsync(host, port, SecureSocketOptions.Auto);
                }

                logger.LogInformation("Connected to SMTP server successfully");

                // Autenticación
                logger.LogInformation("Attempting to authenticate with Gmail...");
                await client.AuthenticateAsync(username, password);
                logger.LogInformation("Authentication successful");

                // Crear mensaje con MimeKit
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                // Enviar
                logger.LogInformation("Sending email message...");
                await client.SendAsync(message);
                logger.LogInformation("Email message sent successfully");
                
                await client.DisconnectAsync(true);
                logger.LogInformation("Disconnected from SMTP server");
                
                logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                logger.LogError(authEx, "Gmail authentication failed for {Username}. Check your app password!", username);
                logger.LogError("Gmail authentication error: {Error}", authEx.Message);
                throw new InvalidOperationException($"Gmail authentication failed: {authEx.Message}. Please check your app password.", authEx);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to {Email}. Error: {Error}", to, ex.Message);
                logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                throw;
            }
            logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}. Error: {Error}", to, ex.Message);
            
            // Verificar si usar fallback
            var useFallback = bool.Parse(smtpSettings["UseFallback"] ?? "false");
            if (useFallback)
            {
                logger.LogWarning("Using fallback: logging email instead of sending");
                logger.LogInformation("Email would be sent to: {Email}", to);
                logger.LogInformation("Subject: {Subject}", subject);
                logger.LogInformation("Body preview: {BodyPreview}", body[..Math.Min(100, body.Length)]);
                return; // No fallar, solo logear
            }
            
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }
}

