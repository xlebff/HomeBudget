using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.EntityFrameworkCore;

public sealed class BudgetDashboardDataService : IBudgetDashboardDataService
{
    private const string DefaultCurrencyCode = "RUB";
    private const string DefaultCurrencySymbol = "₽";
    private static readonly string[] TransactionItemNavigationNames = ["Items", "Positions", "Lines", "TransactionItems"];
    private static readonly string[] SoftDeletePropertyNames = ["IsDeleted", "Deleted", "IsRemoved"];

    private readonly AppDbContext _context;

    public BudgetDashboardDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardPageData> GetDashboardPageDataAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var categories = await GetActiveCategoriesAsync(cancellationToken);
        var currencies = await GetCurrenciesAsync(cancellationToken);
        var allVisibleTransactions = await GetAllVisibleTransactionsAsync(cancellationToken);

        var incomeCategoryIds = categories
            .Where(category => IsCategoryType(category, "income"))
            .Select(category => category.Id)
            .ToHashSet();

        var expenseCategoryIds = categories
            .Where(category => IsCategoryType(category, "expense"))
            .Select(category => category.Id)
            .ToHashSet();

        var consideredTransactions = allVisibleTransactions
            .Where(transaction => transaction.IsConsidered)
            .ToList();

        var effectiveReferenceDate = ResolveEffectiveReferenceDate(referenceDate, consideredTransactions, incomeCategoryIds, expenseCategoryIds);
        var monthStart = new DateTime(effectiveReferenceDate.Year, effectiveReferenceDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var monthTransactions = consideredTransactions
            .Where(transaction => IsTransactionInMonth(transaction, monthStart, monthEnd))
            .ToList();

        var incomeTransactions = monthTransactions
            .Where(transaction => transaction.CategoryId.HasValue && incomeCategoryIds.Contains(transaction.CategoryId.Value))
            .ToList();

        var expenseTransactions = monthTransactions
            .Where(transaction => transaction.CategoryId.HasValue && expenseCategoryIds.Contains(transaction.CategoryId.Value))
            .ToList();

        var totalIncome = incomeTransactions.Sum(GetStatAmount);
        var totalExpense = expenseTransactions.Sum(GetStatAmount);
        var balanceMetric = totalIncome - totalExpense;

        var pageCurrency = ResolveCurrency(monthTransactions, currencies);
        var incomeCurrency = ResolveCurrency(incomeTransactions, currencies, pageCurrency);
        var expenseCurrency = ResolveCurrency(expenseTransactions, currencies, pageCurrency);

        var incomeChart = ChartVm.CreateIncome();
        incomeChart.TotalAmount = totalIncome;
        incomeChart.CurrencyCode = incomeCurrency.Code;
        incomeChart.CurrencySymbol = incomeCurrency.Symbol;
        incomeChart.Slices = BuildChartSlices(incomeTransactions, categories, incomeCategoryIds);

        var expenseChart = ChartVm.CreateExpense();
        expenseChart.TotalAmount = totalExpense;
        expenseChart.CurrencyCode = expenseCurrency.Code;
        expenseChart.CurrencySymbol = expenseCurrency.Symbol;
        expenseChart.Slices = BuildChartSlices(expenseTransactions, categories, expenseCategoryIds);

        var lastTransaction = allVisibleTransactions
            .OrderByDescending(GetTransactionDateSafe)
            .FirstOrDefault();

        return new DashboardPageData
        {
            HeroBadge = BuildHeroBadge(effectiveReferenceDate),
            HeroDescription = BuildHeroDescription(pageCurrency.IsMixed),
            SidebarSummaryHint = BuildSidebarSummaryHint(totalIncome, totalExpense, balanceMetric, pageCurrency.IsMixed),
            CurrencyCode = pageCurrency.Code,
            CurrencySymbol = pageCurrency.Symbol,
            HasMixedCurrencies = pageCurrency.IsMixed,
            DiagnosticText = BuildDashboardDiagnosticText(incomeCategoryIds.Count, expenseCategoryIds.Count, incomeTransactions.Count, expenseTransactions.Count, totalIncome, totalExpense, pageCurrency.IsMixed),
            BalanceMetric = new SummaryMetricVm
            {
                Label = "Общий баланс",
                Value = balanceMetric,
                Note = "Разница между доходами и расходами за выбранный месяц.",
                Accent = balanceMetric switch
                {
                    > 0m => MetricAccent.Positive,
                    < 0m => MetricAccent.Negative,
                    _ => MetricAccent.Neutral
                }
            },
            IncomeMetric = new SummaryMetricVm
            {
                Label = "Доходы",
                Value = totalIncome,
                Note = "Все учтённые поступления за выбранный месяц.",
                Accent = MetricAccent.Positive
            },
            ExpenseMetric = new SummaryMetricVm
            {
                Label = "Расходы",
                Value = totalExpense,
                Note = "Все учтённые списания за выбранный месяц.",
                Accent = MetricAccent.Negative
            },
            IncomeChart = incomeChart,
            ExpenseChart = expenseChart,
            LastTransaction = BuildLastTransactionVm(lastTransaction, categories, currencies),
            MessageCardTitle = "Привет",
            MessageCardText = BuildMessageCardText(balanceMetric, totalIncome, totalExpense)
        };
    }

