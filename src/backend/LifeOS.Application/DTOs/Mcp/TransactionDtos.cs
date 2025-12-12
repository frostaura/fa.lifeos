using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

#region List Transactions

/// <summary>
/// Request to list transactions with filtering options.
/// </summary>
public class ListTransactionsRequest
{
    [Description("Filter by account ID (leave empty for all accounts)")]
    public Guid AccountId { get; set; }

    [Description("Filter by category (e.g., 'food', 'transport', 'income')")]
    public string CategoryFilter { get; set; } = string.Empty;

    [Description("Start date for date range filter")]
    public DateTime StartDate { get; set; }

    [Description("End date for date range filter")]
    public DateTime EndDate { get; set; }

    [Description("Maximum number of transactions to return (default 100)")]
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Response containing list of transactions.
/// </summary>
public class ListTransactionsResponse
{
    [Description("Array of transactions matching the filter criteria")]
    public List<TransactionSummary> Transactions { get; set; } = new();

    [Description("Total number of transactions returned")]
    public int TotalCount { get; set; }

    [Description("Total income (positive amounts)")]
    public decimal TotalIncome { get; set; }

    [Description("Total expenses (absolute value of negative amounts)")]
    public decimal TotalExpenses { get; set; }

    [Description("Net amount (income - expenses)")]
    public decimal NetAmount { get; set; }
}

/// <summary>
/// Summary of a transaction for list views.
/// </summary>
public class TransactionSummary
{
    [Description("Unique identifier for the transaction")]
    public Guid Id { get; set; }

    [Description("Account ID this transaction belongs to")]
    public Guid AccountId { get; set; }

    [Description("Account name")]
    public string AccountName { get; set; } = string.Empty;

    [Description("Transaction date")]
    public DateTime Date { get; set; }

    [Description("Transaction amount (negative for expenses, positive for income)")]
    public decimal Amount { get; set; }

    [Description("Transaction description")]
    public string Description { get; set; } = string.Empty;

    [Description("Transaction category")]
    public string Category { get; set; } = string.Empty;

    [Description("Transaction type: 'income' or 'expense'")]
    public string Type { get; set; } = string.Empty;

    [Description("Whether this is a recurring transaction")]
    public bool IsRecurring { get; set; }
}

#endregion

#region Get Transaction

/// <summary>
/// Request to get a single transaction by ID.
/// </summary>
public class GetTransactionRequest
{
    [Description("The unique identifier of the transaction to retrieve")]
    public Guid TransactionId { get; set; }
}

/// <summary>
/// Response for a single transaction with full details.
/// </summary>
public class GetTransactionResponse
{
    [Description("The requested transaction details")]
    public TransactionDetail Transaction { get; set; } = new();
}

/// <summary>
/// Full transaction details.
/// </summary>
public class TransactionDetail
{
    [Description("Unique identifier for the transaction")]
    public Guid Id { get; set; }

    [Description("Account ID this transaction belongs to")]
    public Guid AccountId { get; set; }

    [Description("Account name")]
    public string AccountName { get; set; } = string.Empty;

    [Description("Transaction date")]
    public DateTime Date { get; set; }

    [Description("Transaction amount (negative for expenses, positive for income)")]
    public decimal Amount { get; set; }

    [Description("Transaction description")]
    public string Description { get; set; } = string.Empty;

    [Description("Transaction category")]
    public string Category { get; set; } = string.Empty;

    [Description("Transaction type: 'income' or 'expense'")]
    public string Type { get; set; } = string.Empty;

    [Description("Whether this is a recurring transaction")]
    public bool IsRecurring { get; set; }

    [Description("Recurrence pattern (e.g., 'monthly', 'weekly')")]
    public string RecurrencePattern { get; set; } = string.Empty;

    [Description("Additional notes")]
    public string Notes { get; set; } = string.Empty;

    [Description("When the transaction was created")]
    public DateTime CreatedAt { get; set; }

    [Description("When the transaction was last updated")]
    public DateTime UpdatedAt { get; set; }
}

#endregion

#region Create Transaction

/// <summary>
/// Request to create a new transaction.
/// </summary>
public class CreateTransactionRequest
{
    [Description("Account ID to record the transaction against (required)")]
    public Guid AccountId { get; set; }

    [Description("Transaction amount (negative for expenses, positive for income)")]
    public decimal Amount { get; set; }

    [Description("Transaction description")]
    public string Description { get; set; } = string.Empty;

