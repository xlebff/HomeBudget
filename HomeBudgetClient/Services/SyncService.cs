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
            var apiCurrencies = await _client.GetAsync<Currency>("currencies");

            if (apiCurrencies is null || apiCurrencies.Count == 0)
                return (SyncStatus.Failed,
                    Messages.Error_InvalidRequestResult);

            var localCurrencies = await _context.Currencies
                .ToDictionaryAsync(c => c.Id);

            var anyChanges = false;

            // from server
            foreach (var apiCurrency in apiCurrencies)
            {
                if (localCurrencies.TryGetValue(apiCurrency.Id,
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
                    Messages.Info_NoContentToSync);

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
                    Messages.Error_InvalidRequestResult);
            }

            Dictionary<Guid, Category> localCategoriesIdDict = 
                await _context.Categories.ToDictionaryAsync(c => c.Id);

            Dictionary<Guid, Category> apiCategoriesIdDict = 
                apiCategories.ToDictionary(c => c.Id);

            if (apiCategories.Count == 0 &&
                localCategoriesIdDict.Count == 0)
            {
                return (SyncStatus.NothingToSync,
                    Messages.Info_NoContentToSync);
            }

            //User user;
            //if (await GetUser() is User userTmp)
            //{
            //    user = userTmp;
            //} 
            //else
            //{
            //    return (SyncStatus.Failed,
            //        Messages.Error_UserIdGetting);
            //}

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
                        if (localCategory.SyncedAt == null || 
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
                    //newCategory.User = user;

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
                //newCategory.User = await _client.GetAsync();

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
            List<DetailedTransactionResponse>? apiTransactionsResponse = 
                await _client.GetAsync<DetailedTransactionResponse>("transactions");

            if (apiTransactionsResponse is null)
            {
                return (SyncStatus.Failed,
                        Messages.Error_InvalidRequestResult);
            }

            Dictionary<Guid, DetailedTransactionResponse> apiTransactionsIdDict =
                apiTransactionsResponse.ToDictionary(t => t.Id);

            Dictionary<Guid, Transaction> localTransactionsIdDict =
                await _context.Transactions
                              .Include(t => t.Items)
                              .ToDictionaryAsync(t => t.Id);

            if (apiTransactionsResponse.Count == 0 &&
                localTransactionsIdDict.Count == 0)
            {
                return (SyncStatus.NothingToSync,
                        Messages.Info_NoContentToSync);
            }

            bool localChanges = false;
            bool apiChanges = false;

            DateTime syncTime = DateTime.UtcNow;

            foreach (DetailedTransactionResponse apiTransaction in
                apiTransactionsIdDict.Values)
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
                        Transaction newTransaction = CreateTransaction(
                            apiTransaction,
                            syncTime);

                        var (IsValid, ErrorMessage) = newTransaction.Validate();
                        if (!IsValid)
                        {
                            return (SyncStatus.Failed,
                                $"Transaction {localTransaction.Id}: {ErrorMessage}");
                        }

                        foreach (var item in localTransaction.Items)
                        {
                            var (itemValid, itemError) = item.Validate();
                            if (!itemValid)
                            {
                                return (SyncStatus.Failed,
                                    $"Transaction item {item.Id}: {itemError}");
                            }
                        }

                        anyLocalChanges = true;
                    }
                    else
                    {
                        // Одинаковая версия -> просто фиксируем sync time
                        if (localTransaction.SyncedAt is null ||
                            localTransaction.SyncedAt < syncTime)
                        {
                            localTransaction.SyncedAt = syncTime;
                            anyLocalChanges = true;
                        }
                    }
                }
                else
                {
                    // Есть на сервере, нет локально -> добавляем локально
                    var newLocalTransaction =
                        MapApiTransactionToLocal(apiTransaction, syncTime);

                    var (isValid, error) = newLocalTransaction.Validate();
                    if (!isValid)
                        return (SyncStatus.Failed,
                            $"Transaction {newLocalTransaction.Id}: {error}");

                    foreach (var item in newLocalTransaction.Items)
                    {
                        var (itemValid, itemError) = item.Validate();
                        if (!itemValid)
                        {
                            return (SyncStatus.Failed,
                                $"Transaction item {item.Id}: {itemError}");
                        }
                    }

                    await _context.Transactions.AddAsync(newLocalTransaction);
                    anyLocalChanges = true;
                }
            }

            // 2. Обрабатываем записи, которые есть локально, но отсутствуют на сервере
            foreach (var localTransaction in localTransactionsIdDict.Values)
            {
                if (apiTransactionsIdDict.ContainsKey(localTransaction.Id))
                    continue;

                await _client.PutAsync(
                    $"transactions/{localTransaction.Id}",
                    CreateTransactionRequest(localTransaction, syncTime));

                localTransaction.SyncedAt = syncTime;
                anyLocalChanges = true;
                anyRemoteChanges = true;
            }

            if (!anyLocalChanges && !anyRemoteChanges)
                return (SyncStatus.NothingToSync, null);

            try
            {
                if (anyLocalChanges)
                    await _context.SaveChangesAsync();

                return (SyncStatus.Synced, null);
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                return (SyncStatus.Failed, ex.GetBaseException().Message);
            }
        }

    //    private Transaction MapApiTransactionToLocal(
    //DetailedTransactionResponse apiTransaction,
    //DateTime syncTime)
    //    {
    //        var transaction = new Transaction
    //        {
    //            Id = apiTransaction.Id,
    //            UserId = apiTransaction.UserId,
    //            CategoryId = apiTransaction.CategoryId,
    //            Type = apiTransaction.Type,
    //            TotalAmount = apiTransaction.TotalAmount,
    //            CurrencyId = apiTransaction.CurrencyId!.Value,
    //            Comment = apiTransaction.Comment,
    //            Date = apiTransaction.Date,
    //            CreatedAt = apiTransaction.CreatedAt,
    //            UpdatedAt = apiTransaction.UpdatedAt,
    //            SyncedAt = syncTime,
    //            IsConsidered = apiTransaction.IsConsidered,
    //            IsDeleted = apiTransaction.IsDeleted,
    //            Items = MapApiItems(apiTransaction.Items, apiTransaction.Id, syncTime)
    //        };

    //        return transaction;
    //    }

        //private void ApplyApiTransactionToLocal(
        //    Transaction localTransaction,
        //    DetailedTransactionResponse apiTransaction,
        //    DateTime syncTime)
        //{
        //    localTransaction.UserId = apiTransaction.UserId;
        //    localTransaction.CategoryId = apiTransaction.CategoryId;
        //    localTransaction.Type = apiTransaction.Type;
        //    localTransaction.TotalAmount = apiTransaction.TotalAmount;
        //    localTransaction.CurrencyId = apiTransaction.CurrencyId!.Value;
        //    localTransaction.Comment = apiTransaction.Comment;
        //    localTransaction.Date = apiTransaction.Date;
        //    localTransaction.CreatedAt = apiTransaction.CreatedAt;
        //    localTransaction.UpdatedAt = apiTransaction.UpdatedAt;
        //    localTransaction.SyncedAt = syncTime;
        //    localTransaction.IsConsidered = apiTransaction.IsConsidered;
        //    localTransaction.IsDeleted = apiTransaction.IsDeleted;

        //    if (localTransaction.Items.Any())
        //        _context.TransactionItems.RemoveRange(localTransaction.Items);

        //    localTransaction.Items =
        //        MapApiItems(apiTransaction.Items, localTransaction.Id, syncTime);
        //}

        //private List<TransactionItem> MapApiItems(
        //    List<TransactionItemResponse>? apiItems,
        //    Guid transactionId,
        //    DateTime syncTime)
        //{
        //    if (apiItems is null || apiItems.Count == 0)
        //        return new List<TransactionItem>();

        //    return apiItems.Select(item => new TransactionItem
        //    {
        //        Id = item.Id,
        //        TransactionId = transactionId,
        //        Name = item.Name,
        //        Quantity = item.Quantity,
        //        UnitPrice = item.UnitPrice,
        //        TotalPrice = item.TotalPrice,

        //        // Если в DTO уже появились эти поля,
        //        // просто замени на значения с сервера
        //        CreatedAt = syncTime,
        //        IsDeleted = false
        //    }).ToList();
        //}

        private Transaction CreateTransaction(
            DetailedTransactionResponse t,
            DateTime syncTime)
        {
            Transaction res = new()
            {
                Id = t.Id,
                UserId = t.UserId,
                Type = t.Type,
                Date = t.Date,
                TotalAmount = t.TotalAmount,
                CurrencyId = (Guid)t.CurrencyId!,
                Comment = t.Comment,
                CategoryId = t.CategoryId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                SyncedAt = syncTime,
                IsDeleted = t.IsDeleted,
                IsConsidered = t.IsConsidered,
            };
        }

        private TransactionItem CreateTransactionItem(
            TransactionItemResponse ti,
            Guid tid)
        {
            return new TransactionItem
            {
                Id = ti.Id,
                Name = ti.Name,
                TransactionId = tid,
                UnitPrice = ti.UnitPrice,
                TotalPrice = ti.TotalPrice
            }
        }

        private CreateTransactionRequest CreateTransactionRequest(
            Transaction transaction,
            DateTime syncTime)
        {
            List<CreateTransactionItemRequest> transactionItemRequests = [];

            foreach (TransactionItem transactionItem in transaction.Items)
            {
                transactionItemRequests.Add(
                    CreateTransactionItemRequest(transactionItem));
            }

            return new CreateTransactionRequest
            {
                Id = transaction.Id,
                CategoryId = transaction.CategoryId,
                Type = transaction.Type,
                TotalAmount = transaction.TotalAmount,
                CurrencyId = transaction.CurrencyId,
                Comment = transaction.Comment,
                Date = transaction.Date,
                UpdatedAt = transaction.UpdatedAt,
                SyncedAt = syncTime,
                IsConsidered = transaction.IsConsidered,
                IsDeleted = transaction.IsDeleted,
                Items = transactionItemRequests
            };
        }

        private CreateTransactionItemRequest CreateTransactionItemRequest(
            TransactionItem ti)
        {
            return new CreateTransactionItemRequest
            {
                Id = ti.Id,
                Name = ti.Name,
                Quantity = ti.Quantity,
                UnitPrice = ti.UnitPrice,
                TotalPrice = ti.TotalPrice
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

        //private async Task<User?> GetUser()
        //{
        //    Guid? userId = await _tokenStorage.GetUserIdAsync();

        //    List<User> userList = [.. (await _context.GetFilteredAsync<User>(
        //        u => u.Id == userId.Value))];

        //    return userList.FirstOrDefault();
        //}
    }
}