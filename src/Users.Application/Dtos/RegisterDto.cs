using System.ComponentModel.DataAnnotations;

namespace Users.Application.Dtos;

/// <summary>
/// DTO para registro de nuevos usuarios (auto-registro público)
/// </summary>
public sealed class RegisterDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    public string Lastname { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "El nickname no puede exceder 50 caracteres")]
    public string? Nickname { get; set; }
}
