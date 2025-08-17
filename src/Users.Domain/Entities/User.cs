using System;
using System.Collections.Generic;

namespace Users.Domain.Entities
{
    public sealed class User
    {
        public int Id { get; set; }
        public string Nickname { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Lastname { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public UserRole Role { get; set; } = UserRole.user;
        public UserStatus Status { get; set; } = UserStatus.active;
        public bool EmailConfirmed { get; set; } = false;
        public DateTime? LastLogin { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Preference? Preference { get; set; }
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}