namespace HomeBudgetShared.Contracts
{
    //public class CategoryResponse
    //{
    //    public Guid Id { get; set; }
    //    public Guid? UserId { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public string Type { get; set; } = string.Empty;
    //    public DateTime CreatedAt { get; set; }
    //    public DateTime UpdatedAt { get; set; }
    //    public DateTime? SyncedAt { get; set; }
    //    public bool IsDeleted { get; set; } = false;
    //}

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "expense";
    }
}