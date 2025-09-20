using System;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Extensions;

public static partial class LoggerExtensions
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "User {Username} registered successfully with email {Email}")]
    public static partial void LogUserRegistered(this ILogger logger, string username, string email);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "User {EmailOrUsername} logged in successfully")]
    public static partial void LogUserLoggedIn(this ILogger logger, string emailOrUsername);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Failed login attempt for {EmailOrUsername}")]
    public static partial void LogFailedLoginAttempt(this ILogger logger, string emailOrUsername);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Registration attempt with existing email {Email}")]
    public static partial void LogRegistrationWithExistingEmail(this ILogger logger, string email);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Registration attempt with existing username {Username}")]
    public static partial void LogRegistrationWithExistingUsername(this ILogger logger, string username);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Error,
        Message = "Error uploading image for user {Username}: {Error}")]
    public static partial void LogImageUploadError(this ILogger logger, string username, string error);
}
