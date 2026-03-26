using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetShared.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; } = Guid.Empty;

        [Required]
        [StringLength(10)]
        public string Type { get; set; } = "expese";

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        public decimal TotalAmount { get; set; } = 0;

        [Required]
        [ForeignKey(nameof(Currency))]
        public Guid CurrencyId { get; set; } = Guid.Empty;

        public string Comment { get; set; } = string.Empty;

        [ForeignKey(nameof(Category))]
        public Guid? CategoryId { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SyncedAt { get; set; }

        public Category? Category { get; set; }

        public User User { get; set; } = null!;

        public Currency Currency { get; set; } = null!;

        public ICollection<TransactionItem> Items { get; set; } = null!;

        public Transaction Clone() => (Transaction)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (UserId == Guid.Empty)
                return (false, $"{nameof(UserId)} is required and must be a valid GUID.");

            if (string.IsNullOrWhiteSpace(Type))
                return (false, $"{nameof(Type)} is required.");
            var allowedTypes = new[] { "expense", "income" };
            if (!allowedTypes.Contains(Type.ToLowerInvariant()))
                return (false, $"{nameof(Type)} must be either 'expense' or 'income'.");

            if (Date == default)
                return (false, $"{nameof(Date)} is required.");

            if (TotalAmount < 0)
                return (false, $"{nameof(TotalAmount)} cannot be negative.");

            if (CurrencyId == Guid.Empty)
                return (false, $"{nameof(CurrencyId)} is required and must be a valid GUID.");

            if (UpdatedAt == default)
                return (false, $"{nameof(UpdatedAt)} is required.");

            return (true, null);
        }
    }
}