    [Description("Category (e.g., 'food', 'transport', 'salary', 'utilities')")]
    public string Category { get; set; } = string.Empty;

    [Description("Transaction date (uses current date if not specified)")]
    public DateTime Date { get; set; }

    [Description("Whether this is a recurring transaction")]
    public bool IsRecurring { get; set; }

    [Description("Recurrence pattern if recurring (e.g., 'monthly', 'weekly')")]
    public string RecurrencePattern { get; set; } = string.Empty;

    [Description("Additional notes")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Response after creating a transaction.
/// </summary>
public class CreateTransactionResponse
{
    [Description("The ID of the newly created transaction")]
    public Guid TransactionId { get; set; }

    [Description("Whether the creation was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("Updated account balance information")]
    public AccountBalanceUpdate AccountBalanceUpdate { get; set; } = new();
}

#endregion

#region Update Transaction

/// <summary>
/// Request to update an existing transaction.
/// </summary>
public class UpdateTransactionRequest
{
    [Description("The unique identifier of the transaction to update")]
    public Guid TransactionId { get; set; }

    [Description("Transaction date")]
    public DateTime Date { get; set; }

    [Description("Transaction amount (negative for expenses, positive for income)")]
    public decimal Amount { get; set; }

    [Description("Transaction description")]
    public string Description { get; set; } = string.Empty;

    [Description("Transaction category")]
    public string Category { get; set; } = string.Empty;

    [Description("Whether this is a recurring transaction")]
    public bool IsRecurring { get; set; }

    [Description("Recurrence pattern if recurring")]
    public string RecurrencePattern { get; set; } = string.Empty;

    [Description("Additional notes")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Response after updating a transaction.
/// </summary>
public class UpdateTransactionResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("The updated transaction summary")]
    public TransactionSummary UpdatedTransaction { get; set; } = new();

    [Description("Account balance update if amount changed (null if no change)")]
    public AccountBalanceUpdate? AccountBalanceUpdate { get; set; }
}

#endregion

#region Delete Transaction

/// <summary>
/// Request to delete a transaction.
/// </summary>
public class DeleteTransactionRequest
{
    [Description("The unique identifier of the transaction to delete")]
    public Guid TransactionId { get; set; }
}

/// <summary>
/// Response after deleting a transaction.
/// </summary>
public class DeleteTransactionResponse
{
    [Description("Whether the deletion was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("ID of the deleted transaction")]
    public Guid DeletedTransactionId { get; set; }

    [Description("Account balance update after reversal")]
    public AccountBalanceUpdate AccountBalanceUpdate { get; set; } = new();
}

/// <summary>
/// Account balance update after transaction modification.
/// </summary>
public class AccountBalanceUpdate
{
    [Description("Account ID")]
    public Guid AccountId { get; set; }

    [Description("Account name")]
    public string AccountName { get; set; } = string.Empty;

    [Description("Balance before the change")]
    public decimal PreviousBalance { get; set; }

    [Description("Balance after the change")]
    public decimal NewBalance { get; set; }

    [Description("The transaction amount that caused the change")]
    public decimal TransactionAmount { get; set; }
}

#endregion

#region Get Transaction Categories

/// <summary>
/// Request to get transaction categories with spending summary.
/// </summary>
public class GetTransactionCategoriesRequest
{
    [Description("Start date for the analysis period")]
    public DateTime StartDate { get; set; }

    [Description("End date for the analysis period")]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Response containing category spending summary.
/// </summary>
public class GetTransactionCategoriesResponse
{
    [Description("List of categories with spending totals")]
    public List<CategorySummary> Categories { get; set; } = new();

    [Description("Start date of the analysis period")]
    public DateTime StartDate { get; set; }

    [Description("End date of the analysis period")]
    public DateTime EndDate { get; set; }

    [Description("Total income across all categories")]
    public decimal TotalIncome { get; set; }

    [Description("Total expenses across all categories")]
    public decimal TotalExpenses { get; set; }
}

/// <summary>
/// Summary of spending by category.
/// </summary>
public class CategorySummary
{
    [Description("Category name")]
    public string Category { get; set; } = string.Empty;

    [Description("Number of transactions in this category")]
    public int TransactionCount { get; set; }

    [Description("Total amount (income - expenses)")]
    public decimal TotalAmount { get; set; }

    [Description("Total income in this category")]
    public decimal IncomeAmount { get; set; }

    [Description("Total expenses in this category")]
    public decimal ExpenseAmount { get; set; }
}

#endregion
