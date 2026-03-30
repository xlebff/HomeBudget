namespace HomeBudgetShared.Contracts
{
    public class DetailedTransactionResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }
        public string Type { get; set; } = "expense";
        public decimal TotalAmount { get; set; } = 0;
        public Guid? CurrencyId { get; set; } = Guid.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SyncedAt { get; set; }
        public bool IsConsidered { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public List<TransactionItemResponse>? Items { get; set; }
    }

    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }
        public string Type { get; set; } = "expense";
        public decimal TotalAmount { get; set; } = 0;
        public Guid CurrencyId { get; set; } = Guid.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SyncedAt { get; set; }
        public bool IsConsidered { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }

    public class CreateTransactionRequest
    {
        public Guid Id { get; set; } = new Guid();
        public string Type { get; set; } = "expense";
        public decimal TotalAmount { get; set; }
        public Guid? CurrencyId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SyncedAt { get; set; } = null;
        public bool IsConsidered { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public List<CreateTransactionItemRequest>? Items { get; set; }
    }

    public class CreateTransactionItemRequest
    {
        public Guid Id { get; set; } = new Guid();
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
    }

    public class TransactionItemResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}
