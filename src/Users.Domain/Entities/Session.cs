using System;
using System.Collections.Generic;

namespace Users.Domain.Entities
{
    public sealed class Session
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public User User { get; set; } = default!;
    }
}