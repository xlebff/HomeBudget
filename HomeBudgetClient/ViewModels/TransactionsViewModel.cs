using CommunityToolkit.Mvvm.ComponentModel;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;

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

        public async Task LoadTransactionsAsync()
        {
            await ExecuteAsync(async () =>
            {
                List<Transaction> transactions = [.. (await _context.GetAllAsync<Transaction>())];

                if (transactions is not null && transactions.Count != 0)
                {
                    Transactions ??= [];

                    foreach (var transaction in transactions)
                    {
                        Transactions.Add(transaction);
                    }
                }
            }, "Fetching products...");
        }

        public async Task<List<Transaction>> FilteredTransactionsAsync(
            Expression<Func<Transaction, bool>> predicate)
        {
            return [.. await _context
                .GetFilteredAsync<Transaction>(predicate)];
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
