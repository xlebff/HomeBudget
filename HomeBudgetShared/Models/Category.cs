using HomeBudgetShared.Resources;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetShared.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(User))]
        public Guid? UserId { get; set; }


        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Type { get; set; } = "expense";


        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public DateTime? SyncedAt { get; set; }


        [Required]
        public bool IsDeleted { get; set; } = false;


        public Category Clone() => (Category)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (UserId == Guid.Empty)
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(UserId)));

            if (string.IsNullOrWhiteSpace(Name))
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Name)));

            if (Name.Length > 100)
                return (false,
                        String.Format(
                            Messages.Error_TooLong,
                            nameof(Name),
                            100));

            if (string.IsNullOrWhiteSpace(Type))
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Type)));

            var allowedTypes = new[] { "expense", "income" };
            if (!allowedTypes.Contains(Type.ToLowerInvariant()))
                return (false, $"{nameof(Type)} " +
                    $"must be either 'expense' or 'income'.");

            return (true, null);
        }
    }
}
