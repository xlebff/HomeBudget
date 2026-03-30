using HomeBudgetShared.Resources;
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
        public Guid? CurrencyId { get; set; }


        [Required]
        public DateTime CreatedAt { get; } = DateTime.Now;


        public User Clone() => (User)MemberwiseClone();


        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Login))
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(Login)));

            if (Login.Length > 100)
                return (false,
                        String.Format(
                            Messages.Error_TooLong,
                            nameof(Login),
                            100));

            if (string.IsNullOrWhiteSpace(PasswordHash))
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(PasswordHash)));

            if (PasswordHash.Length > 255)
                return (false,
                        String.Format(
                            Messages.Error_TooLong,
                            nameof(PasswordHash),
                            255));

            if (CreatedAt == default)
                return (false,
                        String.Format(
                            Messages.Error_Required,
                            nameof(CreatedAt)));

            return (true, null);
        }
    }
}
