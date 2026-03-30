using HomeBudgetShared.Resources;
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


        public TransactionItem Clone() => (TransactionItem)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (TransactionId == Guid.Empty)
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(TransactionId)));

            if (string.IsNullOrWhiteSpace(Name))
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Name)));

            if (Name.Length > 255)
                return (false,
                        String.Format(
                            Messages.Error_TooLong,
                            nameof(Name),
                            255));

            if (Quantity <= 0)
                return (false,
                        String.Format(
                            Messages.Error_TooLittle,
                            nameof(Quantity),
                            0));

            if (UnitPrice < 0)
                return (false,
                        String.Format(
                                Messages.Error_TooLittle,
                                nameof(UnitPrice),
                                0));

            if (TotalPrice < 0)
                return (false,
                        String.Format(
                            Messages.Error_TooLittle,
                            nameof(TotalPrice),
                            0));

            if (CreatedAt == default)
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(CreatedAt)));

            if (UnitPrice.HasValue &&
                TotalPrice.HasValue)
            {
                if (Math.Abs(TotalPrice.Value - UnitPrice.Value *
                    Quantity) > 0.01m)
                    return (false,
                            String.Format(
                                Messages.Error_PriceMismatch,
                                Name));
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
                return (false,
                        String.Format(
                            Messages.Error_EitherRequired,
                            nameof(UnitPrice),
                            nameof(TotalPrice)));
            }

            return (true, null);
        }
    }
}