    public async Task<TransactionsPageData> GetTransactionsPageDataAsync(CancellationToken cancellationToken = default)
    {
        var categories = await GetActiveCategoriesAsync(cancellationToken);
        var currencies = await GetCurrenciesAsync(cancellationToken);
        var allVisibleTransactions = await GetAllVisibleTransactionsAsync(cancellationToken);
        var pageCurrency = ResolveCurrency(allVisibleTransactions, currencies);

        var transactionItems = allVisibleTransactions
            .OrderByDescending(GetTransactionDateSafe)
            .ThenByDescending(GetStatAmount)
            .Select(transaction => BuildTransactionListItemVm(transaction, categories, currencies, pageCurrency))
            .ToList();

        return new TransactionsPageData
        {
            CurrencyCode = pageCurrency.Code,
            CurrencySymbol = pageCurrency.Symbol,
            HasMixedCurrencies = pageCurrency.IsMixed,
            DiagnosticText = BuildTransactionsDiagnosticText(transactionItems.Count, transactionItems.Count(item => item.IsConsidered), transactionItems.Count(item => !item.IsConsidered), pageCurrency.IsMixed),
            TotalCount = transactionItems.Count,
            ConsideredCount = transactionItems.Count(item => item.IsConsidered),
            IgnoredCount = transactionItems.Count(item => !item.IsConsidered),
            Transactions = transactionItems
        };
    }

    public async Task SetTransactionConsideredAsync(Guid transactionId, bool isConsidered, CancellationToken cancellationToken = default)
    {
        var transaction =
            await _context.Transactions.FirstOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
        if (transaction is null)
        {
            return;
        }

        transaction.IsConsidered = isConsidered;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions.FirstOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
        if (transaction is null)
        {
            return;
        }

        var deletedProperty = GetPropertyIgnoreCase(typeof(Transaction), SoftDeletePropertyNames);
        if (deletedProperty is not null && deletedProperty.CanWrite && (deletedProperty.PropertyType == typeof(bool) || deletedProperty.PropertyType == typeof(bool?)))
        {
            deletedProperty.SetValue(transaction, true);
        }
        else
        {
            _context.Transactions.Remove(transaction);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(category => !category.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Transaction>> GetAllVisibleTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await _context.Transactions
            .ToListAsync(cancellationToken);

        transactions = transactions
            .Where(transaction => !IsSoftDeleted(transaction))
            .ToList();

        await LoadTransactionCollectionsAsync(transactions, cancellationToken);

        return transactions;
    }

    private async Task<Dictionary<Guid, CurrencyInfo>> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        var currencies = await _context.Currencies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return currencies
            .GroupBy(currency => currency.Id)
            .ToDictionary(
                group => group.Key,
                group => new CurrencyInfo(
                    GetStringProperty(group.First(), "Code") ?? DefaultCurrencyCode,
                    GetStringProperty(group.First(), "Symbol") ?? DefaultCurrencySymbol));
    }

    private async Task LoadTransactionCollectionsAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken)
    {
        foreach (var transaction in transactions)
        {
            foreach (var navigationName in TransactionItemNavigationNames)
            {
                var navigationProperty = GetPropertyIgnoreCase(transaction.GetType(), navigationName);
                if (navigationProperty is null)
                {
                    continue;
                }

                try
                {
                    var collectionEntry = _context.Entry(transaction).Collection(navigationName);
                    if (!collectionEntry.IsLoaded)
                    {
                        await collectionEntry.LoadAsync(cancellationToken);
                    }
                }
                catch
                {
                    // Спокойно пропускаем: не у всех проектов навигации доступны одинаково.
                }

                break;
            }
        }
    }

