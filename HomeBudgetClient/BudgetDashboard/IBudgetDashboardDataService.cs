using System;
using System.Threading;
using System.Threading.Tasks;

public interface IBudgetDashboardDataService
{
    Task<DashboardPageData> GetDashboardPageDataAsync(DateTime referenceDate, CancellationToken cancellationToken = default);
    Task<TransactionsPageData> GetTransactionsPageDataAsync(CancellationToken cancellationToken = default);
    Task SetTransactionConsideredAsync(Guid transactionId, bool isConsidered, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
