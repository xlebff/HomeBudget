using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeBudgetShared.Models
{
    [Table("currencies")]
    public sealed class Currency
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string Symbol { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = [];

        public Currency Clone() => (Currency)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                return (false, $"{nameof(Code)} is required.");
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                return (false, $"{nameof(Name)} is required.");
            }

            if (string.IsNullOrWhiteSpace(Symbol))
            {
                return (false, $"{nameof(Symbol)} is required.");
            }

            return (true, null);
        }
    }
}
