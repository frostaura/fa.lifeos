using System.ComponentModel;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Services.Mcp;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace LifeOS.Api.Mcp.Tools;

/// <summary>
/// MCP Tools for financial account management operations.
/// </summary>
[McpServerToolType]
public class AccountTools
{
    private readonly IMediator _mediator;
    private readonly ILifeOSDbContext _dbContext;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public AccountTools(
        IMediator mediator,
        ILifeOSDbContext dbContext,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// List all financial accounts for the authenticated user.
    /// </summary>
    [McpServerTool(Name = "listAccounts"), Description("List all financial accounts including bank accounts, credit cards, investments, etc. with their current balances. Example response: { Success: true, Data: { Accounts: [ { Id: <guid>, Name: \"Chase Checking\", AccountType: \"Bank\", Currency: \"USD\", CurrentBalance: 1250.50, IsActive: true } ], TotalCount: 1, TotalBalance: 1250.50 }, Error: null }")]
    public async Task<McpToolResponse<ListAccountsResponse>> ListAccounts(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters with optional type filter")] ListAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<ListAccountsResponse>.Fail(authResult.Error!);

        var query = _dbContext.Accounts.Where(a => a.UserId == authResult.UserId);

        // Apply type filter if provided
        if (!string.IsNullOrEmpty(request.AccountTypeFilter) &&
            Enum.TryParse<AccountType>(request.AccountTypeFilter, true, out var accountType))
        {
            query = query.Where(a => a.AccountType == accountType);
        }

        var accounts = await query
            .OrderBy(a => a.Name)
            .Select(a => new AccountSummary
            {
                Id = a.Id,
                Name = a.Name,
                AccountType = a.AccountType.ToString(),
                Currency = a.Currency,
                CurrentBalance = a.CurrentBalance,
                IsActive = a.IsActive,
                Institution = a.Institution ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        var totalBalance = accounts.Where(a => a.IsActive).Sum(a => a.CurrentBalance);
        var totalAssets = accounts.Where(a => a.IsActive && a.CurrentBalance > 0).Sum(a => a.CurrentBalance);
        var totalLiabilities = accounts.Where(a => a.IsActive && a.CurrentBalance < 0).Sum(a => Math.Abs(a.CurrentBalance));

        return McpToolResponse<ListAccountsResponse>.Ok(new ListAccountsResponse
        {
            Accounts = accounts,
            TotalCount = accounts.Count,
            TotalBalance = totalBalance,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities
        });
    }

    /// <summary>
    /// Get detailed information about a specific financial account.
    /// </summary>
    [McpServerTool(Name = "getAccount"), Description("Get detailed information about a specific financial account including recent transaction summary. Example response: { Success: true, Data: { Account: { Id: <guid>, Name: \"Chase Checking\", Currency: \"USD\", CurrentBalance: 1250.50, RecentTransactionCount: 12 } }, Error: null }")]
    public async Task<McpToolResponse<GetAccountResponse>> GetAccount(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the account ID")] GetAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetAccountResponse>.Fail(authResult.Error!);

        var account = await _dbContext.Accounts
            .Where(a => a.Id == request.AccountId && a.UserId == authResult.UserId)
            .Select(a => new AccountDetail
            {
                Id = a.Id,
                Name = a.Name,
                AccountType = a.AccountType.ToString(),
                Currency = a.Currency,
                CurrentBalance = a.CurrentBalance,
                IsActive = a.IsActive,
                Institution = a.Institution ?? string.Empty,
                AccountNumber = string.Empty,
                Notes = a.Metadata ?? "{}",
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt ?? a.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
            return McpToolResponse<GetAccountResponse>.Fail($"Account with ID {request.AccountId} not found.");

        // Get recent transaction count
        var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var recentTransactionCount = await _dbContext.Transactions
            .CountAsync(t => (t.SourceAccountId == request.AccountId || t.TargetAccountId == request.AccountId)
                && t.TransactionDate >= thirtyDaysAgo, cancellationToken);

        account.RecentTransactionCount = recentTransactionCount;

        return McpToolResponse<GetAccountResponse>.Ok(new GetAccountResponse
        {
            Account = account
        });
    }

    /// <summary>
    /// Create a new financial account.
    /// </summary>
    [McpServerTool(Name = "createAccount"), Description("Create a new financial account to track balances and transactions. Example response: { Success: true, Data: { Success: true, AccountId: <guid>, Message: \"Account 'Chase Checking' created successfully with balance $1,250.50.\" }, Error: null }")]
    public async Task<McpToolResponse<CreateAccountResponse>> CreateAccount(
        [Description("API key for authentication")] string apiKey,
        [Description("Account creation parameters")] CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<CreateAccountResponse>.Fail(authResult.Error!);

        if (string.IsNullOrWhiteSpace(request.Name))
            return McpToolResponse<CreateAccountResponse>.Fail("Account name is required.");

        if (string.IsNullOrWhiteSpace(request.AccountType))
            return McpToolResponse<CreateAccountResponse>.Fail("Account type is required.");

        if (!Enum.TryParse<AccountType>(request.AccountType, true, out var accountType))
            return McpToolResponse<CreateAccountResponse>.Fail($"Invalid account type '{request.AccountType}'. Valid types: Bank, Investment, Loan, Credit, Crypto, Property, Other");

        var account = new LifeOS.Domain.Entities.Account
        {
            UserId = authResult.UserId,
            Name = request.Name,
            AccountType = accountType,
            Currency = request.Currency ?? "USD",
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            IsActive = true,
            Institution = request.Institution,
            Metadata = string.IsNullOrEmpty(request.Notes) ? "{}" : $"{{\"notes\":\"{request.Notes}\"}}"
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<CreateAccountResponse>.Ok(new CreateAccountResponse
        {
            AccountId = account.Id,
            Success = true,
            Message = $"Account '{request.Name}' created successfully with balance {request.InitialBalance:C}."
        });
    }

    /// <summary>
    /// Update an existing financial account.
    /// </summary>
    [McpServerTool(Name = "updateAccount"), Description("Update an existing financial account's details. Example response: { Success: true, Data: { Success: true, Message: \"Account 'Chase Checking' updated successfully.\", UpdatedAccount: { Id: <guid>, Name: \"Chase Checking\", AccountType: \"Bank\", CurrentBalance: 1250.50 } }, Error: null }")]
    public async Task<McpToolResponse<UpdateAccountResponse>> UpdateAccount(
        [Description("API key for authentication")] string apiKey,
        [Description("Account update parameters")] UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateAccountResponse>.Fail(authResult.Error!);

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == authResult.UserId, cancellationToken);

        if (account == null)
            return McpToolResponse<UpdateAccountResponse>.Fail($"Account with ID {request.AccountId} not found.");

        if (!Enum.TryParse<AccountType>(request.AccountType, true, out var accountType))
            return McpToolResponse<UpdateAccountResponse>.Fail($"Invalid account type '{request.AccountType}'.");

        account.Name = request.Name;
        account.AccountType = accountType;
        account.Currency = request.Currency;
        account.Institution = request.Institution;
        account.IsActive = request.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<UpdateAccountResponse>.Ok(new UpdateAccountResponse
        {
            Success = true,
            Message = $"Account '{request.Name}' updated successfully.",
            UpdatedAccount = new AccountSummary
            {
                Id = account.Id,
                Name = account.Name,
                AccountType = account.AccountType.ToString(),
                Currency = account.Currency,
                CurrentBalance = account.CurrentBalance,
                IsActive = account.IsActive,
                Institution = account.Institution ?? string.Empty
            }
        });
    }

    /// <summary>
    /// Delete a financial account.
    /// </summary>
    [McpServerTool(Name = "deleteAccount"), Description("Delete a financial account. This will also delete all associated transactions. Example response: { Success: true, Data: { Success: true, DeletedAccountId: <guid>, DeletedTransactionCount: 42, Message: \"Account 'Chase Checking' and 42 associated transactions deleted successfully.\" }, Error: null }")]
    public async Task<McpToolResponse<DeleteAccountResponse>> DeleteAccount(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the account ID to delete")] DeleteAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<DeleteAccountResponse>.Fail(authResult.Error!);

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == authResult.UserId, cancellationToken);

        if (account == null)
            return McpToolResponse<DeleteAccountResponse>.Fail($"Account with ID {request.AccountId} not found.");

        // Delete associated transactions first
        var transactions = await _dbContext.Transactions
            .Where(t => t.SourceAccountId == request.AccountId || t.TargetAccountId == request.AccountId)
            .ToListAsync(cancellationToken);

        _dbContext.Transactions.RemoveRange(transactions);
        _dbContext.Accounts.Remove(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<DeleteAccountResponse>.Ok(new DeleteAccountResponse
        {
            Success = true,
            Message = $"Account '{account.Name}' and {transactions.Count} associated transactions deleted successfully.",
            DeletedAccountId = request.AccountId,
            DeletedTransactionCount = transactions.Count
        });
    }

    /// <summary>
    /// Update the balance of a financial account.
    /// </summary>
    [McpServerTool(Name = "updateAccountBalance"), Description("Update the current balance of a financial account directly (for reconciliation or manual adjustments). Example response: { Success: true, Data: { Success: true, AccountId: <guid>, AccountName: \"Chase Checking\", PreviousBalance: 1200.00, NewBalance: 1250.50, BalanceChange: 50.50 }, Error: null }")]
    public async Task<McpToolResponse<UpdateAccountBalanceResponse>> UpdateAccountBalance(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing account ID and new balance")] UpdateAccountBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateAccountBalanceResponse>.Fail(authResult.Error!);

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == authResult.UserId, cancellationToken);

        if (account == null)
            return McpToolResponse<UpdateAccountBalanceResponse>.Fail($"Account with ID {request.AccountId} not found.");

        var previousBalance = account.CurrentBalance;
        account.CurrentBalance = request.NewBalance;
        account.BalanceUpdatedAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<UpdateAccountBalanceResponse>.Ok(new UpdateAccountBalanceResponse
        {
            Success = true,
            Message = $"Account '{account.Name}' balance updated from {previousBalance:C} to {request.NewBalance:C}.",
            AccountId = account.Id,
            AccountName = account.Name,
            PreviousBalance = previousBalance,
            NewBalance = request.NewBalance,
            BalanceChange = request.NewBalance - previousBalance
        });
    }
}
