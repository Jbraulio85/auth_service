using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Models;

public class RegisterRequestDto
{
    [Required]
    [MaxLength(25)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(25)]
    public string Surname { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public string Phone { get; set; } = string.Empty;
    
    public IFormFile? ProfilePicture { get; set; }
}