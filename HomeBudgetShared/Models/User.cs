using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetShared.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Login { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [ForeignKey(nameof(Currency))]
        public Guid CurrencyId { get; set; }

        public Currency Currency { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; } = DateTime.Now;

        public DateTime? LastSync { get; set; }

        public ICollection<Category> Categories { get; set; } = null!;

        public ICollection<Transaction> Transactions { get; set; } = null!;


        public User Clone() => (User)MemberwiseClone();


        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Login))
                return (false, $"{nameof(Login)} is required.");
            if (Login.Length > 100)
                return (false, $"{nameof(Login)} cannot exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(PasswordHash))
                return (false, $"{nameof(PasswordHash)} is required.");
            if (PasswordHash.Length > 255)
                return (false, $"{nameof(PasswordHash)} cannot exceed 255 characters.");

            if (CreatedAt == default)
                return (false, $"{nameof(CreatedAt)} is required.");

            return (true, null);
        }
    }
}
