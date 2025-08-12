using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthDemo.Api.Entities.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        // We store a HASH of the token for security (not the raw token)
        [Required]
        public string? TokenHash { get; set; }

        [Required]
        public DateTime? Expires { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Revoked { get; set; }

        // optional: which token replaced this one
        public string? ReplacedByTokenHash { get; set; }

        [ForeignKey(nameof(User))]
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= Expires;
        [NotMapped]
        public bool IsActive => Revoked == null && !IsExpired;
    }

}