    public static DateTime ResolveEffectiveReferenceDate(
        DateTime requestedReferenceDate,
        IEnumerable<Transaction> allTransactions,
        ISet<Guid> incomeCategoryIds,
        ISet<Guid> expenseCategoryIds)
    {
        var monthStart = new DateTime(requestedReferenceDate.Year, requestedReferenceDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var hasRequestedMonthData = allTransactions.Any(transaction =>
            transaction.CategoryId.HasValue &&
            (incomeCategoryIds.Contains(transaction.CategoryId.Value) || expenseCategoryIds.Contains(transaction.CategoryId.Value)) &&
            IsTransactionInMonth(transaction, monthStart, monthEnd));

        if (hasRequestedMonthData)
        {
            return requestedReferenceDate;
        }

        var latestTransactionDate = allTransactions
            .Where(transaction => transaction.CategoryId.HasValue &&
                                  (incomeCategoryIds.Contains(transaction.CategoryId.Value) || expenseCategoryIds.Contains(transaction.CategoryId.Value)))
            .Select(GetTransactionDateNullableSafe)
            .Where(date => date.HasValue)
            .Select(date => date.Value)
            .OrderByDescending(date => date)
            .FirstOrDefault();

        return latestTransactionDate == default ? requestedReferenceDate : latestTransactionDate;
    }

    private static bool IsCategoryType(Category category, string expectedType)
    {
        return string.Equals(category.Type?.Trim(), expectedType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTransactionInMonth(Transaction transaction, DateTime monthStart, DateTime monthEnd)
    {
        var date = GetTransactionDateNullableSafe(transaction);
        return date.HasValue && date.Value >= monthStart && date.Value < monthEnd;
    }

    private static bool IsSoftDeleted(object source)
    {
        var deleted = GetBooleanProperty(source, SoftDeletePropertyNames);
        return deleted ?? false;
    }

    private static decimal GetStatAmount(Transaction transaction)
    {
        return Math.Abs(transaction.TotalAmount);
    }

    private static Guid? GetCurrencyIdSafe(Transaction transaction)
    {
        return GetGuidProperty(transaction, "CurrencyId");
    }

    private static List<ChartSliceVm> BuildChartSlices(IEnumerable<Transaction> transactions, IEnumerable<Category> categories, ISet<Guid> allowedCategoryIds)
    {
        var categoryNames = categories
            .Where(category => allowedCategoryIds.Contains(category.Id))
            .ToDictionary(category => category.Id, category => GetCategoryDisplayName(category));

        return transactions
            .Where(transaction => transaction.CategoryId.HasValue && allowedCategoryIds.Contains(transaction.CategoryId.Value))
            .GroupBy(transaction => transaction.CategoryId!.Value)
            .Select(group => new ChartSliceVm
            {
                Name = categoryNames.TryGetValue(group.Key, out var categoryName) ? categoryName : "Без категории",
                Amount = group.Sum(GetStatAmount)
            })
            .Where(slice => slice.Amount > 0m)
            .OrderByDescending(slice => slice.Amount)
            .ToList();
    }

    private static LastTransactionVm BuildLastTransactionVm(Transaction? transaction, IEnumerable<Category> categories, IReadOnlyDictionary<Guid, CurrencyInfo> currencies)
    {
        if (transaction is null)
        {
            return LastTransactionVm.CreateEmpty();
        }

        var currency = ResolveCurrency(new[] { transaction }, currencies);

        return new LastTransactionVm
        {
            Title = "Последняя транзакция",
            HasData = true,
            Category = ResolveTransactionCategoryName(transaction, categories),
            TypeLabel = ResolveTransactionTypeLabel(transaction, categories),
            TotalAmount = GetStatAmount(transaction),
            Date = GetTransactionDateNullableSafe(transaction),
            Comment = GetStringProperty(transaction, "Comment", "Description", "Note") ?? "—",
            CurrencyCode = currency.Code,
            CurrencySymbol = currency.Symbol,
            Lines = BuildTransactionLines(transaction)
        };
    }

    private static TransactionListItemVm BuildTransactionListItemVm(
        Transaction transaction,
        IEnumerable<Category> categories,
        IReadOnlyDictionary<Guid, CurrencyInfo> currencies,
        CurrencyResolution fallbackCurrency)
    {
        var currency = ResolveCurrency(new[] { transaction }, currencies, fallbackCurrency);

        return new TransactionListItemVm
        {
            Id = transaction.Id,
            Category = ResolveTransactionCategoryName(transaction, categories),
            TypeLabel = ResolveTransactionTypeLabel(transaction, categories),
            TotalAmount = GetStatAmount(transaction),
            Date = GetTransactionDateNullableSafe(transaction),
            Comment = GetStringProperty(transaction, "Comment", "Description", "Note") ?? "—",
            CurrencyCode = currency.Code,
            CurrencySymbol = currency.Symbol,
            IsConsidered = transaction.IsConsidered,
            EditUrl = $"/transactions/edit/{transaction.Id}",
            Lines = BuildTransactionLines(transaction)
        };
    }

    private static List<TransactionLineVm> BuildTransactionLines(Transaction transaction)
    {
        var collectionObject = GetObjectProperty(transaction, TransactionItemNavigationNames);
        if (collectionObject is not IEnumerable items)
        {
            return new List<TransactionLineVm>();
        }

        var lines = new List<TransactionLineVm>();
        foreach (var item in items)
        {
            if (item is null || IsSoftDeleted(item))
            {
                continue;
            }

            lines.Add(new TransactionLineVm
            {
                Title = GetStringProperty(item, "Title", "Name", "ProductName") ?? "Позиция",
                Subtitle = GetStringProperty(item, "Subtitle", "CategoryName", "Description", "Comment") ?? string.Empty,
                Amount = Math.Abs(GetDecimalProperty(item, "Amount", "TotalAmount", "Price", "Sum") ?? 0m)
            });
        }

        return lines;
    }

    private static string ResolveTransactionCategoryName(Transaction transaction, IEnumerable<Category> categories)
    {
        if (!transaction.CategoryId.HasValue)
        {
            return "Без категории";
        }

        var category = categories.FirstOrDefault(item => item.Id == transaction.CategoryId.Value);
        return category is null ? "Без категории" : GetCategoryDisplayName(category);
    }

    private static string ResolveTransactionTypeLabel(Transaction transaction, IEnumerable<Category> categories)
    {
        if (!transaction.CategoryId.HasValue)
        {
            return "—";
        }

        var category = categories.FirstOrDefault(item => item.Id == transaction.CategoryId.Value);
        if (category is null)
        {
            return "—";
        }

        if (IsCategoryType(category, "income"))
        {
            return "Доход";
        }

        if (IsCategoryType(category, "expense"))
        {
            return "Расход";
        }

        return "—";
    }

    private static string GetCategoryDisplayName(Category category)
    {
        return string.IsNullOrWhiteSpace(category.Name) ? "Без категории" : category.Name.Trim();
    }

    private static CurrencyResolution ResolveCurrency(
        IEnumerable<Transaction> transactions,
        IReadOnlyDictionary<Guid, CurrencyInfo> currencies,
        CurrencyResolution? fallback = null)
    {
        var resolved = transactions
            .Select(GetCurrencyIdSafe)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Where(currencies.ContainsKey)
            .Distinct()
            .ToList();

        if (resolved.Count == 1)
        {
            var currency = currencies[resolved[0]];
            return new CurrencyResolution(currency.Code, currency.Symbol, false);
        }

        if (resolved.Count > 1)
        {
            return fallback ?? CurrencyResolution.Default(mixed: true);
        }

        return fallback ?? CurrencyResolution.Default();
    }

    private static string BuildHeroBadge(DateTime referenceDate)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        var monthName = referenceDate.ToString("MMMM yyyy", culture);
        return string.IsNullOrWhiteSpace(monthName)
            ? "Текущий месяц"
            : char.ToUpper(monthName[0], culture) + monthName[1..];
    }

    private static string BuildHeroDescription(bool hasMixedCurrencies)
    {
        return hasMixedCurrencies
            ? "Данные загружены. В периоде есть несколько валют, поэтому суммы показаны без конвертации."
            : "Данные загружены. Сверху — главное, ниже — круговые диаграммы и последняя операция.";
    }

    private static string BuildSidebarSummaryHint(decimal totalIncome, decimal totalExpense, decimal balance, bool hasMixedCurrencies)
    {
        if (hasMixedCurrencies)
        {
            return "В данных встретились разные валюты. Итоги показаны как есть, без автоматической конвертации.";
        }

        if (totalIncome <= 0m && totalExpense <= 0m)
        {
            return "За выбранный период ещё нет учтённых операций. После появления данных вывод обновится автоматически.";
        }

        if (balance > 0m)
        {
            return "Текущий итог месяца. Доходы выше расходов. Это хороший момент, чтобы оценить финансовый запас и свободный остаток.";
        }

        if (balance < 0m)
        {
            return "Текущий итог месяца. Расходы выше доходов. Стоит проверить самые крупные категории и понять, где можно снизить нагрузку.";
        }

        return "Текущий итог месяца. Доходы и расходы сейчас находятся в равновесии.";
    }

    private static string BuildDashboardDiagnosticText(int incomeCategoryCount, int expenseCategoryCount, int incomeTransactionCount, int expenseTransactionCount, decimal totalIncome, decimal totalExpense, bool hasMixedCurrencies)
    {
        return $"Диагностика: категорий доходов — {incomeCategoryCount}, категорий расходов — {expenseCategoryCount}, учтённых доходных операций — {incomeTransactionCount}, учтённых расходных операций — {expenseTransactionCount}, сумма доходов — {totalIncome:N2}, сумма расходов — {totalExpense:N2}." +
               (hasMixedCurrencies ? " В периоде найдено несколько валют." : string.Empty);
    }

    private static string BuildTransactionsDiagnosticText(int totalCount, int consideredCount, int ignoredCount, bool hasMixedCurrencies)
    {
        return $"Диагностика: всего транзакций — {totalCount}, учитываемых — {consideredCount}, исключённых из статистики — {ignoredCount}." +
               (hasMixedCurrencies ? " В журнале присутствуют разные валюты." : string.Empty);
    }

    private static string BuildMessageCardText(decimal balance, decimal totalIncome, decimal totalExpense)
    {
        if (totalIncome <= 0m && totalExpense <= 0m)
        {
            return "Когда появятся данные, здесь можно показывать заметку, семейную цель или подсказку по бюджету.";
        }

        if (balance > 0m)
        {
            return "Привет. В этом месяце баланс положительный. Блок удобно использовать для краткой цели, напоминания или полезной подсказки.";
        }

        if (balance < 0m)
        {
            return "Привет. В этом месяце расходы пока выше доходов. Здесь можно показать мягкий совет проверить самые тяжёлые категории.";
        }

        return "Привет. Баланс месяца сейчас нейтральный. Этот блок подходит для заметок и коротких напоминаний.";
    }

    private static PropertyInfo? GetPropertyIgnoreCase(Type type, IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is not null)
            {
                return property;
            }
        }

        return null;
    }

