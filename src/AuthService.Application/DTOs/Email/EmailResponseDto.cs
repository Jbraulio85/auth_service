namespace AuthService.Application.DTOs.Email;

public class EmailResponseDto
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}