namespace Users.Application.Dtos;

public record LoginDto(string Email, string Password);
public record LoginResponseDto(string Token, DateTime? ExpiresAt, UserWithPreferenceReadDto User);