    private static PropertyInfo? GetPropertyIgnoreCase(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }

    private static string? GetStringProperty(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = GetPropertyIgnoreCase(source.GetType(), propertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(source) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static object? GetObjectProperty(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = GetPropertyIgnoreCase(source.GetType(), propertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(source);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static decimal? GetDecimalProperty(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = GetPropertyIgnoreCase(source.GetType(), propertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(source);
            if (value is decimal decimalValue)
            {
                return decimalValue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is long longValue)
            {
                return longValue;
            }

            if (value is double doubleValue)
            {
                return (decimal)doubleValue;
            }

            if (value is float floatValue)
            {
                return (decimal)floatValue;
            }
        }

        return null;
    }

    private static DateTime? GetDateTimeProperty(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = GetPropertyIgnoreCase(source.GetType(), propertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(source);
            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue;
            }
        }

        return null;
    }

    private static bool? GetBooleanProperty(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = GetPropertyIgnoreCase(source.GetType(), propertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(source);
            if (value is bool booleanValue)
            {
                return booleanValue;
            }
        }

        return null;
    }

    private static Guid? GetGuidProperty(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = GetPropertyIgnoreCase(source.GetType(), propertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(source);
            if (value is Guid guidValue)
            {
                return guidValue;
            }
        }

        return null;
    }

    private static DateTime GetTransactionDateSafe(Transaction transaction)
    {
        return GetTransactionDateNullableSafe(transaction) ?? DateTime.MinValue;
    }

    private static DateTime? GetTransactionDateNullableSafe(Transaction transaction)
    {
        return GetDateTimeProperty(transaction, "Date", "CreatedAt", "TransactionDate");
    }

    private readonly record struct CurrencyInfo(string Code, string Symbol);
    private readonly record struct CurrencyResolution(string Code, string Symbol, bool IsMixed)
    {
        public static CurrencyResolution Default(bool mixed = false) => new(DefaultCurrencyCode, DefaultCurrencySymbol, mixed);
    }
}
