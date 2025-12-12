using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

#region List Accounts

/// <summary>
/// Request to list financial accounts.
/// </summary>
public class ListAccountsRequest
{
    [Description("Filter by account type: 'checking', 'savings', 'investment', 'credit', 'loan', 'property', 'vehicle' (leave empty for all)")]
    public string AccountTypeFilter { get; set; } = string.Empty;
}

/// <summary>
/// Response containing list of accounts.
/// </summary>
public class ListAccountsResponse
{
    [Description("Array of accounts matching the filter criteria")]
    public List<AccountSummary> Accounts { get; set; } = new();

    [Description("Total number of accounts returned")]
    public int TotalCount { get; set; }

    [Description("Total balance across all returned accounts")]
    public decimal TotalBalance { get; set; }

    [Description("Total assets value (positive balances)")]
    public decimal TotalAssets { get; set; }

    [Description("Total liabilities value (negative balances)")]
    public decimal TotalLiabilities { get; set; }
}

/// <summary>
/// Summary of an account for list views.
/// </summary>
public class AccountSummary
{
    [Description("Unique identifier for the account")]
    public Guid Id { get; set; }

    [Description("Account name")]
    public string Name { get; set; } = string.Empty;

    [Description("Account type: 'checking', 'savings', 'investment', 'credit', 'loan', 'property', 'vehicle'")]
    public string AccountType { get; set; } = string.Empty;

    [Description("Currency code (e.g., 'USD', 'EUR', 'ZAR')")]
    public string Currency { get; set; } = string.Empty;

    [Description("Current balance in account currency")]
    public decimal CurrentBalance { get; set; }

    [Description("Financial institution name")]
    public string Institution { get; set; } = string.Empty;

    [Description("Whether the account is active")]
    public bool IsActive { get; set; }
}

#endregion

#region Get Account

/// <summary>
/// Request to get a single account by ID.
/// </summary>
public class GetAccountRequest
{
    [Description("The unique identifier of the account to retrieve")]
    public Guid AccountId { get; set; }
}

/// <summary>
/// Response for a single account with full details.
/// </summary>
public class GetAccountResponse
{
    [Description("The requested account details")]
    public AccountDetail Account { get; set; } = new();
}

/// <summary>
/// Full account details.
/// </summary>
public class AccountDetail
{
    [Description("Unique identifier for the account")]
    public Guid Id { get; set; }

    [Description("Account name")]
    public string Name { get; set; } = string.Empty;

    [Description("Account type")]
    public string AccountType { get; set; } = string.Empty;

    [Description("Currency code")]
    public string Currency { get; set; } = string.Empty;

    [Description("Current balance in account currency")]
    public decimal CurrentBalance { get; set; }

    [Description("Financial institution")]
    public string Institution { get; set; } = string.Empty;

    [Description("Account number (masked or partial)")]
    public string AccountNumber { get; set; } = string.Empty;

    [Description("Notes about the account")]
    public string Notes { get; set; } = string.Empty;

    [Description("Whether the account is active")]
    public bool IsActive { get; set; }

    [Description("Number of transactions in the last 30 days")]
    public int RecentTransactionCount { get; set; }

    [Description("When the account was created")]
    public DateTime CreatedAt { get; set; }

    [Description("When the account was last updated")]
    public DateTime UpdatedAt { get; set; }
}

#endregion

#region Create Account

/// <summary>
/// Request to create a new account.
/// </summary>
public class CreateAccountRequest
{
    [Description("Account name (required)")]
    public string Name { get; set; } = string.Empty;

    [Description("Account type (required): 'checking', 'savings', 'investment', 'credit', 'loan', 'property', 'vehicle'")]
    public string AccountType { get; set; } = string.Empty;

    [Description("Currency code (e.g., 'USD', 'EUR', 'ZAR')")]
    public string Currency { get; set; } = "USD";

    [Description("Initial balance")]
    public decimal InitialBalance { get; set; }

    [Description("Financial institution name")]
    public string Institution { get; set; } = string.Empty;

    [Description("Account number (for reference)")]
    public string AccountNumber { get; set; } = string.Empty;

    [Description("Notes about the account")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Response after creating an account.
/// </summary>
public class CreateAccountResponse
{
    [Description("The ID of the newly created account")]
    public Guid AccountId { get; set; }

    [Description("Whether the creation was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;
}

#endregion

#region Update Account

/// <summary>
/// Request to update an existing account.
/// </summary>
public class UpdateAccountRequest
{
    [Description("The unique identifier of the account to update")]
    public Guid AccountId { get; set; }

    [Description("Account name")]
    public string Name { get; set; } = string.Empty;

    [Description("Account type")]
    public string AccountType { get; set; } = string.Empty;

    [Description("Currency code")]
    public string Currency { get; set; } = string.Empty;

    [Description("Institution name")]
    public string Institution { get; set; } = string.Empty;

    [Description("Account number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Description("Notes about the account")]
    public string Notes { get; set; } = string.Empty;

    [Description("Whether the account is active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Response after updating an account.
/// </summary>
public class UpdateAccountResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("The updated account summary")]
    public AccountSummary UpdatedAccount { get; set; } = new();
}

#endregion

#region Delete Account

/// <summary>
/// Request to delete an account.
/// </summary>
public class DeleteAccountRequest
{
    [Description("The unique identifier of the account to delete")]
    public Guid AccountId { get; set; }
}

/// <summary>
/// Response after deleting an account.
/// </summary>
public class DeleteAccountResponse
{
    [Description("Whether the deletion was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("ID of the deleted account")]
    public Guid DeletedAccountId { get; set; }

    [Description("Number of associated transactions that were deleted")]
    public int DeletedTransactionCount { get; set; }
}

#endregion

#region Update Account Balance

/// <summary>
/// Request to update an account's balance directly.
/// </summary>
public class UpdateAccountBalanceRequest
{
    [Description("The unique identifier of the account to update")]
    public Guid AccountId { get; set; }

    [Description("The new balance value")]
    public decimal NewBalance { get; set; }
}

/// <summary>
/// Response after updating an account's balance.
/// </summary>
public class UpdateAccountBalanceResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("ID of the updated account")]
    public Guid AccountId { get; set; }

    [Description("Name of the updated account")]
    public string AccountName { get; set; } = string.Empty;

    [Description("Previous balance before the update")]
    public decimal PreviousBalance { get; set; }

    [Description("New balance after the update")]
    public decimal NewBalance { get; set; }

    [Description("Change in balance (new - previous)")]
    public decimal BalanceChange { get; set; }
}

#endregion
