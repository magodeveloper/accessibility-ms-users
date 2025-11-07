namespace Users.Application.Dtos
{
    // Resetear contraseña (público - sin token por simplicidad)
    public record ResetPasswordDto(string Email, string NewPassword);

    // Cambiar contraseña estando autenticado
    public record ChangePasswordDto(string CurrentPassword, string NewPassword);
}