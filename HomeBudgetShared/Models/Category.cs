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

        public User? User { get; set; } = null;

        public Category Clone() => (Category)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (UserId == Guid.Empty)
                return (false, $"{nameof(UserId)} is required and must be a valid GUID.");

            if (string.IsNullOrWhiteSpace(Name))
                return (false, $"{nameof(Name)} is required.");
            if (Name.Length > 100)
                return (false, $"{nameof(Name)} cannot exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(Type))
                return (false, $"{nameof(Type)} is required.");
            var allowedTypes = new[] { "expense", "income" };
            if (!allowedTypes.Contains(Type.ToLowerInvariant()))
                return (false, $"{nameof(Type)} must be either 'expense' or 'income'.");

            return (true, null);
        }
    }
}
