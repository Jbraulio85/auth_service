using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Entities;

public class UserEmail
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public bool EmailVerified { get; set; } = false;

    [MaxLength(256)]
    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiry { get; set; }

    [Required]
    public User User { get; set; } = null!;
}
