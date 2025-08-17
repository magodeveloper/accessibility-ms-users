namespace Users.Application.Dtos;

public record SessionReadDto(
    int Id,
    int UserId,
    string TokenHash,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    UserReadDto? User
);