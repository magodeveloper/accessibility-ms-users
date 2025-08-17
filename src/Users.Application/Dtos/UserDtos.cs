namespace Users.Application.Dtos
{
    public record UserWithPreferenceReadDto(
        int Id,
        string Nickname,
        string Name,
        string Lastname,
        string Email,
        string Role,
        string Status,
        bool EmailConfirmed,
        DateTime? LastLogin,
        DateTime RegistrationDate,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        PreferenceReadDto? Preference
    );

    public record UserCreateDto(string Nickname, string Name, string Lastname, string Email, string Password);
    public record UserReadDto(int Id, string Nickname, string Name, string Lastname, string Email, string Role, string Status, bool EmailConfirmed, DateTime? LastLogin, DateTime RegistrationDate, DateTime CreatedAt, DateTime UpdatedAt);
    public record UserPatchDto(string? Nickname, string? Name, string? Lastname, string? Role, string? Status, bool? EmailConfirmed, string? Email, string? Password);
}