namespace Users.Application.Dtos;

public record ResetPasswordRequestDto(string Email);
public record ResetPasswordDto(string Email, string NewPassword);