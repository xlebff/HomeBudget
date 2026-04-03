using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class DashboardPageData
{
    public string AppTitle { get; set; } = "Home Budget";
    public string AppDescription { get; set; } = "Панель управления доходами, расходами и последними операциями за выбранный месяц.";
    public string HeroBadge { get; set; } = "Текущий месяц";
    public string HeroTitle { get; set; } = "Доходы и расходы видны с первого экрана";
    public string HeroDescription { get; set; } = "Сверху — главное: общий баланс, суммы за месяц и быстрые действия. Ниже — аналитика и последняя операция с позициями.";
    public string SidebarSummaryTitle { get; set; } = "Текущий итог месяца";
    public string SidebarSummaryHint { get; set; } = "После загрузки данных здесь появится короткий вывод по выбранному периоду.";
    public string MessageCardTitle { get; set; } = "Привет";
    public string MessageCardText { get; set; } = "Здесь можно показывать заметку, подсказку или важное семейное напоминание.";
    public string CurrencySymbol { get; set; } = "₽";
    public string CurrencyCode { get; set; } = "RUB";
    public bool HasMixedCurrencies { get; set; }
    public string DiagnosticText { get; set; } = string.Empty;

    public SummaryMetricVm BalanceMetric { get; set; } = new();
    public SummaryMetricVm IncomeMetric { get; set; } = new();
    public SummaryMetricVm ExpenseMetric { get; set; } = new();

    public ChartVm IncomeChart { get; set; } = ChartVm.CreateIncome();
    public ChartVm ExpenseChart { get; set; } = ChartVm.CreateExpense();

    public LastTransactionVm LastTransaction { get; set; } = LastTransactionVm.CreateEmpty();

    public static DashboardPageData CreateEmpty(DateTime referenceDate)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        var monthName = referenceDate.ToString("MMMM yyyy", culture);
        if (!string.IsNullOrWhiteSpace(monthName))
        {
            monthName = char.ToUpper(monthName[0], culture) + monthName[1..];
        }

        return new DashboardPageData
        {
            HeroBadge = monthName,
            BalanceMetric = new SummaryMetricVm
            {
                Label = "Общий баланс",
                Note = "Разница между доходами и расходами за выбранный месяц.",
                Accent = MetricAccent.Neutral
            },
            IncomeMetric = new SummaryMetricVm
            {
                Label = "Доходы",
                Note = "Все учтённые поступления за выбранный месяц.",
                Accent = MetricAccent.Positive
            },
            ExpenseMetric = new SummaryMetricVm
            {
                Label = "Расходы",
                Note = "Все учтённые списания за выбранный месяц.",
                Accent = MetricAccent.Negative
            },
            IncomeChart = ChartVm.CreateIncome(),
            ExpenseChart = ChartVm.CreateExpense(),
            LastTransaction = LastTransactionVm.CreateEmpty()
        };
    }
}

public sealed class TransactionsPageData
{
    public string AppTitle { get; set; } = "Home Budget";
    public string AppDescription { get; set; } = "Полный список транзакций с быстрыми действиями и позициями.";
    public string HeroBadge { get; set; } = "Журнал операций";
    public string HeroTitle { get; set; } = "Все транзакции в одном месте";
    public string HeroDescription { get; set; } = "Здесь видны все операции, в том числе исключённые из статистики. Удалённые позиции скрываются автоматически.";
    public string CurrencySymbol { get; set; } = "₽";
    public string CurrencyCode { get; set; } = "RUB";
    public bool HasMixedCurrencies { get; set; }
    public string DiagnosticText { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ConsideredCount { get; set; }
    public int IgnoredCount { get; set; }
    public string EmptyStateText { get; set; } = "Транзакции пока не найдены.";
    public List<TransactionListItemVm> Transactions { get; set; } = new();
}

public enum MetricAccent
{
    Neutral,
    Positive,
    Negative
}

public sealed class SummaryMetricVm
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Note { get; set; } = string.Empty;
    public MetricAccent Accent { get; set; }
}

public enum ChartKind
{
    Income,
    Expense
}

public sealed class ChartVm
{
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ChartKind Kind { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencySymbol { get; set; } = "₽";
    public string CurrencyCode { get; set; } = "RUB";
    public string EmptyStateText { get; set; } = "Нет данных";
    public List<ChartSliceVm> Slices { get; set; } = new();

    public static ChartVm CreateIncome() => new()
    {
        Label = "Структура доходов",
        Title = "Доходы за выбранный месяц",
        Kind = ChartKind.Income,
        EmptyStateText = "За выбранный период нет учтённых доходов."
    };

    public static ChartVm CreateExpense() => new()
    {
        Label = "Структура расходов",
        Title = "Расходы за выбранный месяц",
        Kind = ChartKind.Expense,
        EmptyStateText = "За выбранный период нет учтённых расходов."
    };
}

public sealed class ChartSliceVm
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class LastTransactionVm
{
    public string Title { get; set; } = "Последняя транзакция";
    public bool HasData { get; set; }
    public string Category { get; set; } = "—";
    public string TypeLabel { get; set; } = "—";
    public decimal TotalAmount { get; set; }
    public DateTime? Date { get; set; }
    public string Comment { get; set; } = "—";
    public string CurrencySymbol { get; set; } = "₽";
    public string CurrencyCode { get; set; } = "RUB";
    public List<TransactionLineVm> Lines { get; set; } = new();

    public static LastTransactionVm CreateEmpty() => new()
    {
        HasData = false,
        Comment = "Последняя транзакция появится здесь автоматически.",
        Lines = new List<TransactionLineVm>()
    };
}

public sealed class TransactionLineVm
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class TransactionListItemVm
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "Без категории";
    public string TypeLabel { get; set; } = "—";
    public decimal TotalAmount { get; set; }
    public DateTime? Date { get; set; }
    public string Comment { get; set; } = "—";
    public string CurrencySymbol { get; set; } = "₽";
    public string CurrencyCode { get; set; } = "RUB";
    public bool IsConsidered { get; set; }
    public string EditUrl { get; set; } = "/transactions/edit";
    public List<TransactionLineVm> Lines { get; set; } = new();
}
