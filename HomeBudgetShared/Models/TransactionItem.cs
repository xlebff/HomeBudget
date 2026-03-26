using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetShared.Models
{
    [Table("transaction_items")]
    public class TransactionItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [ForeignKey(nameof(Transaction))]
        public Guid TransactionId { get; set; } = Guid.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        public decimal? UnitPrice { get; set; } = 0;

        [Required]
        public decimal? TotalPrice { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsDeleted { get; set; } = false;

        public Transaction Transaction { get; set; } = null!;

        public TransactionItem Clone() => (TransactionItem)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (TransactionId == Guid.Empty)
                return (false, $"{nameof(TransactionId)} is required and " +
                    $"must be a valid GUID.");

            if (string.IsNullOrWhiteSpace(Name))
                return (false, $"{nameof(Name)} is required.");
            if (Name.Length > 255)
                return (false, $"{nameof(Name)} cannot exceed " +
                    $"255 characters.");

            if (Quantity <= 0)
                return (false, $"{nameof(Quantity)} must be" +
                    $" greater than zero.");

            if (UnitPrice < 0)
                return (false, $"{nameof(UnitPrice)} cannot be negative.");

            if (TotalPrice < 0)
                return (false, $"{nameof(TotalPrice)} cannot be negative.");

            if (CreatedAt == default)
                return (false, $"{nameof(CreatedAt)} is required.");

            if (UnitPrice.HasValue &&
                TotalPrice.HasValue)
            {
                if (Math.Abs(TotalPrice.Value - UnitPrice.Value *
                    Quantity) > 0.01m)
                    return (false, $"Price mismatch for item '{Name}'.");
            }
            else if (UnitPrice.HasValue)
            {
                TotalPrice = UnitPrice.Value * Quantity;
            }
            else if (TotalPrice.HasValue)
            {
                UnitPrice = TotalPrice / Quantity;
            }
            else
            {
                return (false, $"Either " +
                    $"{nameof(UnitPrice)} or {nameof(TotalPrice)} " +
                    $"must be provided for item " +
                    $"'{Name}'");
            }

            return (true, null);
        }
    }
}
