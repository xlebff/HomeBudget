using HomeBudgetShared.Resources;
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
        [ForeignKey(nameof(Currency))]
        public Guid CurrencyId { get; set; } = Guid.Empty;

        [ForeignKey(nameof(Category))]
        public Guid? CategoryId { get; set; }


        [Required]
        public decimal TotalAmount { get; set; } = 0;


        public string? Comment { get; set; }


        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;


        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SyncedAt { get; set; }


        [Required]
        public bool IsConsidered { get; set; } = true;

        [Required]
        public bool IsDeleted { get; set; } = false;


        public ICollection<TransactionItem> Items { get; set; } = null!;


        public Transaction Clone() => (Transaction)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (UserId == Guid.Empty)
                return (false, 
                        String.Format(
                            Messages.Error_Required,
                            nameof(UserId)));

            if (Date == default)
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Date)));

            if (TotalAmount < 0)
                return (false,
                        String.Format(
                            Messages.Error_Negative,
                            nameof(TotalAmount)));

            if (CurrencyId == Guid.Empty)
                return (false,
                        String.Format(
                                Messages.Error_Required,
                                nameof(CurrencyId)));

            if (UpdatedAt == default)
                return (false,
                        String.Format(
                                Messages.Error_Required,
                                nameof(UpdatedAt)));

            return (true, null);
        }
    }
}
