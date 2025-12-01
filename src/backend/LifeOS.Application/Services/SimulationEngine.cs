using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.Simulations;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace LifeOS.Application.Services;

public class SimulationEngine : ISimulationEngine
{
    private readonly ILifeOSDbContext _context;
    private readonly IConditionParser _conditionParser;

    public SimulationEngine(ILifeOSDbContext context, IConditionParser conditionParser)
    {
        _context = context;
        _conditionParser = conditionParser;
    }

    public async Task<RunSimulationData> RunSimulationAsync(
        Guid userId,
        Guid scenarioId,
        bool recalculateFromStart = true,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Load scenario with events
        var scenario = await _context.SimulationScenarios
            .Include(s => s.Events.Where(e => e.IsActive))
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.UserId == userId, cancellationToken);

        if (scenario == null)
            throw new InvalidOperationException("Scenario not found");

        // Load user and accounts
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("User not found");

        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Load income sources
        var incomeSources = await _context.IncomeSources
            .Where(i => i.UserId == userId && i.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Load expense definitions
        var expenseDefinitions = await _context.ExpenseDefinitions
            .Where(e => e.UserId == userId && e.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Load tax profiles
        var taxProfiles = await _context.TaxProfiles
            .Where(t => t.UserId == userId && t.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Parse base assumptions
        var assumptions = ParseAssumptions(scenario.BaseAssumptions);

        // Clear existing projections if recalculating
        if (recalculateFromStart)
        {
            var existingAccountProjections = _context.AccountProjections
                .Where(p => p.ScenarioId == scenarioId);
            _context.AccountProjections.RemoveRange(existingAccountProjections);

            var existingNetWorthProjections = _context.NetWorthProjections
                .Where(p => p.ScenarioId == scenarioId);
            _context.NetWorthProjections.RemoveRange(existingNetWorthProjections);
        }

        // Determine simulation date range
        var startDate = scenario.StartDate;
        var endDate = scenario.EndDate ?? startDate.AddYears(30);

        // Initialize account balances
        var accountBalances = accounts.ToDictionary(a => a.Id, a => a.CurrentBalance);
        var accountInterestRates = accounts.ToDictionary(a => a.Id, a => a.InterestRateAnnual ?? 0m);
        var accountCompounding = accounts.ToDictionary(a => a.Id, a => a.InterestCompounding ?? CompoundingFrequency.None);
        var accountCurrencies = accounts.ToDictionary(a => a.Id, a => a.Currency);
        var accountLiability = accounts.ToDictionary(a => a.Id, a => a.IsLiability);

        // Calculate user age at start
        var userAge = user.DateOfBirth.HasValue
            ? (startDate.Year - user.DateOfBirth.Value.Year)
            : 30; // Default age if not set

        // Track milestones
        var milestones = new List<MilestoneResult>();
        var milestoneTargets = new List<decimal> { 100000, 500000, 1000000, 5000000, 10000000 };
        var milestonesReached = new HashSet<decimal>();

        // Track applied one-time events
        var appliedOneTimeEvents = new HashSet<Guid>();

        // Run month-by-month simulation
        var currentDate = startDate;
        var monthsCalculated = 0;
        var endConditionMet = false;

        while (currentDate <= endDate && !endConditionMet)
        {
            var monthsElapsed = monthsCalculated;
            var currentAge = userAge + (monthsElapsed / 12);

            // Create simulation state for condition evaluation
            var state = CreateSimulationState(
                accountBalances, accountLiability, currentDate, currentAge, monthsElapsed,
                incomeSources, expenseDefinitions);

            // Check end condition
            if (!string.IsNullOrEmpty(scenario.EndCondition) && 
                _conditionParser.Evaluate(scenario.EndCondition, state))
            {
                endConditionMet = true;
            }

            var periodAccountProjections = new List<AccountProjection>();
            var accountIncomes = new Dictionary<Guid, decimal>();
            var accountExpenses = new Dictionary<Guid, decimal>();
            var accountInterests = new Dictionary<Guid, decimal>();
            var eventsAppliedThisPeriod = new List<string>();

            // 1. Apply interest/growth for each account
            foreach (var account in accounts)
            {
                var interest = CalculateMonthlyInterest(
                    accountBalances[account.Id],
                    accountInterestRates[account.Id],
                    accountCompounding[account.Id],
                    assumptions);

                accountBalances[account.Id] += interest;
                accountInterests[account.Id] = interest;
                accountIncomes[account.Id] = 0;
                accountExpenses[account.Id] = 0;
            }

            // 2. Apply income sources
            foreach (var income in incomeSources)
            {
                if (!ShouldApplyRecurringItem(income.PaymentFrequency, currentDate, income.NextPaymentDate))
                    continue;

                var grossAmount = income.BaseAmount;

                // Apply annual increase if applicable
                if (income.AnnualIncreaseRate.HasValue && monthsElapsed > 0)
                {
                    var yearsElapsed = monthsElapsed / 12.0m;
                    grossAmount *= (decimal)Math.Pow((double)(1 + income.AnnualIncreaseRate.Value), (double)yearsElapsed);
                }

                // Calculate net income after tax
                var netAmount = income.IsPreTax && income.TaxProfileId.HasValue
                    ? CalculateNetIncome(grossAmount, income.TaxProfileId.Value, taxProfiles, income.PaymentFrequency)
                    : grossAmount;

                // Find primary bank account to credit (or first available)
                var targetAccountId = accounts.FirstOrDefault(a => a.AccountType == AccountType.Bank && !a.IsLiability)?.Id
                    ?? accounts.FirstOrDefault()?.Id;

                if (targetAccountId.HasValue)
                {
                    accountBalances[targetAccountId.Value] += netAmount;
                    accountIncomes[targetAccountId.Value] += netAmount;
                }
            }

            // 3. Apply expense definitions
            foreach (var expense in expenseDefinitions)
            {
                if (!ShouldApplyRecurringItem(expense.Frequency, currentDate, null))
                    continue;

                var amount = CalculateExpenseAmount(expense, state, assumptions);

                // Debit from linked account or primary bank account
                var sourceAccountId = expense.LinkedAccountId 
                    ?? accounts.FirstOrDefault(a => a.AccountType == AccountType.Bank && !a.IsLiability)?.Id;

                if (sourceAccountId.HasValue)
                {
                    accountBalances[sourceAccountId.Value] -= amount;
                    accountExpenses[sourceAccountId.Value] += amount;
                }
            }

            // 4. Apply simulation events
            foreach (var evt in scenario.Events.OrderBy(e => e.SortOrder))
            {
                if (!evt.IsActive)
                    continue;

                // Check if already applied (for one-time events)
                if (evt.AppliesOnce && appliedOneTimeEvents.Contains(evt.Id))
                    continue;

                // Check trigger conditions
                var shouldApply = evt.TriggerType switch
                {
                    SimTriggerType.Date => evt.TriggerDate.HasValue && currentDate >= evt.TriggerDate.Value,
                    SimTriggerType.Age => evt.TriggerAge.HasValue && currentAge >= evt.TriggerAge.Value,
                    SimTriggerType.Condition => !string.IsNullOrEmpty(evt.TriggerCondition) &&
                        _conditionParser.Evaluate(evt.TriggerCondition, state),
                    _ => false
                };

                if (!shouldApply)
                    continue;

                // For recurring events, check recurrence
                if (!evt.AppliesOnce && evt.RecurrenceFrequency.HasValue)
                {
                    if (!ShouldApplyRecurringItem(evt.RecurrenceFrequency.Value, currentDate, evt.TriggerDate))
                        continue;

                    if (evt.RecurrenceEndDate.HasValue && currentDate > evt.RecurrenceEndDate.Value)
                        continue;
                }

                // Apply the event
                ApplySimulationEvent(evt, state, accountBalances, accountIncomes, accountExpenses, accounts);
                eventsAppliedThisPeriod.Add(evt.Name);

                if (evt.AppliesOnce)
                    appliedOneTimeEvents.Add(evt.Id);
            }

            // 5. Create projections for this period
            decimal totalAssets = 0;
            decimal totalLiabilities = 0;
            var breakdownByType = new Dictionary<string, decimal>();
            var breakdownByCurrency = new Dictionary<string, decimal>();

            foreach (var account in accounts)
            {
                var balance = accountBalances[account.Id];
                var balanceHomeCurrency = balance; // TODO: Apply FX conversion

                if (account.IsLiability)
                {
                    totalLiabilities += Math.Abs(balanceHomeCurrency);
                    breakdownByType[account.AccountType.ToString().ToLowerInvariant()] =
                        breakdownByType.GetValueOrDefault(account.AccountType.ToString().ToLowerInvariant()) - Math.Abs(balanceHomeCurrency);
                }
                else
                {
                    totalAssets += balanceHomeCurrency;
                    breakdownByType[account.AccountType.ToString().ToLowerInvariant()] =
                        breakdownByType.GetValueOrDefault(account.AccountType.ToString().ToLowerInvariant()) + balanceHomeCurrency;
                }

                breakdownByCurrency[account.Currency] =
                    breakdownByCurrency.GetValueOrDefault(account.Currency) + balance;

                var projection = new AccountProjection
                {
                    ScenarioId = scenarioId,
                    AccountId = account.Id,
                    PeriodDate = currentDate,
                    Balance = balance,
                    BalanceHomeCurrency = balanceHomeCurrency,
                    PeriodIncome = accountIncomes.GetValueOrDefault(account.Id),
                    PeriodExpenses = accountExpenses.GetValueOrDefault(account.Id),
                    PeriodInterest = accountInterests.GetValueOrDefault(account.Id),
                    EventsApplied = eventsAppliedThisPeriod.Count > 0 
                        ? JsonSerializer.Serialize(eventsAppliedThisPeriod) 
                        : null
                };

                _context.AccountProjections.Add(projection);
                periodAccountProjections.Add(projection);
            }

            var netWorth = totalAssets - totalLiabilities;

            // Check for milestones
            var milestonesReachedThisPeriod = new List<string>();
            foreach (var target in milestoneTargets)
            {
                if (!milestonesReached.Contains(target) && netWorth >= target)
                {
                    milestonesReached.Add(target);
                    var yearsAway = monthsElapsed / 12.0m;
                    milestones.Add(new MilestoneResult
                    {
                        Description = $"Net worth reaches {target:C0}",
                        Date = currentDate,
                        Value = target,
                        YearsAway = yearsAway
                    });
                    milestonesReachedThisPeriod.Add($"netWorth >= {target}");
                }
            }

            var netWorthProjection = new NetWorthProjection
            {
                ScenarioId = scenarioId,
                PeriodDate = currentDate,
                TotalAssets = totalAssets,
                TotalLiabilities = totalLiabilities,
                NetWorth = netWorth,
                BreakdownByType = JsonSerializer.Serialize(breakdownByType),
                BreakdownByCurrency = JsonSerializer.Serialize(breakdownByCurrency),
                MilestonesReached = milestonesReachedThisPeriod.Count > 0
                    ? JsonSerializer.Serialize(milestonesReachedThisPeriod)
                    : null
            };

            _context.NetWorthProjections.Add(netWorthProjection);

            // Move to next month
            currentDate = currentDate.AddMonths(1);
            monthsCalculated++;
        }

        // Update scenario last run time
        scenario.LastRunAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();

        return new RunSimulationData
        {
            ScenarioId = scenarioId,
            Status = "completed",
            PeriodsCalculated = monthsCalculated,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            StartDate = startDate,
            EndDate = currentDate.AddMonths(-1),
            KeyMilestones = milestones
        };
    }

    public async Task<ProjectionData> GetProjectionsAsync(
        Guid userId,
        Guid scenarioId,
        DateOnly? from = null,
        DateOnly? to = null,
        string granularity = "monthly",
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        // Verify scenario belongs to user
        var scenario = await _context.SimulationScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.UserId == userId, cancellationToken);

        if (scenario == null)
            throw new InvalidOperationException("Scenario not found");

        // Get net worth projections
        var netWorthQuery = _context.NetWorthProjections
            .Where(p => p.ScenarioId == scenarioId);

        if (from.HasValue)
            netWorthQuery = netWorthQuery.Where(p => p.PeriodDate >= from.Value);
        if (to.HasValue)
            netWorthQuery = netWorthQuery.Where(p => p.PeriodDate <= to.Value);

        var netWorthProjections = await netWorthQuery
            .OrderBy(p => p.PeriodDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get account projections
        var accountQuery = _context.AccountProjections
            .Where(p => p.ScenarioId == scenarioId);

        if (from.HasValue)
            accountQuery = accountQuery.Where(p => p.PeriodDate >= from.Value);
        if (to.HasValue)
            accountQuery = accountQuery.Where(p => p.PeriodDate <= to.Value);
        if (accountId.HasValue)
            accountQuery = accountQuery.Where(p => p.AccountId == accountId.Value);

        var accountProjections = await accountQuery
            .OrderBy(p => p.PeriodDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Load account names
        var accountIds = accountProjections.Select(p => p.AccountId).Distinct().ToList();
        var accounts = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        // Group by period and apply granularity
        var monthlyProjections = new List<MonthlyProjection>();
        var groupedNetWorth = GroupByGranularity(netWorthProjections, granularity);

        foreach (var (period, projections) in groupedNetWorth)
        {
            var lastProjection = projections.Last();
            var periodAccounts = accountProjections
                .Where(p => p.PeriodDate == lastProjection.PeriodDate)
                .Select(p => new AccountProjectionItem
                {
                    AccountId = p.AccountId,
                    AccountName = accounts.GetValueOrDefault(p.AccountId, "Unknown"),
                    Balance = p.Balance,
                    BalanceHomeCurrency = p.BalanceHomeCurrency,
                    PeriodIncome = p.PeriodIncome,
                    PeriodExpenses = p.PeriodExpenses,
                    PeriodInterest = p.PeriodInterest
                })
                .ToList();

            monthlyProjections.Add(new MonthlyProjection
            {
                Period = period,
                NetWorth = lastProjection.NetWorth,
                TotalAssets = lastProjection.TotalAssets,
                TotalLiabilities = lastProjection.TotalLiabilities,
                BreakdownByType = JsonSerializer.Deserialize<Dictionary<string, decimal>>(lastProjection.BreakdownByType),
                BreakdownByCurrency = JsonSerializer.Deserialize<Dictionary<string, decimal>>(lastProjection.BreakdownByCurrency),
                Accounts = periodAccounts
            });
        }

        // Calculate milestones from projections
        var milestones = ExtractMilestones(netWorthProjections, scenario.StartDate);

        // Calculate summary
        var summary = CalculateSummary(netWorthProjections);

        return new ProjectionData
        {
            ScenarioId = scenarioId,
            MonthlyProjections = monthlyProjections,
            Milestones = milestones,
            Summary = summary
        };
    }

    public async Task<List<MilestoneResult>> CalculateMilestonesAsync(
        Guid userId,
        Guid scenarioId,
        List<decimal> targetNetWorths,
        CancellationToken cancellationToken = default)
    {
        var projections = await _context.NetWorthProjections
            .Where(p => p.ScenarioId == scenarioId)
            .OrderBy(p => p.PeriodDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!projections.Any())
            return new List<MilestoneResult>();

        var scenario = await _context.SimulationScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.UserId == userId, cancellationToken);

        if (scenario == null)
            return new List<MilestoneResult>();

        var results = new List<MilestoneResult>();
        var startDate = scenario.StartDate;

        foreach (var target in targetNetWorths.OrderBy(t => t))
        {
            var hitProjection = projections.FirstOrDefault(p => p.NetWorth >= target);
            if (hitProjection != null)
            {
                var monthsAway = ((hitProjection.PeriodDate.Year - startDate.Year) * 12) +
                    (hitProjection.PeriodDate.Month - startDate.Month);
                results.Add(new MilestoneResult
                {
                    Description = $"Net worth reaches {target:C0}",
                    Date = hitProjection.PeriodDate,
                    Value = target,
                    YearsAway = monthsAway / 12.0m
                });
            }
        }

        return results;
    }

    #region Helper Methods

    private SimulationState CreateSimulationState(
        Dictionary<Guid, decimal> accountBalances,
        Dictionary<Guid, bool> accountLiability,
        DateOnly currentDate,
        int currentAge,
        int monthsElapsed,
        List<IncomeSource> incomeSources,
        List<ExpenseDefinition> expenseDefinitions)
    {
        var totalAssets = accountBalances
            .Where(kv => !accountLiability.GetValueOrDefault(kv.Key))
            .Sum(kv => kv.Value);

        var totalLiabilities = accountBalances
            .Where(kv => accountLiability.GetValueOrDefault(kv.Key))
            .Sum(kv => Math.Abs(kv.Value));

        return new SimulationState
        {
            NetWorth = totalAssets - totalLiabilities,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            Age = currentAge,
            CurrentDate = currentDate,
            MonthsElapsed = monthsElapsed,
            AccountBalances = new Dictionary<Guid, decimal>(accountBalances),
            TotalMonthlyIncome = incomeSources
                .Where(i => i.IsActive)
                .Sum(i => ConvertToMonthly(i.BaseAmount, i.PaymentFrequency)),
            TotalMonthlyExpenses = expenseDefinitions
                .Where(e => e.IsActive && e.AmountValue.HasValue)
                .Sum(e => ConvertToMonthly(e.AmountValue!.Value, e.Frequency))
        };
    }

    private decimal ConvertToMonthly(decimal amount, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Weekly => amount * 52 / 12,
            PaymentFrequency.Biweekly => amount * 26 / 12,
            PaymentFrequency.Monthly => amount,
            PaymentFrequency.Quarterly => amount / 3,
            PaymentFrequency.Annually => amount / 12,
            _ => amount
        };
    }

    private SimulationAssumptions ParseAssumptions(string assumptionsJson)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(assumptionsJson);
            return new SimulationAssumptions
            {
                InflationRate = dict?.TryGetValue("inflationRate", out var inf) == true 
                    ? inf.GetDecimal() 
                    : 0.05m,
                DefaultGrowthRate = dict?.TryGetValue("growthRates", out var growth) == true &&
                    growth.TryGetProperty("default", out var defGrowth)
                    ? defGrowth.GetDecimal()
                    : 0.07m
            };
        }
        catch
        {
            return new SimulationAssumptions();
        }
    }

    private decimal CalculateMonthlyInterest(
        decimal balance,
        decimal annualRate,
        CompoundingFrequency compounding,
        SimulationAssumptions assumptions)
    {
        if (annualRate == 0 || balance == 0)
            return 0;

        // Monthly compounding calculation
        return compounding switch
        {
            CompoundingFrequency.None => 0,
            CompoundingFrequency.Daily => balance * ((decimal)Math.Pow((double)(1 + annualRate / 365), 30.42) - 1),
            CompoundingFrequency.Monthly => balance * (annualRate / 12),
            CompoundingFrequency.Quarterly => balance * ((decimal)Math.Pow((double)(1 + annualRate / 4), 1.0 / 3) - 1),
            CompoundingFrequency.Annually => balance * ((decimal)Math.Pow((double)(1 + annualRate), 1.0 / 12) - 1),
            CompoundingFrequency.Continuous => balance * ((decimal)Math.Exp((double)(annualRate / 12)) - 1),
            _ => 0
        };
    }

    private bool ShouldApplyRecurringItem(PaymentFrequency frequency, DateOnly currentDate, DateOnly? startDate)
    {
        // For simplicity, assume monthly simulation and check if this month should have a payment
        return frequency switch
        {
            PaymentFrequency.Weekly => true, // Apply weekly items every month (4x)
            PaymentFrequency.Biweekly => true, // Apply biweekly items every month (2x)
            PaymentFrequency.Monthly => true,
            PaymentFrequency.Quarterly => currentDate.Month % 3 == (startDate?.Month ?? 1) % 3,
            PaymentFrequency.Annually => currentDate.Month == (startDate?.Month ?? 1),
            _ => true
        };
    }

    private decimal CalculateNetIncome(
        decimal grossAmount,
        Guid taxProfileId,
        List<TaxProfile> taxProfiles,
        PaymentFrequency frequency)
    {
        var taxProfile = taxProfiles.FirstOrDefault(t => t.Id == taxProfileId);
        if (taxProfile == null)
            return grossAmount;

        // Convert to annual for tax calculation
        var annualGross = frequency switch
        {
            PaymentFrequency.Weekly => grossAmount * 52,
            PaymentFrequency.Biweekly => grossAmount * 26,
            PaymentFrequency.Monthly => grossAmount * 12,
            PaymentFrequency.Quarterly => grossAmount * 4,
            PaymentFrequency.Annually => grossAmount,
            _ => grossAmount * 12
        };

        // Calculate tax using brackets
        var tax = CalculateTaxFromBrackets(annualGross, taxProfile.Brackets);

        // Apply UIF (cap is stored as monthly value, multiply by 12 for annual cap)
        if (taxProfile.UifRate.HasValue)
        {
            var uifContribution = annualGross * taxProfile.UifRate.Value;
            if (taxProfile.UifCap.HasValue)
                uifContribution = Math.Min(uifContribution, taxProfile.UifCap.Value * 12);
            tax += uifContribution;
        }

        // Apply rebates
        var rebates = ParseRebates(taxProfile.TaxRebates);
        tax = Math.Max(0, tax - rebates);

        // Convert back to payment frequency
        var annualNet = annualGross - tax;
        return frequency switch
        {
            PaymentFrequency.Weekly => annualNet / 52,
            PaymentFrequency.Biweekly => annualNet / 26,
            PaymentFrequency.Monthly => annualNet / 12,
            PaymentFrequency.Quarterly => annualNet / 4,
            PaymentFrequency.Annually => annualNet,
            _ => annualNet / 12
        };
    }

    private decimal CalculateTaxFromBrackets(decimal annualIncome, string bracketsJson)
    {
        try
        {
            var brackets = JsonSerializer.Deserialize<List<TaxBracket>>(bracketsJson);
            if (brackets == null || !brackets.Any())
                return 0;

            foreach (var bracket in brackets.OrderByDescending(b => b.Min))
            {
                if (annualIncome >= bracket.Min)
                {
                    var taxableInBracket = annualIncome - bracket.Min;
                    return bracket.BaseTax + (taxableInBracket * bracket.Rate);
                }
            }

            return 0;
        }
        catch
        {
            return annualIncome * 0.25m; // Default 25% if parsing fails
        }
    }

    private decimal ParseRebates(string? rebatesJson)
    {
        if (string.IsNullOrEmpty(rebatesJson))
            return 0;

        try
        {
            var rebates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(rebatesJson);
            return rebates?.Values.Sum() ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private decimal CalculateExpenseAmount(ExpenseDefinition expense, SimulationState state, SimulationAssumptions assumptions)
    {
        var baseAmount = expense.AmountType switch
        {
            AmountType.Fixed => expense.AmountValue ?? 0,
            AmountType.Percentage => (expense.AmountValue ?? 0) * state.TotalMonthlyIncome,
            AmountType.Formula => EvaluateFormula(expense.AmountFormula, state),
            _ => expense.AmountValue ?? 0
        };

        // Apply inflation adjustment if enabled
        if (expense.InflationAdjusted && state.MonthsElapsed > 0)
        {
            var yearsElapsed = state.MonthsElapsed / 12.0;
            baseAmount *= (decimal)Math.Pow((double)(1 + assumptions.InflationRate), yearsElapsed);
        }

        return baseAmount;
    }

    private decimal EvaluateFormula(string? formula, SimulationState state)
    {
        // Simple formula evaluation - just return 0 for now
        // Could be extended to support expressions like "income * 0.1"
        return 0;
    }

    private void ApplySimulationEvent(
        SimulationEvent evt,
        SimulationState state,
        Dictionary<Guid, decimal> accountBalances,
        Dictionary<Guid, decimal> accountIncomes,
        Dictionary<Guid, decimal> accountExpenses,
        List<Account> accounts)
    {
        var amount = evt.AmountType switch
        {
            AmountType.Fixed => evt.AmountValue ?? 0,
            AmountType.Percentage => (evt.AmountValue ?? 0) * state.NetWorth,
            AmountType.Formula => EvaluateFormula(evt.AmountFormula, state),
            _ => evt.AmountValue ?? 0
        };

        var targetAccountId = evt.AffectedAccountId
            ?? accounts.FirstOrDefault(a => a.AccountType == AccountType.Bank && !a.IsLiability)?.Id;

        if (!targetAccountId.HasValue)
            return;

        var eventType = evt.EventType.ToLowerInvariant();

        switch (eventType)
        {
            case "one_off_transaction":
            case "income":
            case "deposit":
                accountBalances[targetAccountId.Value] += amount;
                accountIncomes[targetAccountId.Value] += amount;
                break;

            case "expense":
            case "withdrawal":
            case "purchase":
                accountBalances[targetAccountId.Value] -= amount;
                accountExpenses[targetAccountId.Value] += amount;
                break;

            case "income_change":
                // This would modify income sources - simplified for now
                accountBalances[targetAccountId.Value] += amount;
                break;

            case "expense_change":
                // This would modify expense definitions - simplified for now
                accountBalances[targetAccountId.Value] -= amount;
                break;

            case "account_adjustment":
            case "adjustment":
                accountBalances[targetAccountId.Value] = amount;
                break;

            case "transfer":
                // Need source and target account
                accountBalances[targetAccountId.Value] += amount;
                break;
        }
    }

    private Dictionary<string, List<NetWorthProjection>> GroupByGranularity(
        List<NetWorthProjection> projections,
        string granularity)
    {
        return granularity.ToLowerInvariant() switch
        {
            "yearly" => projections
                .GroupBy(p => p.PeriodDate.Year.ToString())
                .ToDictionary(g => g.Key, g => g.ToList()),
            "quarterly" => projections
                .GroupBy(p => $"{p.PeriodDate.Year}-Q{(p.PeriodDate.Month - 1) / 3 + 1}")
                .ToDictionary(g => g.Key, g => g.ToList()),
            _ => projections
                .GroupBy(p => p.PeriodDate.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => g.ToList())
        };
    }

    private List<MilestoneResult> ExtractMilestones(List<NetWorthProjection> projections, DateOnly startDate)
    {
        var milestones = new List<MilestoneResult>();
        var targets = new[] { 100000m, 500000m, 1000000m, 5000000m, 10000000m };
        var reached = new HashSet<decimal>();

        foreach (var projection in projections.OrderBy(p => p.PeriodDate))
        {
            foreach (var target in targets)
            {
                if (!reached.Contains(target) && projection.NetWorth >= target)
                {
                    reached.Add(target);
                    var monthsAway = ((projection.PeriodDate.Year - startDate.Year) * 12) +
                        (projection.PeriodDate.Month - startDate.Month);
                    milestones.Add(new MilestoneResult
                    {
                        Description = $"Net worth reaches {target:C0}",
                        Date = projection.PeriodDate,
                        Value = target,
                        YearsAway = monthsAway / 12.0m
                    });
                }
            }
        }

        return milestones;
    }

    private ProjectionSummary CalculateSummary(List<NetWorthProjection> projections)
    {
        if (!projections.Any())
            return new ProjectionSummary();

        var ordered = projections.OrderBy(p => p.PeriodDate).ToList();
        var first = ordered.First();
        var last = ordered.Last();
        var totalMonths = ordered.Count;
        var totalGrowth = last.NetWorth - first.NetWorth;

        // Calculate annualized return using CAGR formula
        decimal annualizedReturn = 0;
        if (first.NetWorth > 0 && totalMonths > 0)
        {
            var years = totalMonths / 12.0;
            annualizedReturn = (decimal)(Math.Pow((double)(last.NetWorth / first.NetWorth), 1.0 / years) - 1);
        }

        return new ProjectionSummary
        {
            StartNetWorth = first.NetWorth,
            EndNetWorth = last.NetWorth,
            TotalGrowth = totalGrowth,
            AnnualizedReturn = Math.Round(annualizedReturn, 4),
            TotalMonths = totalMonths
        };
    }

    #endregion
}

internal class SimulationAssumptions
{
    public decimal InflationRate { get; set; } = 0.05m;
    public decimal DefaultGrowthRate { get; set; } = 0.07m;
}

internal class TaxBracket
{
    public decimal Min { get; set; }
    public decimal? Max { get; set; }
    public decimal Rate { get; set; }
    public decimal BaseTax { get; set; }
}
