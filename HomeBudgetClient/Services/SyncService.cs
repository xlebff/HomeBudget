using HomeBudgetClient.Resources;
using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBudgetClient.Services
{
    internal enum SyncStatus
    {
        NothingToSync = 0,
        Synced = 1,
        Failed = 2
    }

    internal class SyncService(ApiClient client,
        AppDbContext context,
        TokenStorageService tokenStorage)
    {
        private readonly ApiClient _client = client;
        private readonly AppDbContext _context = context;
        private readonly TokenStorageService _tokenStorage = tokenStorage;

        public async Task<(bool IsSucced, string? ErrorMessage)> RunSyncAsync()
        {
            bool res = true;
            string? error = null;

            var (CurrenciesStatus, CurrenciesMessage) =
                await CurrenciesSyncAsync();

            if (CurrenciesStatus == SyncStatus.Failed)
            {
                res = false;
                error += CurrenciesMessage;
            }

            var (CategoriesStatus, CategoriesMessage) =
                await CategoriesSyncAsync();

            if (CategoriesStatus == SyncStatus.Failed)
            {
                res = false;
                error += CategoriesMessage;
            }

            var (TransactionsStatus, TransactionMessage) =
                await TransactionsSyncAsync();

            if (TransactionsStatus == SyncStatus.Failed)
            {
                res = false;
                error += TransactionMessage;
            }

            return (res, error);
        }

        private async Task<(SyncStatus, string?)> CurrenciesSyncAsync()
        {
            List<Currency>? apiCurrencies =
                await _client.GetAsync<Currency>("currencies");

            if (apiCurrencies is null ||
                apiCurrencies.Count == 0)
            {
                return (SyncStatus.Failed,
                        ClientMessages.Error_InvalidRequestResult);
            }

            Dictionary<Guid, Currency> localCurrenciesIdDict =
                await _context.Currencies.ToDictionaryAsync(c => c.Id);

            bool anyChanges = false;

            // from server
            foreach (Currency apiCurrency in apiCurrencies)
            {
                if (localCurrenciesIdDict.TryGetValue(apiCurrency.Id,
                    out var localCurrency))
                {
                    // if any changes in existing
                    if (localCurrency.Code != apiCurrency.Code ||
                        localCurrency.Name != apiCurrency.Name ||
                        localCurrency.Symbol != apiCurrency.Symbol)
                    {
                        _context.Entry(localCurrency).CurrentValues
                            .SetValues(apiCurrency);

                        anyChanges = true;
                    }
                }
                // if currency doesn`t exist
                else
                {
                    await _context.Currencies.AddAsync(apiCurrency);
                    anyChanges = true;
                }
            }

            if (!anyChanges)
                return (SyncStatus.NothingToSync,
                        ClientMessages.Info_NoContentToSync);

            try
            {
                await _context.SaveChangesAsync();
                return (SyncStatus.Synced, null);
            }
            catch (Exception ex)
            {
                return (SyncStatus.Failed, ex.Message);
            }
        }

        private async Task<(SyncStatus, string?)> CategoriesSyncAsync()
        {
            List<Category>? apiCategories = 
                await _client.GetAsync<Category>("categories");

            if (apiCategories is null ||
                apiCategories.Count == 0)
            {
                return (SyncStatus.Failed,
                    ClientMessages.Error_InvalidRequestResult);
            }

            Dictionary<Guid, Category> localCategoriesIdDict = 
                await _context.Categories.ToDictionaryAsync(c => c.Id);

            Dictionary<Guid, Category> apiCategoriesIdDict = 
                apiCategories.ToDictionary(c => c.Id);

            if (apiCategories.Count == 0 &&
                localCategoriesIdDict.Count == 0)
            {
                return (SyncStatus.NothingToSync,
                        ClientMessages.Info_NoContentToSync);
            }

            bool localChanges = false;
            bool apiChanges = false;

            DateTime syncTime = DateTime.UtcNow;

            foreach (Category apiCategory in apiCategories)
            {
                // have local
                if (localCategoriesIdDict.TryGetValue(
                    apiCategory.Id,
                    out var localCategory))
                {
                    // local newer
                    if (localCategory.UpdatedAt > apiCategory.UpdatedAt)
                    {
                        Category? updatedCategory =
                            CreateCategory(localCategory, syncTime);

                        await _client.PutAsync(
                            $"categories/{localCategory.Id}",
                            updatedCategory);

                        localCategory.SyncedAt = syncTime;

                        apiChanges = true;
                        localChanges = true;
                    }
                    // server newer
                    else if (apiCategory.UpdatedAt > localCategory.UpdatedAt)
                    {
                        localCategory = CreateCategory(apiCategory, syncTime);
                        localChanges = true;
                    }
                    // everything is up do date, sync time updating
                    else
                    {
                        if (localCategory.SyncedAt is null || 
                            localCategory.SyncedAt < syncTime)
                        {
                            localCategory.SyncedAt = syncTime;
                            localChanges = true;
                        }
                    }
                }
                // no local
                else
                {
                    Category newCategory = 
                        CreateCategory(apiCategory, syncTime);

                    await _context.Categories.AddAsync(newCategory);

                    localChanges = true;
                }
            }

            foreach (Category localCategory in localCategoriesIdDict.Values)
            {
                // have remotely
                if (apiCategoriesIdDict.ContainsKey(localCategory.Id))
                    continue;

                // no remotely
                Category newCategory = 
                    CreateCategory(localCategory, syncTime);

                await _client.PutAsync(
                    $"categories/{localCategory.Id}",
                    newCategory);

                localCategory.SyncedAt = syncTime;

                localChanges = true;
                apiChanges = true;
            }

            if (!localChanges && !apiChanges)
                return (SyncStatus.NothingToSync, null);

            try
            {
                if (localChanges)
                    await _context.SaveChangesAsync();

                return (SyncStatus.Synced, null);
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                return (SyncStatus.Failed, ex.GetBaseException().Message);
            }
        }

        private async Task<(SyncStatus, string?)> TransactionsSyncAsync()
        {
            List<Transaction>? apiTransactions = 
                await _client.GetAsync<Transaction>("transactions");

            if (apiTransactions is null)
            {
                return (SyncStatus.Failed,
                        ClientMessages.Error_InvalidRequestResult);
            }

            Dictionary<Guid, Transaction> apiTransactionsIdDict =
                apiTransactions.ToDictionary(t => t.Id);

            Dictionary<Guid, Transaction> localTransactionsIdDict =
                await _context.Transactions
                              .Include(t => t.Items)
                              .ToDictionaryAsync(t => t.Id);

            if (apiTransactions.Count == 0 &&
                localTransactionsIdDict.Count == 0)
            {
                return (SyncStatus.NothingToSync,
                        ClientMessages.Info_NoContentToSync);
            }

            bool localChanges = false;
            bool apiChanges = false;

            DateTime syncTime = DateTime.UtcNow;

            foreach (Transaction apiTransaction in apiTransactions)
            {
                if (localTransactionsIdDict.TryGetValue(
                    apiTransaction.Id,
                    out var localTransaction))
                {
                    // local newer
                    if (localTransaction.UpdatedAt >
                        apiTransaction.UpdatedAt)
                    {
                        await _client.PutAsync(
                            $"transactions/{localTransaction.Id}",
                            CreateTransactionRequest(localTransaction, syncTime));

                        localTransaction.SyncedAt = syncTime;

                        localChanges = true;
                        apiChanges = true;
                    }
                    // server newer
                    else if (apiTransaction.UpdatedAt >
                             localTransaction.UpdatedAt)
                    {
                        _context.TransactionItems
                            .RemoveRange(localTransaction.Items);

                        localTransaction.Items.Clear();

                        List<TransactionItem> items = [];

                        foreach (TransactionItem item in apiTransaction.Items)
                        {
                            items.Add(item);
                        }

                        await _context.TransactionItems.AddRangeAsync(items);

                        _context.Entry(localTransaction).CurrentValues
                            .SetValues(apiTransaction);

                        localTransaction.Items = items;

                        localTransaction.SyncedAt = syncTime;

                        localChanges = true;
                    }
                    else
                    {
                        if (localTransaction.SyncedAt is null ||
                            localTransaction.SyncedAt < syncTime)
                        {
                            localTransaction.SyncedAt = syncTime;
                            localChanges = true;
                        }
                    }
                }
                // no local
                else
                {
                    Transaction newTransaction = apiTransaction.Clone();
                    await _context.AddAsync(newTransaction);

                    List<TransactionItem> items = [];

                    foreach (TransactionItem item in apiTransaction.Items)
                    {
                        items.Add(item);
                    }

                    newTransaction.Items = items;

                    await _context.TransactionItems.AddRangeAsync(items);

                    localChanges = true;
                }
            }

            // from local
            foreach (Transaction localTransaction in 
                localTransactionsIdDict.Values)
            {
                // exists on api
                if (apiTransactionsIdDict.ContainsKey(localTransaction.Id))
                    continue;

                await _client.PutAsync(
                    $"transactions/{localTransaction.Id}",
                    CreateTransactionRequest(localTransaction, syncTime));

                localTransaction.SyncedAt = syncTime;

                localChanges = true;
                apiChanges = true;
            }

            if (!localChanges && !apiChanges)
                return (SyncStatus.NothingToSync, null);

            try
            {
                if (localChanges)
                    await _context.SaveChangesAsync();

                return (SyncStatus.Synced, null);
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                return (SyncStatus.Failed, ex.GetBaseException().Message);
            }
        }

        

        private CreateTransactionRequest CreateTransactionRequest(
            Transaction t,
            DateTime syncTime)
        {
            List<CreateTransactionItemRequest> itemRequests = [];

            if (t.Items.Count > 0)
            {
                foreach (TransactionItem item in t.Items)
                {
                    itemRequests.Add(new CreateTransactionItemRequest
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    });
                }
            }

            return new CreateTransactionRequest
            {
                Id = t.Id,
                CurrencyId = t.CurrencyId,
                CategoryId = t.CategoryId,
                TotalAmount = t.TotalAmount,
                Comment = t.Comment,
                Date = t.Date,
                UpdatedAt = t.UpdatedAt,
                SyncedAt = syncTime,
                IsConsidered = t.IsConsidered,
                IsDeleted = t.IsDeleted,
                Items = itemRequests
            };
        }

        private Category CreateCategory(Category c, DateTime syncTime)
        {
            return new Category
            {
                Id = c.Id,
                UserId = c.UserId,
                Name = c.Name,
                Type = c.Type,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                SyncedAt = syncTime,
                IsDeleted = c.IsDeleted
            };
        }
    }
}