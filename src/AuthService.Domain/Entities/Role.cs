using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Entities;

public class Role
{
     [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre del rol no puede superar los 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
