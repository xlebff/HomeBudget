using HomeBudgetShared.Resources;
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


        public Currency Clone() => (Currency)MemberwiseClone();

        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Code)));
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Name)));
            }

            if (string.IsNullOrWhiteSpace(Symbol))
            {
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Symbol)));
            }

            return (true, null);
        }
    }
}
