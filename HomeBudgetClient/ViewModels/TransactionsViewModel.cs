using CommunityToolkit.Mvvm.ComponentModel;
using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HomeBudgetClient.Services
{
    internal partial class TransactionsViewModel(AppDbContext context, 
        ApiClient client)
        : ObservableObject
    {
        public readonly AppDbContext _context = context;

        public readonly ApiClient _client = client;

        [ObservableProperty]
        private ObservableCollection<Transaction>
            _transactions = [];

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyText;

        //public async Task TestDataInsert(Guid userId)
        //{
        //    await ExecuteAsync(async () =>
        //    {
        //        await _context.Database.EnsureCreatedAsync();

        //        var currencies = await _client.GetCurrenciesAsync();

        //        foreach (var currency in currencies)
        //        {
        //            await _context.AddAsync(currency);
        //        }
        //    });
        //}

        public async Task LoadTransactionsAsync(Guid userId)
        {
            await ExecuteAsync(async () =>
            {
                var transactions = await _context.GetFilteredAsync<Transaction>(
                    t => t.UserId == userId);

                if (transactions is not null && transactions.Any())
                {
                    Transactions ??= [];

                    foreach (var transaction in transactions)
                    {
                        Transactions.Add(transaction);
                    }
                }
            }, "Fetching products...");
        }


        private async Task ExecuteAsync(Func<Task> operation, string? busyText = null)
        {
            IsBusy = true;
            BusyText = busyText ?? "Processing...";
            try
            {
                await operation?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
                BusyText = "Processing...";
            }
        }
    }
}
