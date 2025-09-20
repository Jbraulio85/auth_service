using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Entities;

public class UserPasswordReset
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [MaxLength(256)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    [Required]
    public User User { get; set; } = null!;
}
