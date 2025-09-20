namespace AuthService.Application.DTOs;

public class RegisterResponseDto
{
    public UserResponseDto User { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool EmailVerificationRequired { get; set; } = true;
}
