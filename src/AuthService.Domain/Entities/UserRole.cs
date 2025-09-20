using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Entities;

public class UserRole
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    [Required]
    public Role Role { get; set; } = null!;
}
