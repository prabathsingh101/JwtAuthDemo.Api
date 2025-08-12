using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.Api.Entities.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string? Username { get; set; }
        [Required]
        public string? PasswordHash { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
