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
/// MCP Tools for financial transaction management operations.
/// </summary>
[McpServerToolType]
public class TransactionTools
{
    private readonly IMediator _mediator;
    private readonly ILifeOSDbContext _dbContext;
    private readonly IMcpApiKeyValidator _apiKeyValidator;

    public TransactionTools(
        IMediator mediator,
        ILifeOSDbContext dbContext,
        IMcpApiKeyValidator apiKeyValidator)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _apiKeyValidator = apiKeyValidator;
    }

    /// <summary>
    /// List transactions for the authenticated user.
    /// </summary>
    [McpServerTool(Name = "listTransactions"), Description("List financial transactions with optional filtering by account, category, or date range. Example response: { Success: true, Data: { Transactions: [ { Id: <guid>, AccountId: <guid>, Date: \"2025-12-12T00:00:00Z\", Amount: -45.20, Description: \"Groceries\", Category: \"Expense\" } ], TotalCount: 1, TotalIncome: 0, TotalExpenses: 45.20, NetAmount: -45.20 }, Error: null }")]
    public async Task<McpToolResponse<ListTransactionsResponse>> ListTransactions(
        [Description("API key for authentication")] string apiKey,
        [Description("Request parameters with filters")] ListTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<ListTransactionsResponse>.Fail(authResult.Error!);

        var query = _dbContext.Transactions
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .Where(t => t.UserId == authResult.UserId);

        // Apply account filter
        if (request.AccountId != Guid.Empty)
        {
            query = query.Where(t => t.SourceAccountId == request.AccountId || t.TargetAccountId == request.AccountId);
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(request.CategoryFilter) &&
            Enum.TryParse<TransactionCategory>(request.CategoryFilter, true, out var category))
        {
            query = query.Where(t => t.Category == category);
        }

        // Apply date range filter
        if (request.StartDate != default)
        {
            var startDateOnly = DateOnly.FromDateTime(request.StartDate);
            query = query.Where(t => t.TransactionDate >= startDateOnly);
        }

        if (request.EndDate != default)
        {
            var endDateOnly = DateOnly.FromDateTime(request.EndDate);
            query = query.Where(t => t.TransactionDate <= endDateOnly);
        }

        // Default to last 30 days if no date range specified
        if (request.StartDate == default && request.EndDate == default)
        {
            var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            query = query.Where(t => t.TransactionDate >= thirtyDaysAgo);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(request.Limit > 0 ? request.Limit : 100)
            .Select(t => new TransactionSummary
            {
                Id = t.Id,
                AccountId = t.SourceAccountId ?? t.TargetAccountId ?? Guid.Empty,
                AccountName = t.SourceAccount != null ? t.SourceAccount.Name : (t.TargetAccount != null ? t.TargetAccount.Name : string.Empty),
                Date = t.TransactionDate.ToDateTime(TimeOnly.MinValue),
                Amount = t.Amount,
                Description = t.Description ?? string.Empty,
                Category = t.Category.ToString(),
                Type = t.Amount >= 0 ? "income" : "expense",
                IsRecurring = false
            })
            .ToListAsync(cancellationToken);

        var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));

        return McpToolResponse<ListTransactionsResponse>.Ok(new ListTransactionsResponse
        {
            Transactions = transactions,
            TotalCount = transactions.Count,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetAmount = totalIncome - totalExpenses
        });
    }

    /// <summary>
    /// Get detailed information about a specific transaction.
    /// </summary>
    [McpServerTool(Name = "getTransaction"), Description("Get detailed information about a specific transaction. Example response: { Success: true, Data: { Transaction: { Id: <guid>, AccountId: <guid>, Date: \"2025-12-12T00:00:00Z\", Amount: -45.20, Description: \"Groceries\", Category: \"Expense\", Notes: \"\" } }, Error: null }")]
    public async Task<McpToolResponse<GetTransactionResponse>> GetTransaction(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the transaction ID")] GetTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetTransactionResponse>.Fail(authResult.Error!);

        var transaction = await _dbContext.Transactions
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .Where(t => t.Id == request.TransactionId && t.UserId == authResult.UserId)
            .Select(t => new TransactionDetail
            {
                Id = t.Id,
                AccountId = t.SourceAccountId ?? t.TargetAccountId ?? Guid.Empty,
                AccountName = t.SourceAccount != null ? t.SourceAccount.Name : (t.TargetAccount != null ? t.TargetAccount.Name : string.Empty),
                Date = t.TransactionDate.ToDateTime(TimeOnly.MinValue),
                Amount = t.Amount,
                Description = t.Description ?? string.Empty,
                Category = t.Category.ToString(),
                Type = t.Amount >= 0 ? "income" : "expense",
                IsRecurring = false,
                RecurrencePattern = string.Empty,
                Notes = t.Notes ?? string.Empty,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt ?? t.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction == null)
            return McpToolResponse<GetTransactionResponse>.Fail($"Transaction with ID {request.TransactionId} not found.");

        return McpToolResponse<GetTransactionResponse>.Ok(new GetTransactionResponse
        {
            Transaction = transaction
        });
    }

    /// <summary>
    /// Create a new transaction.
    /// </summary>
    [McpServerTool(Name = "createTransaction"), Description("Create a new financial transaction. Use negative amounts for expenses, positive for income. Example response: { Success: true, Data: { Success: true, TransactionId: <guid>, Message: \"Transaction created: Groceries (-$45.20).\", AccountBalanceUpdate: { AccountId: <guid>, PreviousBalance: 1200.00, NewBalance: 1154.80 } }, Error: null }")]
    public async Task<McpToolResponse<CreateTransactionResponse>> CreateTransaction(
        [Description("API key for authentication")] string apiKey,
        [Description("Transaction creation parameters")] CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<CreateTransactionResponse>.Fail(authResult.Error!);

        // Verify account exists and belongs to user
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == authResult.UserId, cancellationToken);

        if (account == null)
            return McpToolResponse<CreateTransactionResponse>.Fail($"Account with ID {request.AccountId} not found.");

        if (!Enum.TryParse<TransactionCategory>(request.Category, true, out var category))
            category = request.Amount >= 0 ? TransactionCategory.Income : TransactionCategory.Expense;

        var now = DateTime.UtcNow;
        var transactionDate = request.Date != default ? DateOnly.FromDateTime(request.Date) : DateOnly.FromDateTime(now);

        var transaction = new LifeOS.Domain.Entities.Transaction
        {
            UserId = authResult.UserId,
            SourceAccountId = request.Amount < 0 ? request.AccountId : null,
            TargetAccountId = request.Amount >= 0 ? request.AccountId : null,
            TransactionDate = transactionDate,
            Amount = Math.Abs(request.Amount),
            Description = request.Description,
            Category = category,
            Notes = request.Notes,
            Currency = account.Currency,
            Source = "mcp"
        };

        // Update account balance
        var previousBalance = account.CurrentBalance;
        account.CurrentBalance += request.Amount;
        account.BalanceUpdatedAt = now;
        account.UpdatedAt = now;

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<CreateTransactionResponse>.Ok(new CreateTransactionResponse
        {
            TransactionId = transaction.Id,
            Success = true,
            Message = $"Transaction created: {request.Description} ({request.Amount:C}).",
            AccountBalanceUpdate = new AccountBalanceUpdate
            {
                AccountId = account.Id,
                AccountName = account.Name,
                PreviousBalance = previousBalance,
                NewBalance = account.CurrentBalance,
                TransactionAmount = request.Amount
            }
        });
    }

    /// <summary>
    /// Update an existing transaction.
    /// </summary>
    [McpServerTool(Name = "updateTransaction"), Description("Update an existing transaction's details. Note: Changing the amount will adjust the account balance. Example response: { Success: true, Data: { Success: true, UpdatedTransaction: { Id: <guid>, Date: \"2025-12-12T00:00:00Z\", Amount: -40.00, Description: \"Groceries\", Category: \"Expense\" }, AccountBalanceUpdate: { AccountId: <guid>, PreviousBalance: 1154.80, NewBalance: 1160.00 } }, Error: null }")]
    public async Task<McpToolResponse<UpdateTransactionResponse>> UpdateTransaction(
        [Description("API key for authentication")] string apiKey,
        [Description("Transaction update parameters")] UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<UpdateTransactionResponse>.Fail(authResult.Error!);

        var transaction = await _dbContext.Transactions
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == authResult.UserId, cancellationToken);

        if (transaction == null)
            return McpToolResponse<UpdateTransactionResponse>.Fail($"Transaction with ID {request.TransactionId} not found.");

        // Get the account associated with this transaction
        var account = transaction.SourceAccount ?? transaction.TargetAccount;
        if (account == null)
            return McpToolResponse<UpdateTransactionResponse>.Fail("No account associated with this transaction.");

        // Calculate balance adjustment if amount changed
        var oldAmount = transaction.SourceAccountId.HasValue ? -transaction.Amount : transaction.Amount;
        var newAmount = request.Amount;
        var amountDifference = newAmount - oldAmount;
        var previousBalance = account.CurrentBalance;

        // Update transaction
        if (request.Date != default)
            transaction.TransactionDate = DateOnly.FromDateTime(request.Date);

        transaction.Amount = Math.Abs(request.Amount);
        transaction.Description = request.Description;

        if (!string.IsNullOrEmpty(request.Category) &&
            Enum.TryParse<TransactionCategory>(request.Category, true, out var category))
        {
            transaction.Category = category;
        }

        transaction.Notes = request.Notes;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Adjust account balance for amount difference
        if (amountDifference != 0)
        {
            account.CurrentBalance += amountDifference;
            account.BalanceUpdatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<UpdateTransactionResponse>.Ok(new UpdateTransactionResponse
        {
            Success = true,
            Message = $"Transaction updated successfully.",
            UpdatedTransaction = new TransactionSummary
            {
                Id = transaction.Id,
                AccountId = transaction.SourceAccountId ?? transaction.TargetAccountId ?? Guid.Empty,
                AccountName = account.Name,
                Date = transaction.TransactionDate.ToDateTime(TimeOnly.MinValue),
                Amount = request.Amount,
                Description = transaction.Description ?? string.Empty,
                Category = transaction.Category.ToString(),
                Type = request.Amount >= 0 ? "income" : "expense",
                IsRecurring = false
            },
            AccountBalanceUpdate = amountDifference != 0 ? new AccountBalanceUpdate
            {
                AccountId = account.Id,
                AccountName = account.Name,
                PreviousBalance = previousBalance,
                NewBalance = account.CurrentBalance,
                TransactionAmount = amountDifference
            } : null
        });
    }

    /// <summary>
    /// Delete a transaction.
    /// </summary>
    [McpServerTool(Name = "deleteTransaction"), Description("Delete a transaction. This will reverse its effect on the account balance. Example response: { Success: true, Data: { Success: true, DeletedTransactionId: <guid>, Message: \"Transaction deleted. Account balance adjusted from $1,154.80 to $1,200.00.\", AccountBalanceUpdate: { AccountId: <guid>, PreviousBalance: 1154.80, NewBalance: 1200.00 } }, Error: null }")]
    public async Task<McpToolResponse<DeleteTransactionResponse>> DeleteTransaction(
        [Description("API key for authentication")] string apiKey,
        [Description("Request containing the transaction ID to delete")] DeleteTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<DeleteTransactionResponse>.Fail(authResult.Error!);

        var transaction = await _dbContext.Transactions
            .Include(t => t.SourceAccount)
            .Include(t => t.TargetAccount)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == authResult.UserId, cancellationToken);

        if (transaction == null)
            return McpToolResponse<DeleteTransactionResponse>.Fail($"Transaction with ID {request.TransactionId} not found.");

        var account = transaction.SourceAccount ?? transaction.TargetAccount;
        if (account == null)
            return McpToolResponse<DeleteTransactionResponse>.Fail("No account associated with this transaction.");

        var previousBalance = account.CurrentBalance;

        // Reverse the transaction effect on balance
        var transactionEffect = transaction.SourceAccountId.HasValue ? -transaction.Amount : transaction.Amount;
        account.CurrentBalance -= transactionEffect;
        account.BalanceUpdatedAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;

        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return McpToolResponse<DeleteTransactionResponse>.Ok(new DeleteTransactionResponse
        {
            Success = true,
            Message = $"Transaction deleted. Account balance adjusted from {previousBalance:C} to {account.CurrentBalance:C}.",
            DeletedTransactionId = request.TransactionId,
            AccountBalanceUpdate = new AccountBalanceUpdate
            {
                AccountId = account.Id,
                AccountName = account.Name,
                PreviousBalance = previousBalance,
                NewBalance = account.CurrentBalance,
                TransactionAmount = -transactionEffect
            }
        });
    }

    /// <summary>
    /// Get transaction categories with spending summary.
    /// </summary>
    [McpServerTool(Name = "getTransactionCategories"), Description("Get a list of transaction categories with spending totals for the specified period. Example response: { Success: true, Data: { Categories: [ { Category: \"Expense\", TransactionCount: 12, TotalAmount: -420.10 } ], StartDate: \"2025-11-12T00:00:00Z\", EndDate: \"2025-12-12T00:00:00Z\", TotalIncome: 2500.00, TotalExpenses: 420.10 }, Error: null }")]
    public async Task<McpToolResponse<GetTransactionCategoriesResponse>> GetTransactionCategories(
        [Description("API key for authentication")] string apiKey,
        [Description("Request with date range for analysis")] GetTransactionCategoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _apiKeyValidator.ValidateAndGetUserIdAsync(apiKey, cancellationToken);
        if (!authResult.IsValid)
            return McpToolResponse<GetTransactionCategoriesResponse>.Fail(authResult.Error!);

        var startDate = request.StartDate != default
            ? DateOnly.FromDateTime(request.StartDate)
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = request.EndDate != default
            ? DateOnly.FromDateTime(request.EndDate)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var categories = await _dbContext.Transactions
            .Where(t => t.UserId == authResult.UserId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .GroupBy(t => t.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key.ToString(),
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.SourceAccountId.HasValue ? -t.Amount : t.Amount),
                IncomeAmount = g.Where(t => t.TargetAccountId.HasValue).Sum(t => t.Amount),
                ExpenseAmount = g.Where(t => t.SourceAccountId.HasValue).Sum(t => t.Amount)
            })
            .OrderByDescending(c => Math.Abs(c.TotalAmount))
            .ToListAsync(cancellationToken);

        return McpToolResponse<GetTransactionCategoriesResponse>.Ok(new GetTransactionCategoriesResponse
        {
            Categories = categories,
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            EndDate = endDate.ToDateTime(TimeOnly.MinValue),
            TotalIncome = categories.Sum(c => c.IncomeAmount),
            TotalExpenses = categories.Sum(c => c.ExpenseAmount)
        });
    }
}
