using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.DataPortability;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Commands.DataPortability;

public class ExportDataCommandHandler : IRequestHandler<ExportDataCommand, LifeOSExportDto>
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<ExportDataCommandHandler> _logger;

    public ExportDataCommandHandler(ILifeOSDbContext context, ILogger<ExportDataCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LifeOSExportDto> Handle(ExportDataCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data export for user {UserId}", request.UserId);

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User {request.UserId} not found");
        }

        var exportData = new ExportDataDto
        {
            Profile = new ProfileExportDto
            {
                Email = user.Email,
                Username = user.Username,
                HomeCurrency = user.HomeCurrency,
                DateOfBirth = user.DateOfBirth,
                LifeExpectancyBaseline = (int)user.LifeExpectancyBaseline,
                InflationRateAnnual = ParseDefaultAssumptionDecimal(user.DefaultAssumptions, "inflationRateAnnual"),
                DefaultGrowthRate = ParseDefaultAssumptionDecimal(user.DefaultAssumptions, "defaultGrowthRate"),
                RetirementAge = ParseDefaultAssumptionInt(user.DefaultAssumptions, "retirementAge")
            },
            Dimensions = await ExportDimensionsAsync(request.UserId, cancellationToken),
            MetricDefinitions = await ExportMetricDefinitionsAsync(request.UserId, cancellationToken),
            ScoreDefinitions = await ExportScoreDefinitionsAsync(request.UserId, cancellationToken),
            TaxProfiles = await ExportTaxProfilesAsync(request.UserId, cancellationToken),
            LongevityModels = await ExportLongevityModelsAsync(request.UserId, cancellationToken),
            Accounts = await ExportAccountsAsync(request.UserId, cancellationToken),
            Milestones = await ExportMilestonesAsync(request.UserId, cancellationToken),
            Tasks = await ExportTasksAsync(request.UserId, cancellationToken),
            Streaks = await ExportStreaksAsync(request.UserId, cancellationToken),
            MetricRecords = await ExportMetricRecordsAsync(request.UserId, cancellationToken),
            ScoreRecords = await ExportScoreRecordsAsync(request.UserId, cancellationToken),
            IncomeSources = await ExportIncomeSourcesAsync(request.UserId, cancellationToken),
            ExpenseDefinitions = await ExportExpenseDefinitionsAsync(request.UserId, cancellationToken),
            InvestmentContributions = await ExportInvestmentContributionsAsync(request.UserId, cancellationToken),
            FinancialGoals = await ExportFinancialGoalsAsync(request.UserId, cancellationToken),
            FxRates = await ExportFxRatesAsync(request.UserId, cancellationToken),
            Transactions = await ExportTransactionsAsync(request.UserId, cancellationToken),
            SimulationScenarios = await ExportSimulationScenariosAsync(request.UserId, cancellationToken),
            SimulationEvents = await ExportSimulationEventsAsync(request.UserId, cancellationToken),
            AccountProjections = await ExportAccountProjectionsAsync(request.UserId, cancellationToken),
            NetWorthProjections = await ExportNetWorthProjectionsAsync(request.UserId, cancellationToken),
            LongevitySnapshots = await ExportLongevitySnapshotsAsync(request.UserId, cancellationToken),
            Achievements = await ExportAchievementsAsync(cancellationToken),
            UserAchievements = await ExportUserAchievementsAsync(request.UserId, cancellationToken),
            UserXP = await ExportUserXPAsync(request.UserId, cancellationToken),
            NetWorthSnapshots = await ExportNetWorthSnapshotsAsync(request.UserId, cancellationToken)
        };

        var entityCounts = new EntityCountsDto
        {
            Dimensions = exportData.Dimensions.Count,
            MetricDefinitions = exportData.MetricDefinitions.Count,
            ScoreDefinitions = exportData.ScoreDefinitions.Count,
            TaxProfiles = exportData.TaxProfiles.Count,
            LongevityModels = exportData.LongevityModels.Count,
            Accounts = exportData.Accounts.Count,
            Milestones = exportData.Milestones.Count,
            Tasks = exportData.Tasks.Count,
            Streaks = exportData.Streaks.Count,
            MetricRecords = exportData.MetricRecords.Count,
            ScoreRecords = exportData.ScoreRecords.Count,
            IncomeSources = exportData.IncomeSources.Count,
            ExpenseDefinitions = exportData.ExpenseDefinitions.Count,
            InvestmentContributions = exportData.InvestmentContributions.Count,
            FinancialGoals = exportData.FinancialGoals.Count,
            FxRates = exportData.FxRates.Count,
            Transactions = exportData.Transactions.Count,
            SimulationScenarios = exportData.SimulationScenarios.Count,
            SimulationEvents = exportData.SimulationEvents.Count,
            AccountProjections = exportData.AccountProjections.Count,
            NetWorthProjections = exportData.NetWorthProjections.Count,
            LongevitySnapshots = exportData.LongevitySnapshots.Count,
            Achievements = exportData.Achievements.Count,
            UserAchievements = exportData.UserAchievements.Count,
            UserXP = exportData.UserXP != null ? 1 : 0,
            NetWorthSnapshots = exportData.NetWorthSnapshots.Count
        };

        var totalEntities = entityCounts.Dimensions + entityCounts.MetricDefinitions +
            entityCounts.ScoreDefinitions + entityCounts.TaxProfiles + entityCounts.LongevityModels +
            entityCounts.Accounts + entityCounts.Milestones + entityCounts.Tasks + entityCounts.Streaks +
            entityCounts.MetricRecords + entityCounts.ScoreRecords + entityCounts.IncomeSources +
            entityCounts.ExpenseDefinitions + entityCounts.InvestmentContributions + entityCounts.FinancialGoals +
            entityCounts.FxRates + entityCounts.Transactions + entityCounts.SimulationScenarios +
            entityCounts.SimulationEvents + entityCounts.AccountProjections + entityCounts.NetWorthProjections +
            entityCounts.LongevitySnapshots + entityCounts.Achievements + entityCounts.UserAchievements +
            entityCounts.UserXP + entityCounts.NetWorthSnapshots;

        _logger.LogInformation("Export completed: {TotalEntities} entities exported", totalEntities);

        return new LifeOSExportDto
        {
            Schema = new ExportSchemaDto
            {
                Version = "1.0.0",
                Generator = "LifeOS",
                ExportedAt = DateTime.UtcNow
            },
            Data = exportData,
            Meta = new ExportMetaDto
            {
                TotalEntities = totalEntities,
                EntityCounts = entityCounts
            }
        };
    }

    private async Task<List<DimensionExportDto>> ExportDimensionsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Dimensions
            .AsNoTracking()
            .Select(d => new DimensionExportDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                Icon = d.Icon,
                DefaultWeight = d.DefaultWeight,
                SortOrder = d.SortOrder,
                IsActive = d.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<MetricDefinitionExportDto>> ExportMetricDefinitionsAsync(Guid userId, CancellationToken ct)
    {
        // MetricDefinitions are global (no UserId), export all active
        return await _context.MetricDefinitions
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new MetricDefinitionExportDto
            {
                Id = m.Id,
                DimensionId = m.DimensionId,
                Code = m.Code,
                Name = m.Name,
                Description = m.Description,
                Unit = m.Unit,
                ValueType = m.ValueType.ToString(),
                AggregationType = m.AggregationType.ToString(),
                MinValue = m.MinValue,
                MaxValue = m.MaxValue,
                TargetValue = m.TargetValue,
                Icon = m.Icon,
                Tags = m.Tags,
                EnumValues = m.EnumValues,
                IsDerived = m.IsDerived,
                DerivedFormula = m.DerivationFormula,
                IsActive = m.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<ScoreDefinitionExportDto>> ExportScoreDefinitionsAsync(Guid userId, CancellationToken ct)
    {
        // ScoreDefinitions are global (no UserId), export all active
        return await _context.ScoreDefinitions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new ScoreDefinitionExportDto
            {
                Id = s.Id,
                DimensionId = s.DimensionId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Formula = s.Formula,
                MinScore = s.MinScore,
                MaxScore = s.MaxScore,
                IsActive = s.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<TaxProfileExportDto>> ExportTaxProfilesAsync(Guid userId, CancellationToken ct)
    {
        return await _context.TaxProfiles
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TaxProfileExportDto
            {
                Id = t.Id,
                Name = t.Name,
                TaxYear = t.TaxYear,
                CountryCode = t.CountryCode,
                Brackets = t.Brackets,
                UifRate = t.UifRate,
                UifCap = t.UifCap,
                VatRate = t.VatRate,
                IsVatRegistered = t.IsVatRegistered,
                TaxRebates = t.TaxRebates,
                IsActive = t.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<LongevityModelExportDto>> ExportLongevityModelsAsync(Guid userId, CancellationToken ct)
    {
        // LongevityModel is global (no UserId), export all active
        var models = await _context.LongevityModels
            .AsNoTracking()
            .Where(l => l.IsActive)
            .ToListAsync(ct);
            
        return models.Select(l => new LongevityModelExportDto
        {
            Id = l.Id,
            UserId = l.UserId,
            Code = l.Code,
            Name = l.Name,
            Description = l.Description,
            InputMetrics = System.Text.Json.JsonSerializer.Deserialize<string[]>(l.InputMetrics),
            ModelType = l.ModelType.ToString(),
            Parameters = l.Parameters,
            MaxRiskReduction = l.MaxRiskReduction,
            IsActive = l.IsActive
        }).ToList();
    }

    private async Task<List<AccountExportDto>> ExportAccountsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new AccountExportDto
            {
                Id = a.Id,
                Name = a.Name,
                AccountType = a.AccountType.ToString(),
                Currency = a.Currency,
                InitialBalance = a.InitialBalance,
                CurrentBalance = a.CurrentBalance,
                BalanceUpdatedAt = a.BalanceUpdatedAt,
                Institution = a.Institution,
                IsLiability = a.IsLiability,
                InterestRateAnnual = a.InterestRateAnnual,
                InterestCompounding = a.InterestCompounding.ToString(),
                MonthlyFee = a.MonthlyFee,
                Metadata = a.Metadata,
                IsActive = a.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<MilestoneExportDto>> ExportMilestonesAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Milestones
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new MilestoneExportDto
            {
                Id = m.Id,
                DimensionId = m.DimensionId,
                Title = m.Title,
                Description = m.Description,
                TargetDate = m.TargetDate,
                TargetMetricCode = m.TargetMetricCode,
                TargetMetricValue = m.TargetMetricValue,
                Status = m.Status.ToString(),
                CompletedAt = m.CompletedAt,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<LifeTaskExportDto>> ExportTasksAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new LifeTaskExportDto
            {
                Id = t.Id,
                DimensionId = t.DimensionId,
                MilestoneId = t.MilestoneId,
                Title = t.Title,
                Description = t.Description,
                TaskType = t.TaskType.ToString(),
                Frequency = t.Frequency != null ? t.Frequency.ToString() : null,
                LinkedMetricCode = t.LinkedMetricCode,
                ScheduledDate = t.ScheduledDate,
                ScheduledTime = t.ScheduledTime,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsCompleted = t.IsCompleted,
                CompletedAt = t.CompletedAt,
                IsActive = t.IsActive,
                Tags = t.Tags,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<StreakExportDto>> ExportStreaksAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Streaks
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new StreakExportDto
            {
                Id = s.Id,
                TaskId = s.TaskId,
                MetricCode = s.MetricCode,
                CurrentStreakLength = s.CurrentStreakLength,
                LongestStreakLength = s.LongestStreakLength,
                LastSuccessDate = s.LastSuccessDate,
                StreakStartDate = s.StreakStartDate,
                MissCount = s.MissCount,
                MaxAllowedMisses = s.MaxAllowedMisses,
                IsActive = s.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<MetricRecordExportDto>> ExportMetricRecordsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.MetricRecords
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new MetricRecordExportDto
            {
                Id = m.Id,
                MetricCode = m.MetricCode,
                ValueNumber = m.ValueNumber,
                ValueBoolean = m.ValueBoolean,
                ValueString = m.ValueString,
                RecordedAt = m.RecordedAt,
                Source = m.Source,
                Notes = m.Notes,
                Metadata = m.Metadata
            })
            .ToListAsync(ct);
    }

    private async Task<List<ScoreRecordExportDto>> ExportScoreRecordsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.ScoreRecords
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new ScoreRecordExportDto
            {
                Id = s.Id,
                ScoreCode = s.ScoreCode,
                ScoreValue = s.ScoreValue,
                PeriodType = s.PeriodType.ToString(),
                PeriodStart = s.PeriodStart,
                PeriodEnd = s.PeriodEnd,
                Breakdown = s.Breakdown,
                CalculatedAt = s.CalculatedAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<IncomeSourceExportDto>> ExportIncomeSourcesAsync(Guid userId, CancellationToken ct)
    {
        return await _context.IncomeSources
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new IncomeSourceExportDto
            {
                Id = i.Id,
                TaxProfileId = i.TaxProfileId,
                Name = i.Name,
                Currency = i.Currency,
                BaseAmount = i.BaseAmount,
                IsPreTax = i.IsPreTax,
                PaymentFrequency = i.PaymentFrequency.ToString(),
                NextPaymentDate = i.NextPaymentDate,
                AnnualIncreaseRate = i.AnnualIncreaseRate,
                EmployerName = i.EmployerName,
                Notes = i.Notes,
                IsActive = i.IsActive,
                TargetAccountId = i.TargetAccountId
            })
            .ToListAsync(ct);
    }

    private async Task<List<ExpenseDefinitionExportDto>> ExportExpenseDefinitionsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.ExpenseDefinitions
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => new ExpenseDefinitionExportDto
            {
                Id = e.Id,
                LinkedAccountId = e.LinkedAccountId,
                Name = e.Name,
                Currency = e.Currency,
                AmountType = e.AmountType.ToString(),
                AmountValue = e.AmountValue,
                AmountFormula = e.AmountFormula,
                Frequency = e.Frequency.ToString(),
                StartDate = e.StartDate,
                Category = e.Category,
                IsTaxDeductible = e.IsTaxDeductible,
                InflationAdjusted = e.InflationAdjusted,
                IsActive = e.IsActive,
                EndConditionType = e.EndConditionType.ToString(),
                EndConditionAccountId = e.EndConditionAccountId,
                EndDate = e.EndDate,
                EndAmountThreshold = e.EndAmountThreshold
            })
            .ToListAsync(ct);
    }

    private async Task<List<InvestmentContributionExportDto>> ExportInvestmentContributionsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.InvestmentContributions
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new InvestmentContributionExportDto
            {
                Id = i.Id,
                TargetAccountId = i.TargetAccountId,
                SourceAccountId = i.SourceAccountId,
                Name = i.Name,
                Currency = i.Currency,
                Amount = i.Amount,
                Frequency = i.Frequency.ToString(),
                Category = i.Category,
                AnnualIncreaseRate = i.AnnualIncreaseRate,
                Notes = i.Notes,
                IsActive = i.IsActive,
                StartDate = i.StartDate,
                EndConditionType = i.EndConditionType.ToString(),
                EndConditionAccountId = i.EndConditionAccountId,
                EndDate = i.EndDate,
                EndAmountThreshold = i.EndAmountThreshold
            })
            .ToListAsync(ct);
    }

    private async Task<List<FinancialGoalExportDto>> ExportFinancialGoalsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.FinancialGoals
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => new FinancialGoalExportDto
            {
                Id = f.Id,
                Name = f.Name,
                TargetAmount = f.TargetAmount,
                CurrentAmount = f.CurrentAmount,
                Currency = f.Currency,
                TargetDate = f.TargetDate,
                Priority = f.Priority,
                Category = f.Category,
                IconName = f.IconName,
                Notes = f.Notes,
                IsActive = f.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<FxRateExportDto>> ExportFxRatesAsync(Guid userId, CancellationToken ct)
    {
        return await _context.FxRates
            .AsNoTracking()
            .Select(f => new FxRateExportDto
            {
                Id = f.Id,
                BaseCurrency = f.BaseCurrency,
                QuoteCurrency = f.QuoteCurrency,
                Rate = f.Rate,
                RateTimestamp = f.RateTimestamp,
                Source = f.Source
            })
            .ToListAsync(ct);
    }

    private async Task<List<TransactionExportDto>> ExportTransactionsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TransactionExportDto
            {
                Id = t.Id,
                SourceAccountId = t.SourceAccountId,
                TargetAccountId = t.TargetAccountId,
                Currency = t.Currency,
                Amount = t.Amount,
                AmountHomeCurrency = t.AmountHomeCurrency,
                FxRateUsed = t.FxRateUsed,
                Category = t.Category.ToString(),
                Subcategory = t.Subcategory,
                Tags = t.Tags,
                Description = t.Description,
                Notes = t.Notes,
                TransactionDate = t.TransactionDate,
                RecordedAt = t.RecordedAt,
                Source = t.Source,
                IsReconciled = t.IsReconciled
            })
            .ToListAsync(ct);
    }

    private async Task<List<SimulationScenarioExportDto>> ExportSimulationScenariosAsync(Guid userId, CancellationToken ct)
    {
        return await _context.SimulationScenarios
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new SimulationScenarioExportDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                EndCondition = s.EndCondition,
                BaseAssumptions = s.BaseAssumptions,
                IsBaseline = s.IsBaseline,
                LastRunAt = s.LastRunAt
            })
            .ToListAsync(ct);
    }

    private async Task<List<SimulationEventExportDto>> ExportSimulationEventsAsync(Guid userId, CancellationToken ct)
    {
        var scenarioIds = await _context.SimulationScenarios
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        return await _context.SimulationEvents
            .AsNoTracking()
            .Where(e => scenarioIds.Contains(e.ScenarioId))
            .Select(e => new SimulationEventExportDto
            {
                Id = e.Id,
                ScenarioId = e.ScenarioId,
                Name = e.Name,
                Description = e.Description,
                TriggerType = e.TriggerType.ToString(),
                TriggerDate = e.TriggerDate,
                TriggerAge = e.TriggerAge,
                TriggerCondition = e.TriggerCondition,
                EventType = e.EventType,
                Currency = e.Currency,
                AmountType = e.AmountType.ToString(),
                AmountValue = e.AmountValue,
                AffectedAccountId = e.AffectedAccountId,
                AppliesOnce = e.AppliesOnce,
                SortOrder = e.SortOrder,
                IsActive = e.IsActive
            })
            .ToListAsync(ct);
    }

    private async Task<List<AccountProjectionExportDto>> ExportAccountProjectionsAsync(Guid userId, CancellationToken ct)
    {
        var scenarioIds = await _context.SimulationScenarios
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        return await _context.AccountProjections
            .AsNoTracking()
            .Where(a => scenarioIds.Contains(a.ScenarioId))
            .Select(a => new AccountProjectionExportDto
            {
                Id = a.Id,
                ScenarioId = a.ScenarioId,
                AccountId = a.AccountId,
                PeriodDate = a.PeriodDate,
                Balance = a.Balance,
                BalanceHomeCurrency = a.BalanceHomeCurrency,
                PeriodIncome = a.PeriodIncome,
                PeriodExpenses = a.PeriodExpenses,
                PeriodInterest = a.PeriodInterest
            })
            .ToListAsync(ct);
    }

    private async Task<List<NetWorthProjectionExportDto>> ExportNetWorthProjectionsAsync(Guid userId, CancellationToken ct)
    {
        var scenarioIds = await _context.SimulationScenarios
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        return await _context.NetWorthProjections
            .AsNoTracking()
            .Where(n => scenarioIds.Contains(n.ScenarioId))
            .Select(n => new NetWorthProjectionExportDto
            {
                Id = n.Id,
                ScenarioId = n.ScenarioId,
                PeriodDate = n.PeriodDate,
                TotalAssets = n.TotalAssets,
                TotalLiabilities = n.TotalLiabilities,
                NetWorth = n.NetWorth,
                BreakdownByType = n.BreakdownByType,
                BreakdownByCurrency = n.BreakdownByCurrency
            })
            .ToListAsync(ct);
    }

    private async Task<List<LongevitySnapshotExportDto>> ExportLongevitySnapshotsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.LongevitySnapshots
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => new LongevitySnapshotExportDto
            {
                Id = l.Id,
                CalculatedAt = l.Timestamp,
                BaselineLifeExpectancy = l.BaselineLifeExpectancyYears,
                EstimatedYearsAdded = l.TotalYearsAdded,
                AdjustedLifeExpectancy = l.AdjustedLifeExpectancyYears,
                Breakdown = l.Breakdown,
                InputMetricsSnapshot = "{}",  // Legacy field, no longer used
                ConfidenceLevel = l.Confidence
            })
            .ToListAsync(ct);
    }

    private async Task<List<AchievementExportDto>> ExportAchievementsAsync(CancellationToken ct)
    {
        return await _context.Achievements
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new AchievementExportDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                Icon = a.Icon,
                XpValue = a.XpValue,
                Category = a.Category,
                Tier = a.Tier,
                UnlockCondition = a.UnlockCondition,
                IsActive = a.IsActive,
                SortOrder = a.SortOrder
            })
            .ToListAsync(ct);
    }

    private async Task<List<UserAchievementExportDto>> ExportUserAchievementsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.UserAchievements
            .AsNoTracking()
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .Select(ua => new UserAchievementExportDto
            {
                Id = ua.Id,
                AchievementCode = ua.Achievement.Code,
                UnlockedAt = ua.UnlockedAt,
                Progress = ua.Progress,
                UnlockContext = ua.UnlockContext
            })
            .ToListAsync(ct);
    }

    private async Task<UserXPExportDto?> ExportUserXPAsync(Guid userId, CancellationToken ct)
    {
        var userXp = await _context.UserXPs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (userXp == null)
            return null;

        return new UserXPExportDto
        {
            Id = userXp.Id,
            TotalXp = userXp.TotalXp,
            Level = userXp.Level,
            WeeklyXp = userXp.WeeklyXp,
            WeekStartDate = userXp.WeekStartDate
        };
    }

    private async Task<List<NetWorthSnapshotExportDto>> ExportNetWorthSnapshotsAsync(Guid userId, CancellationToken ct)
    {
        return await _context.NetWorthSnapshots
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .Select(n => new NetWorthSnapshotExportDto
            {
                Id = n.Id,
                SnapshotDate = n.SnapshotDate,
                TotalAssets = n.TotalAssets,
                TotalLiabilities = n.TotalLiabilities,
                NetWorth = n.NetWorth,
                HomeCurrency = n.HomeCurrency,
                BreakdownByType = n.BreakdownByType,
                BreakdownByCurrency = n.BreakdownByCurrency,
                AccountCount = n.AccountCount
            })
            .ToListAsync(ct);
    }

    private static decimal? ParseDefaultAssumptionDecimal(string json, string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var element))
            {
                return element.GetDecimal();
            }
        }
        catch
        {
            // Parsing failed, return null
        }
        return null;
    }

    private static int? ParseDefaultAssumptionInt(string json, string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var element))
            {
                return element.GetInt32();
            }
        }
        catch
        {
            // Parsing failed, return null
        }
        return null;
    }
}
