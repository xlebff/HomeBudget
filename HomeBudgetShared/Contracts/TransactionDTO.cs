namespace HomeBudgetShared.Contracts
{
    public class CreateTransactionRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? CurrencyId { get; set; }
        public Guid? CategoryId { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Comment { get; set; }

        public DateTime Date { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SyncedAt { get; set; } = null;

        public bool IsConsidered { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public List<CreateTransactionItemRequest>? Items { get; set; }
    }

    public class CreateTransactionItemRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}
