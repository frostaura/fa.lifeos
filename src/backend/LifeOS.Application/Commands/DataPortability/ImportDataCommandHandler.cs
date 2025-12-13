using System.Diagnostics;
using System.Text.Json;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.DTOs.DataPortability;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeOS.Application.Commands.DataPortability;

public class ImportDataCommandHandler : IRequestHandler<ImportDataCommand, ImportResultDto>
{
    private readonly ILifeOSDbContext _context;
    private readonly ILogger<ImportDataCommandHandler> _logger;

    private readonly Dictionary<Guid, Guid> _dimensionIdMap = new();
    private readonly Dictionary<Guid, Guid> _taxProfileIdMap = new();
    private readonly Dictionary<Guid, Guid> _accountIdMap = new();
    private readonly Dictionary<Guid, Guid> _scenarioIdMap = new();
    private readonly Dictionary<Guid, Guid> _milestoneIdMap = new();

    public ImportDataCommandHandler(ILifeOSDbContext context, ILogger<ImportDataCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResultDto> Handle(ImportDataCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _dimensionIdMap.Clear();
        _taxProfileIdMap.Clear();
        _accountIdMap.Clear();
        _scenarioIdMap.Clear();
        _milestoneIdMap.Clear();

        _logger.LogInformation("Starting data import for user {UserId} in {Mode} mode (DryRun: {DryRun})", 
            request.UserId, request.Mode, request.DryRun);

        var results = new Dictionary<string, ImportEntityResultDto>();
        var totalImported = 0;
        var totalSkipped = 0;
        var totalErrors = 0;

        var schemaVersion = request.Data.Schema?.Version ?? "1.0.0";
        if (!IsSchemaCompatible(schemaVersion))
        {
            throw new InvalidOperationException($"Incompatible schema version: {schemaVersion}. Current version: 1.0.0");
        }

        var isReplaceMode = request.Mode.ToLowerInvariant() == "replace";
        var data = request.Data.Data;

        try
        {
            // Import profile data first
            if (!request.DryRun)
            {
                await ImportProfileAsync(request.UserId, data.Profile, cancellationToken);
            }

            if (isReplaceMode && !request.DryRun)
            {
                await DeleteAllUserDataAsync(request.UserId, cancellationToken);
                // Clear change tracker to ensure deleted entities are fully detached
                // This prevents PK constraint violations when adding entities with same IDs
                _context.ChangeTracker.Clear();
                _logger.LogInformation("Change tracker cleared after deletion");
            }

            // Import in FK-safe order
            _logger.LogInformation("Importing {Count} dimensions", data.Dimensions?.Count ?? 0);
            results["dimensions"] = await ImportDimensionsAsync(data.Dimensions, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["dimensions"], ref totalImported, ref totalSkipped, ref totalErrors);
            
            // Save dimensions first (they have no dependencies)
            if (!request.DryRun)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully saved dimensions");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to save dimensions: {Message}", ex.InnerException?.Message ?? ex.Message);
                    throw new InvalidOperationException($"Import failed while saving dimensions: {ex.InnerException?.Message ?? ex.Message}", ex);
                }
            }

            results["metricDefinitions"] = await ImportMetricDefinitionsAsync(data.MetricDefinitions, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["metricDefinitions"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["scoreDefinitions"] = await ImportScoreDefinitionsAsync(data.ScoreDefinitions, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["scoreDefinitions"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["taxProfiles"] = await ImportTaxProfilesAsync(request.UserId, data.TaxProfiles, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["taxProfiles"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["longevityModels"] = await ImportLongevityModelsAsync(request.UserId, data.LongevityModels, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["longevityModels"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["accounts"] = await ImportAccountsAsync(request.UserId, data.Accounts, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["accounts"], ref totalImported, ref totalSkipped, ref totalErrors);
            
            // Save remaining base entities (accounts, tax profiles, etc.)
            if (!request.DryRun)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully saved base entities");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to save base entities: {Message}", ex.InnerException?.Message ?? ex.Message);
                    throw new InvalidOperationException($"Import failed while saving base entities: {ex.InnerException?.Message ?? ex.Message}", ex);
                }
            }

            results["milestones"] = await ImportMilestonesAsync(request.UserId, data.Milestones, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["milestones"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["tasks"] = await ImportTasksAsync(request.UserId, data.Tasks, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["tasks"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["streaks"] = await ImportStreaksAsync(request.UserId, data.Streaks, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["streaks"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["metricRecords"] = await ImportMetricRecordsAsync(request.UserId, data.MetricRecords, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["metricRecords"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["scoreRecords"] = await ImportScoreRecordsAsync(request.UserId, data.ScoreRecords, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["scoreRecords"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["incomeSources"] = await ImportIncomeSourcesAsync(request.UserId, data.IncomeSources, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["incomeSources"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["expenseDefinitions"] = await ImportExpenseDefinitionsAsync(request.UserId, data.ExpenseDefinitions, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["expenseDefinitions"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["investmentContributions"] = await ImportInvestmentContributionsAsync(request.UserId, data.InvestmentContributions, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["investmentContributions"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["financialGoals"] = await ImportFinancialGoalsAsync(request.UserId, data.FinancialGoals, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["financialGoals"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["fxRates"] = await ImportFxRatesAsync(data.FxRates, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["fxRates"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["transactions"] = await ImportTransactionsAsync(request.UserId, data.Transactions, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["transactions"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["simulationScenarios"] = await ImportSimulationScenariosAsync(request.UserId, data.SimulationScenarios, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["simulationScenarios"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["simulationEvents"] = await ImportSimulationEventsAsync(request.UserId, data.SimulationEvents, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["simulationEvents"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["accountProjections"] = await ImportAccountProjectionsAsync(request.UserId, data.AccountProjections, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["accountProjections"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["netWorthProjections"] = await ImportNetWorthProjectionsAsync(request.UserId, data.NetWorthProjections, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["netWorthProjections"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["longevitySnapshots"] = await ImportLongevitySnapshotsAsync(request.UserId, data.LongevitySnapshots, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["longevitySnapshots"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["achievements"] = await ImportAchievementsAsync(data.Achievements, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["achievements"], ref totalImported, ref totalSkipped, ref totalErrors);
            
            // Save achievements before user achievements
            if (!request.DryRun)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully saved achievements");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to save achievements: {Message}", ex.InnerException?.Message ?? ex.Message);
                    throw new InvalidOperationException($"Import failed while saving achievements: {ex.InnerException?.Message ?? ex.Message}", ex);
                }
            }

            results["userAchievements"] = await ImportUserAchievementsAsync(request.UserId, data.UserAchievements, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["userAchievements"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["userXP"] = await ImportUserXPAsync(request.UserId, data.UserXP, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["userXP"], ref totalImported, ref totalSkipped, ref totalErrors);

            results["netWorthSnapshots"] = await ImportNetWorthSnapshotsAsync(request.UserId, data.NetWorthSnapshots, isReplaceMode, request.DryRun, cancellationToken);
            UpdateCounts(results["netWorthSnapshots"], ref totalImported, ref totalSkipped, ref totalErrors);

            if (!request.DryRun)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully saved all imported entities");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to save imported entities: {Message}", ex.InnerException?.Message ?? ex.Message);
                    throw new InvalidOperationException($"Import failed while saving entities: {ex.InnerException?.Message ?? ex.Message}", ex);
                }
            }

            stopwatch.Stop();

            return new ImportResultDto
            {
                Status = request.DryRun ? "dry_run_complete" : "success",
                Mode = request.Mode,
                ImportedAt = DateTime.UtcNow,
                SchemaVersion = schemaVersion,
                Results = results,
                TotalImported = totalImported,
                TotalSkipped = totalSkipped,
                TotalErrors = totalErrors,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IsDryRun = request.DryRun
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed for user {UserId}", request.UserId);
            throw;
        }
    }

    private static void UpdateCounts(ImportEntityResultDto result, ref int imported, ref int skipped, ref int errors)
    {
        imported += result.Imported;
        skipped += result.Skipped;
        errors += result.Errors;
    }

    private static bool IsSchemaCompatible(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 1 && parts[0] == "1";
    }

    private Guid MapDimensionId(Guid id) => _dimensionIdMap.TryGetValue(id, out var mapped) ? mapped : id;
    private Guid? MapDimensionId(Guid? id) => id.HasValue ? MapDimensionId(id.Value) : null;

    private Guid MapTaxProfileId(Guid id) => _taxProfileIdMap.TryGetValue(id, out var mapped) ? mapped : id;
    private Guid? MapTaxProfileId(Guid? id) => id.HasValue ? MapTaxProfileId(id.Value) : null;
    private Guid MapAccountId(Guid id) => _accountIdMap.TryGetValue(id, out var mapped) ? mapped : id;
    private Guid? MapAccountId(Guid? id) => id.HasValue ? MapAccountId(id.Value) : null;
    private Guid MapScenarioId(Guid id) => _scenarioIdMap.TryGetValue(id, out var mapped) ? mapped : id;
    private Guid MapMilestoneId(Guid id) => _milestoneIdMap.TryGetValue(id, out var mapped) ? mapped : id;
    private Guid? MapMilestoneId(Guid? id) => id.HasValue ? MapMilestoneId(id.Value) : null;

    private async Task DeleteAllUserDataAsync(Guid userId, CancellationToken ct)
    {
        _logger.LogInformation("Deleting all data for user {UserId}", userId);
        
        // Delete in reverse FK dependency order to avoid constraint violations
        
        // 1. Delete user achievements, XP, and snapshots (no dependencies)
        var userAchievements = await _context.UserAchievements.Where(ua => ua.UserId == userId).ToListAsync(ct);
        _context.UserAchievements.RemoveRange(userAchievements);
        
        var userXPs = await _context.UserXPs.Where(x => x.UserId == userId).ToListAsync(ct);
        _context.UserXPs.RemoveRange(userXPs);
        
        var netWorthSnapshots = await _context.NetWorthSnapshots.Where(n => n.UserId == userId).ToListAsync(ct);
        _context.NetWorthSnapshots.RemoveRange(netWorthSnapshots);
        
        var longevitySnapshots = await _context.LongevitySnapshots.Where(l => l.UserId == userId).ToListAsync(ct);
        _context.LongevitySnapshots.RemoveRange(longevitySnapshots);
        
        // 2. Delete simulation-related data (depends on scenarios and accounts)
        var scenarioIds = await _context.SimulationScenarios.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync(ct);
        if (scenarioIds.Any())
        {
            var netWorthProjections = await _context.NetWorthProjections.Where(n => scenarioIds.Contains(n.ScenarioId)).ToListAsync(ct);
            _context.NetWorthProjections.RemoveRange(netWorthProjections);
            
            var accountProjections = await _context.AccountProjections.Where(a => scenarioIds.Contains(a.ScenarioId)).ToListAsync(ct);
            _context.AccountProjections.RemoveRange(accountProjections);
            
            var simEvents = await _context.SimulationEvents.Where(e => scenarioIds.Contains(e.ScenarioId)).ToListAsync(ct);
            _context.SimulationEvents.RemoveRange(simEvents);
            
            var scenarios = await _context.SimulationScenarios.Where(s => s.UserId == userId).ToListAsync(ct);
            _context.SimulationScenarios.RemoveRange(scenarios);
        }
        
        // 3. Delete financial data that references accounts (must be deleted BEFORE accounts)
        var transactions = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync(ct);
        _context.Transactions.RemoveRange(transactions);
        
        var investmentContributions = await _context.InvestmentContributions.Where(i => i.UserId == userId).ToListAsync(ct);
        _context.InvestmentContributions.RemoveRange(investmentContributions);
        
        var expenseDefinitions = await _context.ExpenseDefinitions.Where(e => e.UserId == userId).ToListAsync(ct);
        _context.ExpenseDefinitions.RemoveRange(expenseDefinitions);
        
        var incomeSources = await _context.IncomeSources.Where(i => i.UserId == userId).ToListAsync(ct);
        _context.IncomeSources.RemoveRange(incomeSources);
        
        var financialGoals = await _context.FinancialGoals.Where(f => f.UserId == userId).ToListAsync(ct);
        _context.FinancialGoals.RemoveRange(financialGoals);
        
        // 4. Delete tasks and milestones (may reference accounts)
        var streaks = await _context.Streaks.Where(s => s.UserId == userId).ToListAsync(ct);
        _context.Streaks.RemoveRange(streaks);
        
        var tasks = await _context.Tasks.Where(t => t.UserId == userId).ToListAsync(ct);
        _context.Tasks.RemoveRange(tasks);
        
        var milestones = await _context.Milestones.Where(m => m.UserId == userId).ToListAsync(ct);
        _context.Milestones.RemoveRange(milestones);
        
        // 5. Delete metric and score records
        var scoreRecords = await _context.ScoreRecords.Where(s => s.UserId == userId).ToListAsync(ct);
        _context.ScoreRecords.RemoveRange(scoreRecords);
        
        var metricRecords = await _context.MetricRecords.Where(m => m.UserId == userId).ToListAsync(ct);
        _context.MetricRecords.RemoveRange(metricRecords);
        
        // 6. Finally delete accounts and tax profiles (base entities)
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync(ct);
        _context.Accounts.RemoveRange(accounts);
        
        var taxProfiles = await _context.TaxProfiles.Where(t => t.UserId == userId).ToListAsync(ct);
        _context.TaxProfiles.RemoveRange(taxProfiles);
        
        await _context.SaveChangesAsync(ct);
    }

    private async Task<ImportEntityResultDto> ImportDimensionsAsync(List<DimensionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("ImportDimensionsAsync received null or empty items list");
            return new ImportEntityResultDto { Imported = 0, Skipped = 0, Errors = 0 };
        }

        _logger.LogInformation("ImportDimensionsAsync processing {Count} items", items.Count);
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.Dimensions.FirstOrDefaultAsync(d => d.Code == item.Code, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                // In replace mode with global entities, we need to preserve the import ID
                // Delete existing and recreate with correct ID
                if (!dryRun)
                {
                    _context.Dimensions.Remove(existing);
                    var entity = new Dimension
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Icon = item.Icon,
                        DefaultWeight = item.DefaultWeight,
                        SortOrder = item.SortOrder,
                        IsActive = item.IsActive
                    };
                    _context.Dimensions.Add(entity);
                }
            }
            else
            {
                // Add new entity
                if (!dryRun)
                {
                    var entity = new Dimension
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Icon = item.Icon,
                        DefaultWeight = item.DefaultWeight,
                        SortOrder = item.SortOrder,
                        IsActive = item.IsActive
                    };
                    _context.Dimensions.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportMetricDefinitionsAsync(List<MetricDefinitionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.MetricDefinitions.FirstOrDefaultAsync(m => m.Code == item.Code, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                // In replace mode with global entities, we need to preserve the import ID
                // Delete existing and recreate with correct ID
                if (!dryRun)
                {
                    _context.MetricDefinitions.Remove(existing);
                    var entity = new MetricDefinition
                    {
                        Id = item.Id,
                        DimensionId = item.DimensionId,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Unit = item.Unit,
                        ValueType = Enum.TryParse<MetricValueType>(item.ValueType, true, out var vt) ? vt : MetricValueType.Number,
                        AggregationType = Enum.TryParse<AggregationType>(item.AggregationType, true, out var at) ? at : AggregationType.Last,
                        MinValue = item.MinValue,
                        MaxValue = item.MaxValue,
                        TargetValue = item.TargetValue,
                        Icon = item.Icon,
                        Tags = item.Tags,
                        EnumValues = item.EnumValues,
                        IsDerived = item.IsDerived,
                        DerivationFormula = item.DerivedFormula,
                        IsActive = item.IsActive
                    };
                    _context.MetricDefinitions.Add(entity);
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new MetricDefinition
                    {
                        Id = item.Id,
                        DimensionId = item.DimensionId,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Unit = item.Unit,
                        ValueType = Enum.TryParse<MetricValueType>(item.ValueType, true, out var vt) ? vt : MetricValueType.Number,
                        AggregationType = Enum.TryParse<AggregationType>(item.AggregationType, true, out var at) ? at : AggregationType.Last,
                        MinValue = item.MinValue,
                        MaxValue = item.MaxValue,
                        TargetValue = item.TargetValue,
                        Icon = item.Icon,
                        Tags = item.Tags,
                        EnumValues = item.EnumValues,
                        IsDerived = item.IsDerived,
                        DerivationFormula = item.DerivedFormula,
                        IsActive = item.IsActive
                    };
                    _context.MetricDefinitions.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportScoreDefinitionsAsync(List<ScoreDefinitionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.ScoreDefinitions.FirstOrDefaultAsync(s => s.Code == item.Code, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }

                if (!dryRun)
                {
                    existing.DimensionId = MapDimensionId(item.DimensionId);
                    existing.Name = item.Name;
                    existing.Description = item.Description;
                    existing.Formula = item.Formula ?? string.Empty;
                    existing.MinScore = item.MinScore;
                    existing.MaxScore = item.MaxScore;
                    existing.IsActive = item.IsActive;
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new ScoreDefinition
                    {
                        Id = item.Id,
                        DimensionId = MapDimensionId(item.DimensionId),
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Formula = item.Formula ?? string.Empty,
                        MinScore = item.MinScore,
                        MaxScore = item.MaxScore,
                        IsActive = item.IsActive
                    };
                    _context.ScoreDefinitions.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportTaxProfilesAsync(Guid userId, List<TaxProfileExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.TaxProfiles.FirstOrDefaultAsync(t => t.UserId == userId && t.Name == item.Name, ct);
            
            if (existing != null)
            {
                _taxProfileIdMap[item.Id] = existing.Id;
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.TaxYear = item.TaxYear;
                    existing.CountryCode = item.CountryCode;
                    existing.Brackets = item.Brackets ?? "[]";
                    existing.UifRate = item.UifRate;
                    existing.UifCap = item.UifCap;
                    existing.VatRate = item.VatRate;
                    existing.IsVatRegistered = item.IsVatRegistered;
                    existing.TaxRebates = item.TaxRebates;
                    existing.IsActive = item.IsActive;
                }
            }
            else
            {
                var newId = await _context.TaxProfiles.AnyAsync(t => t.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                _taxProfileIdMap[item.Id] = newId;

                if (!dryRun)
                {
                    var entity = new TaxProfile
                    {
                        Id = newId,
                        UserId = userId,
                        Name = item.Name,
                        TaxYear = item.TaxYear,
                        CountryCode = item.CountryCode,
                        Brackets = item.Brackets ?? "[]",
                        UifRate = item.UifRate,
                        UifCap = item.UifCap,
                        VatRate = item.VatRate,
                        IsVatRegistered = item.IsVatRegistered,
                        TaxRebates = item.TaxRebates,
                        IsActive = item.IsActive
                    };
                    _context.TaxProfiles.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportLongevityModelsAsync(Guid userId, List<LongevityModelExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.LongevityModels.FirstOrDefaultAsync(l => l.Code == item.Code, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }

                if (!dryRun)
                {
                    existing.UserId = userId;
                    existing.Name = item.Name;
                    existing.Description = item.Description;
                    existing.InputMetrics = System.Text.Json.JsonSerializer.Serialize(item.InputMetrics ?? Array.Empty<string>());
                    existing.ModelType = Enum.Parse<Domain.Enums.LongevityModelType>(item.ModelType);
                    existing.Parameters = item.Parameters ?? "{}";
                    existing.MaxRiskReduction = item.MaxRiskReduction;
                    existing.IsActive = item.IsActive;
                }
            }
            else
            {
                var newId = await _context.LongevityModels.AnyAsync(l => l.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;

                if (!dryRun)
                {
                    var entity = new LongevityModel
                    {
                        Id = newId,
                        UserId = userId,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        InputMetrics = System.Text.Json.JsonSerializer.Serialize(item.InputMetrics ?? Array.Empty<string>()),
                        ModelType = Enum.Parse<Domain.Enums.LongevityModelType>(item.ModelType),
                        Parameters = item.Parameters ?? "{}",
                        MaxRiskReduction = item.MaxRiskReduction,
                        IsActive = item.IsActive
                    };
                    _context.LongevityModels.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportAccountsAsync(Guid userId, List<AccountExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId && a.Name == item.Name, ct);
            
            if (existing != null)
            {
                _accountIdMap[item.Id] = existing.Id;
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.AccountType = Enum.TryParse<AccountType>(item.AccountType, true, out var atype) ? atype : AccountType.Bank;
                    existing.Currency = item.Currency;
                    existing.InitialBalance = item.InitialBalance;
                    existing.CurrentBalance = item.CurrentBalance;
                    existing.BalanceUpdatedAt = item.BalanceUpdatedAt;
                    existing.Institution = item.Institution;
                    existing.IsLiability = item.IsLiability;
                    existing.InterestRateAnnual = item.InterestRateAnnual;
                    existing.InterestCompounding = Enum.TryParse<CompoundingFrequency>(item.InterestCompounding, true, out var cf) ? cf : CompoundingFrequency.Monthly;
                    existing.MonthlyFee = item.MonthlyFee ?? 0;
                    existing.Metadata = item.Metadata;
                    existing.IsActive = item.IsActive;
                }
            }
            else
            {
                var newId = await _context.Accounts.AnyAsync(a => a.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                _accountIdMap[item.Id] = newId;

                if (!dryRun)
                {
                    var entity = new Account
                    {
                        Id = newId,
                        UserId = userId,
                        Name = item.Name,
                        AccountType = Enum.TryParse<AccountType>(item.AccountType, true, out var atype) ? atype : AccountType.Bank,
                        Currency = item.Currency,
                        InitialBalance = item.InitialBalance,
                        CurrentBalance = item.CurrentBalance,
                        BalanceUpdatedAt = item.BalanceUpdatedAt,
                        Institution = item.Institution,
                        IsLiability = item.IsLiability,
                        InterestRateAnnual = item.InterestRateAnnual,
                        InterestCompounding = Enum.TryParse<CompoundingFrequency>(item.InterestCompounding, true, out var cf) ? cf : CompoundingFrequency.Monthly,
                        MonthlyFee = item.MonthlyFee ?? 0,
                        Metadata = item.Metadata,
                        IsActive = item.IsActive
                    };
                    _context.Accounts.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportMilestonesAsync(Guid userId, List<MilestoneExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.Milestones.FirstOrDefaultAsync(m => m.UserId == userId && m.Title == item.Title, ct);
            
            if (existing != null)
            {
                _milestoneIdMap[item.Id] = existing.Id;
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.DimensionId = item.DimensionId;
                    existing.Description = item.Description;
                    existing.TargetDate = item.TargetDate;
                    existing.TargetMetricCode = item.TargetMetricCode;
                    existing.TargetMetricValue = item.TargetMetricValue;
                    existing.Status = Enum.TryParse<MilestoneStatus>(item.Status, true, out var ms) ? ms : MilestoneStatus.Active;
                    existing.CompletedAt = item.CompletedAt;
                    existing.CreatedAt = item.CreatedAt;
                }
            }
            else
            {
                var newId = await _context.Milestones.AnyAsync(m => m.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                _milestoneIdMap[item.Id] = newId;

                if (!dryRun)
                {
                    var entity = new Milestone
                    {
                        Id = newId,
                        UserId = userId,
                        DimensionId = MapDimensionId(item.DimensionId),
                        Title = item.Title,
                        Description = item.Description,
                        TargetDate = item.TargetDate,
                        TargetMetricCode = item.TargetMetricCode,
                        TargetMetricValue = item.TargetMetricValue,
                        Status = Enum.TryParse<MilestoneStatus>(item.Status, true, out var ms) ? ms : MilestoneStatus.Active,
                        CompletedAt = item.CompletedAt,
                        CreatedAt = item.CreatedAt
                    };
                    _context.Milestones.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportTasksAsync(Guid userId, List<LifeTaskExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.Tasks.FirstOrDefaultAsync(t => t.UserId == userId && t.Title == item.Title, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.DimensionId = MapDimensionId(item.DimensionId);
                    existing.MilestoneId = MapMilestoneId(item.MilestoneId);
                    existing.Description = item.Description;
                    existing.TaskType = Enum.TryParse<TaskType>(item.TaskType, true, out var tt) ? tt : TaskType.Habit;
                    existing.Frequency = Enum.TryParse<Frequency>(item.Frequency, true, out var f) ? f : Frequency.AdHoc;
                    existing.LinkedMetricCode = item.LinkedMetricCode;
                    existing.ScheduledDate = item.ScheduledDate;
                    existing.ScheduledTime = item.ScheduledTime;
                    existing.StartDate = item.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                    existing.EndDate = item.EndDate;
                    existing.IsCompleted = item.IsCompleted;
                    existing.CompletedAt = item.CompletedAt;
                    existing.IsActive = item.IsActive;
                    existing.Tags = item.Tags;
                    existing.CreatedAt = item.CreatedAt;
                }
            }
            else
            {
                var newId = await _context.Tasks.AnyAsync(t => t.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;

                if (!dryRun)
                {
                    var entity = new LifeTask
                    {
                        Id = newId,
                        UserId = userId,
                        DimensionId = MapDimensionId(item.DimensionId),
                        MilestoneId = MapMilestoneId(item.MilestoneId),
                        Title = item.Title,
                        Description = item.Description,
                        TaskType = Enum.TryParse<TaskType>(item.TaskType, true, out var tt) ? tt : TaskType.Habit,
                        Frequency = Enum.TryParse<Frequency>(item.Frequency, true, out var f) ? f : Frequency.AdHoc,
                        LinkedMetricCode = item.LinkedMetricCode,
                        ScheduledDate = item.ScheduledDate,
                        ScheduledTime = item.ScheduledTime,
                        StartDate = item.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                        EndDate = item.EndDate,
                        IsCompleted = item.IsCompleted,
                        CompletedAt = item.CompletedAt,
                        IsActive = item.IsActive,
                        Tags = item.Tags,
                        CreatedAt = item.CreatedAt
                    };
                    _context.Tasks.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportStreaksAsync(Guid userId, List<StreakExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            if (!dryRun)
            {
                var entity = new Streak
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TaskId = item.TaskId,
                    MetricCode = item.MetricCode,
                    CurrentStreakLength = item.CurrentStreakLength,
                    LongestStreakLength = item.LongestStreakLength,
                    LastSuccessDate = item.LastSuccessDate,
                    StreakStartDate = item.StreakStartDate,
                    MissCount = item.MissCount,
                    MaxAllowedMisses = item.MaxAllowedMisses,
                    IsActive = item.IsActive
                };
                _context.Streaks.Add(entity);
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportMetricRecordsAsync(Guid userId, List<MetricRecordExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            if (!dryRun)
            {
                var entity = new MetricRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MetricCode = item.MetricCode,
                    ValueNumber = item.ValueNumber,
                    ValueBoolean = item.ValueBoolean,
                    ValueString = item.ValueString,
                    RecordedAt = item.RecordedAt,
                    Source = item.Source,
                    Notes = item.Notes,
                    Metadata = item.Metadata
                };
                _context.MetricRecords.Add(entity);
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportScoreRecordsAsync(Guid userId, List<ScoreRecordExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.ScoreRecords.FirstOrDefaultAsync(s => s.UserId == userId && s.ScoreCode == item.ScoreCode && s.PeriodStart == item.PeriodStart, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.ScoreValue = item.ScoreValue;
                    existing.PeriodType = Enum.TryParse<ScorePeriodType>(item.PeriodType, true, out var pt) ? pt : ScorePeriodType.Daily;
                    existing.PeriodEnd = item.PeriodEnd;
                    existing.Breakdown = item.Breakdown;
                    existing.CalculatedAt = item.CalculatedAt;
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new ScoreRecord
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ScoreCode = item.ScoreCode,
                        ScoreValue = item.ScoreValue,
                        PeriodType = Enum.TryParse<ScorePeriodType>(item.PeriodType, true, out var pt) ? pt : ScorePeriodType.Daily,
                        PeriodStart = item.PeriodStart,
                        PeriodEnd = item.PeriodEnd,
                        Breakdown = item.Breakdown,
                        CalculatedAt = item.CalculatedAt
                    };
                    _context.ScoreRecords.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportIncomeSourcesAsync(Guid userId, List<IncomeSourceExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        // Get all account IDs and tax profile IDs for this user to validate foreign keys
        var accountIds = await _context.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);
        var taxProfileIds = await _context.TaxProfiles.Where(t => t.UserId == userId).Select(t => t.Id).ToListAsync(ct);

        foreach (var item in items)
        {
            var existing = await _context.IncomeSources.FirstOrDefaultAsync(i => i.UserId == userId && i.Name == item.Name, ct);
            
            // Validate foreign keys - remap first, then set to null if they don't exist
            var mappedTargetAccountId = MapAccountId(item.TargetAccountId);
            var targetAccountId = mappedTargetAccountId.HasValue && accountIds.Contains(mappedTargetAccountId.Value) ? mappedTargetAccountId : null;
            var mappedTaxProfileId = MapTaxProfileId(item.TaxProfileId);
            var taxProfileId = mappedTaxProfileId.HasValue && taxProfileIds.Contains(mappedTaxProfileId.Value) ? mappedTaxProfileId : null;
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.TaxProfileId = taxProfileId;
                    existing.Currency = item.Currency;
                    existing.BaseAmount = item.BaseAmount;
                    existing.IsPreTax = item.IsPreTax;
                    existing.PaymentFrequency = Enum.TryParse<PaymentFrequency>(item.PaymentFrequency, true, out var pf) ? pf : PaymentFrequency.Monthly;
                    existing.NextPaymentDate = item.NextPaymentDate;
                    existing.AnnualIncreaseRate = item.AnnualIncreaseRate;
                    existing.EmployerName = item.EmployerName;
                    existing.Notes = item.Notes;
                    existing.IsActive = item.IsActive;
                    existing.TargetAccountId = targetAccountId;
                }
            }
            else
            {
                var newId = await _context.IncomeSources.AnyAsync(i => i.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;

                if (!dryRun)
                {
                    var entity = new IncomeSource
                    {
                        Id = newId,
                        UserId = userId,
                        TaxProfileId = taxProfileId,
                        Name = item.Name,
                        Currency = item.Currency,
                        BaseAmount = item.BaseAmount,
                        IsPreTax = item.IsPreTax,
                        PaymentFrequency = Enum.TryParse<PaymentFrequency>(item.PaymentFrequency, true, out var pf) ? pf : PaymentFrequency.Monthly,
                        NextPaymentDate = item.NextPaymentDate,
                        AnnualIncreaseRate = item.AnnualIncreaseRate,
                        EmployerName = item.EmployerName,
                        Notes = item.Notes,
                        IsActive = item.IsActive,
                        TargetAccountId = targetAccountId
                    };
                    _context.IncomeSources.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportExpenseDefinitionsAsync(Guid userId, List<ExpenseDefinitionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        // Get all account IDs for this user to validate foreign keys
        var accountIds = await _context.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);

        foreach (var item in items)
        {
            var existing = await _context.ExpenseDefinitions.FirstOrDefaultAsync(e => e.UserId == userId && e.Name == item.Name, ct);
            
            // Validate foreign keys - remap first, then set to null if account doesn't exist
            var mappedLinkedAccountId = MapAccountId(item.LinkedAccountId);
            var linkedAccountId = mappedLinkedAccountId.HasValue && accountIds.Contains(mappedLinkedAccountId.Value) ? mappedLinkedAccountId : null;
            var mappedEndConditionAccountId = MapAccountId(item.EndConditionAccountId);
            var endConditionAccountId = mappedEndConditionAccountId.HasValue && accountIds.Contains(mappedEndConditionAccountId.Value) ? mappedEndConditionAccountId : null;
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.LinkedAccountId = linkedAccountId;
                    existing.Currency = item.Currency;
                    existing.AmountType = Enum.TryParse<AmountType>(item.AmountType, true, out var amt) ? amt : AmountType.Fixed;
                    existing.AmountValue = item.AmountValue;
                    existing.AmountFormula = item.AmountFormula;
                    existing.Frequency = Enum.TryParse<PaymentFrequency>(item.Frequency, true, out var freq) ? freq : PaymentFrequency.Monthly;
                    existing.StartDate = item.StartDate;
                    existing.Category = item.Category;
                    existing.IsTaxDeductible = item.IsTaxDeductible;
                    existing.InflationAdjusted = item.InflationAdjusted;
                    existing.IsActive = item.IsActive;
                    existing.EndConditionType = Enum.TryParse<EndConditionType>(item.EndConditionType, true, out var ect) ? ect : EndConditionType.None;
                    existing.EndConditionAccountId = endConditionAccountId;
                    existing.EndDate = item.EndDate;
                    existing.EndAmountThreshold = item.EndAmountThreshold;
                }
            }
            else
            {
                var newId = await _context.ExpenseDefinitions.AnyAsync(e => e.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;

                if (!dryRun)
                {
                    var entity = new ExpenseDefinition
                    {
                        Id = newId,
                        UserId = userId,
                        LinkedAccountId = linkedAccountId,
                        Name = item.Name,
                        Currency = item.Currency,
                        AmountType = Enum.TryParse<AmountType>(item.AmountType, true, out var amt) ? amt : AmountType.Fixed,
                        AmountValue = item.AmountValue,
                        AmountFormula = item.AmountFormula,
                        Frequency = Enum.TryParse<PaymentFrequency>(item.Frequency, true, out var freq) ? freq : PaymentFrequency.Monthly,
                        StartDate = item.StartDate,
                        Category = item.Category,
                        IsTaxDeductible = item.IsTaxDeductible,
                        InflationAdjusted = item.InflationAdjusted,
                        IsActive = item.IsActive,
                        EndConditionType = Enum.TryParse<EndConditionType>(item.EndConditionType, true, out var ect) ? ect : EndConditionType.None,
                        EndConditionAccountId = endConditionAccountId,
                        EndDate = item.EndDate,
                        EndAmountThreshold = item.EndAmountThreshold
                    };
                    _context.ExpenseDefinitions.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportInvestmentContributionsAsync(Guid userId, List<InvestmentContributionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        // Get all account IDs for this user to validate foreign keys
        var accountIds = await _context.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);

        foreach (var item in items)
        {
            var existing = await _context.InvestmentContributions.FirstOrDefaultAsync(i => i.UserId == userId && i.Name == item.Name, ct);
            
            // Validate foreign keys - remap first, then set to null if account doesn't exist
            var mappedTargetAccountId = MapAccountId(item.TargetAccountId);
            var targetAccountId = mappedTargetAccountId.HasValue && accountIds.Contains(mappedTargetAccountId.Value) ? mappedTargetAccountId : null;
            var mappedSourceAccountId = MapAccountId(item.SourceAccountId);
            var sourceAccountId = mappedSourceAccountId.HasValue && accountIds.Contains(mappedSourceAccountId.Value) ? mappedSourceAccountId : null;
            var mappedEndConditionAccountId = MapAccountId(item.EndConditionAccountId);
            var endConditionAccountId = mappedEndConditionAccountId.HasValue && accountIds.Contains(mappedEndConditionAccountId.Value) ? mappedEndConditionAccountId : null;
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.TargetAccountId = targetAccountId;
                    existing.SourceAccountId = sourceAccountId;
                    existing.Currency = item.Currency;
                    existing.Amount = item.Amount;
                    existing.Frequency = Enum.TryParse<PaymentFrequency>(item.Frequency, true, out var invfreq) ? invfreq : PaymentFrequency.Monthly;
                    existing.Category = item.Category;
                    existing.AnnualIncreaseRate = item.AnnualIncreaseRate;
                    existing.Notes = item.Notes;
                    existing.IsActive = item.IsActive;
                    existing.StartDate = item.StartDate;
                    existing.EndConditionType = Enum.TryParse<EndConditionType>(item.EndConditionType, true, out var ect) ? ect : EndConditionType.None;
                    existing.EndConditionAccountId = endConditionAccountId;
                    existing.EndDate = item.EndDate;
                    existing.EndAmountThreshold = item.EndAmountThreshold;
                }
            }
            else
            {
                var newId = await _context.InvestmentContributions.AnyAsync(i => i.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;

                if (!dryRun)
                {
                    var entity = new InvestmentContribution
                    {
                        Id = newId,
                        UserId = userId,
                        TargetAccountId = targetAccountId,
                        SourceAccountId = sourceAccountId,
                        Name = item.Name,
                        Currency = item.Currency,
                        Amount = item.Amount,
                        Frequency = Enum.TryParse<PaymentFrequency>(item.Frequency, true, out var invfreq) ? invfreq : PaymentFrequency.Monthly,
                        Category = item.Category,
                        AnnualIncreaseRate = item.AnnualIncreaseRate,
                        Notes = item.Notes,
                        IsActive = item.IsActive,
                        StartDate = item.StartDate,
                        EndConditionType = Enum.TryParse<EndConditionType>(item.EndConditionType, true, out var ect) ? ect : EndConditionType.None,
                        EndConditionAccountId = endConditionAccountId,
                        EndDate = item.EndDate,
                        EndAmountThreshold = item.EndAmountThreshold
                    };
                    _context.InvestmentContributions.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportFinancialGoalsAsync(Guid userId, List<FinancialGoalExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.FinancialGoals.FirstOrDefaultAsync(f => f.UserId == userId && f.Name == item.Name, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.TargetAmount = item.TargetAmount;
                    existing.CurrentAmount = item.CurrentAmount;
                    existing.Currency = item.Currency;
                    existing.TargetDate = item.TargetDate;
                    existing.Priority = item.Priority;
                    existing.Category = item.Category;
                    existing.IconName = item.IconName;
                    existing.Notes = item.Notes;
                    existing.IsActive = item.IsActive;
                }
            }
            else
            {
                var newId = await _context.FinancialGoals.AnyAsync(f => f.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;

                if (!dryRun)
                {
                    var entity = new FinancialGoal
                    {
                        Id = newId,
                        UserId = userId,
                        Name = item.Name,
                        TargetAmount = item.TargetAmount,
                        CurrentAmount = item.CurrentAmount,
                        Currency = item.Currency,
                        TargetDate = item.TargetDate,
                        Priority = item.Priority,
                        Category = item.Category,
                        IconName = item.IconName,
                        Notes = item.Notes,
                        IsActive = item.IsActive
                    };
                    _context.FinancialGoals.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportFxRatesAsync(List<FxRateExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.FxRates.FirstOrDefaultAsync(f => f.BaseCurrency == item.BaseCurrency && f.QuoteCurrency == item.QuoteCurrency, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.Rate = item.Rate;
                    existing.RateTimestamp = item.RateTimestamp;
                    existing.Source = item.Source;
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new FxRate
                    {
                        Id = Guid.NewGuid(),
                        BaseCurrency = item.BaseCurrency,
                        QuoteCurrency = item.QuoteCurrency,
                        Rate = item.Rate,
                        RateTimestamp = item.RateTimestamp,
                        Source = item.Source
                    };
                    _context.FxRates.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportTransactionsAsync(Guid userId, List<TransactionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        var accountIds = dryRun
            ? new List<Guid>()
            : await _context.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);

        foreach (var item in items)
        {
            if (!dryRun)
            {
                var mappedSourceAccountId = MapAccountId(item.SourceAccountId);
                var mappedTargetAccountId = MapAccountId(item.TargetAccountId);

                if (!mappedSourceAccountId.HasValue || !mappedTargetAccountId.HasValue ||
                    !accountIds.Contains(mappedSourceAccountId.Value) || !accountIds.Contains(mappedTargetAccountId.Value))
                {
                    skipped++;
                    continue;
                }

                var entity = new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SourceAccountId = mappedSourceAccountId.Value,
                    TargetAccountId = mappedTargetAccountId.Value,
                    Currency = item.Currency,
                    Amount = item.Amount,
                    AmountHomeCurrency = item.AmountHomeCurrency,
                    FxRateUsed = item.FxRateUsed,
                    Category = Enum.TryParse<TransactionCategory>(item.Category, true, out var tc) ? tc : TransactionCategory.Expense,
                    Subcategory = item.Subcategory,
                    Tags = item.Tags,
                    Description = item.Description,
                    Notes = item.Notes,
                    TransactionDate = item.TransactionDate,
                    RecordedAt = item.RecordedAt,
                    Source = item.Source,
                    IsReconciled = item.IsReconciled
                };
                _context.Transactions.Add(entity);
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportSimulationScenariosAsync(Guid userId, List<SimulationScenarioExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.SimulationScenarios.FirstOrDefaultAsync(s => s.UserId == userId && s.Name == item.Name, ct);
            
            if (existing != null)
            {
                _scenarioIdMap[item.Id] = existing.Id;
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.Description = item.Description;
                    existing.StartDate = item.StartDate;
                    existing.EndDate = item.EndDate;
                    existing.EndCondition = item.EndCondition;
                    existing.BaseAssumptions = item.BaseAssumptions;
                    existing.IsBaseline = item.IsBaseline;
                    existing.LastRunAt = item.LastRunAt;
                }
            }
            else
            {
                var newId = await _context.SimulationScenarios.AnyAsync(s => s.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                _scenarioIdMap[item.Id] = newId;

                if (!dryRun)
                {
                    var entity = new SimulationScenario
                    {
                        Id = newId,
                        UserId = userId,
                        Name = item.Name,
                        Description = item.Description,
                        StartDate = item.StartDate,
                        EndDate = item.EndDate,
                        EndCondition = item.EndCondition,
                        BaseAssumptions = item.BaseAssumptions,
                        IsBaseline = item.IsBaseline,
                        LastRunAt = item.LastRunAt
                    };
                    _context.SimulationScenarios.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportSimulationEventsAsync(Guid userId, List<SimulationEventExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        if (!dryRun)
        {
            // Get all valid account and scenario IDs for this user
            var accountIds = await _context.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);
            var scenarioIds = await _context.SimulationScenarios.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync(ct);

            foreach (var item in items)
            {
                var mappedScenarioId = MapScenarioId(item.ScenarioId);
                var mappedAffectedAccountId = MapAccountId(item.AffectedAccountId);

                // Skip if scenario doesn't exist, or if AffectedAccountId is set but doesn't exist
                if (!scenarioIds.Contains(mappedScenarioId) ||
                    (mappedAffectedAccountId.HasValue && !accountIds.Contains(mappedAffectedAccountId.Value)))
                {
                    skipped++;
                    continue;
                }

                var newId = await _context.SimulationEvents.AnyAsync(e => e.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                var entity = new SimulationEvent
                {
                    Id = newId,
                    ScenarioId = mappedScenarioId,
                    Name = item.Name,
                    Description = item.Description,
                    TriggerType = Enum.TryParse<SimTriggerType>(item.TriggerType, true, out var tt) ? tt : SimTriggerType.Date,
                    TriggerDate = item.TriggerDate,
                    TriggerAge = item.TriggerAge,
                    TriggerCondition = item.TriggerCondition,
                    EventType = item.EventType,
                    Currency = item.Currency,
                    AmountType = Enum.TryParse<AmountType>(item.AmountType, true, out var at) ? at : AmountType.Fixed,
                    AmountValue = item.AmountValue,
                    AffectedAccountId = mappedAffectedAccountId,
                    AppliesOnce = item.AppliesOnce,
                    SortOrder = item.SortOrder,
                    IsActive = item.IsActive
                };
                _context.SimulationEvents.Add(entity);
                imported++;
            }
        }
        else
        {
            imported = items.Count;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportAccountProjectionsAsync(Guid userId, List<AccountProjectionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        if (!dryRun)
        {
            // Get all valid account and scenario IDs for this user
            var accountIds = await _context.Accounts.Where(a => a.UserId == userId).Select(a => a.Id).ToListAsync(ct);
            var scenarioIds = await _context.SimulationScenarios.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync(ct);

            foreach (var item in items)
            {
                var mappedScenarioId = MapScenarioId(item.ScenarioId);
                var mappedAccountId = MapAccountId(item.AccountId);

                // Skip if account or scenario doesn't exist
                if (!accountIds.Contains(mappedAccountId) || !scenarioIds.Contains(mappedScenarioId))
                {
                    skipped++;
                    continue;
                }

                var newId = await _context.AccountProjections.AnyAsync(p => p.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                var entity = new AccountProjection
                {
                    Id = newId,
                    ScenarioId = mappedScenarioId,
                    AccountId = mappedAccountId,
                    PeriodDate = item.PeriodDate,
                    Balance = item.Balance,
                    BalanceHomeCurrency = item.BalanceHomeCurrency ?? item.Balance,
                    PeriodIncome = item.PeriodIncome ?? 0,
                    PeriodExpenses = item.PeriodExpenses ?? 0,
                    PeriodInterest = item.PeriodInterest ?? 0
                };
                _context.AccountProjections.Add(entity);
                imported++;
            }
        }
        else
        {
            imported = items.Count;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportNetWorthProjectionsAsync(Guid userId, List<NetWorthProjectionExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        if (!dryRun)
        {
            // Get all valid scenario IDs for this user
            var scenarioIds = await _context.SimulationScenarios.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync(ct);

            foreach (var item in items)
            {
                var mappedScenarioId = MapScenarioId(item.ScenarioId);

                // Skip if scenario doesn't exist
                if (!scenarioIds.Contains(mappedScenarioId))
                {
                    skipped++;
                    continue;
                }

                var newId = await _context.NetWorthProjections.AnyAsync(p => p.Id == item.Id, ct) ? Guid.NewGuid() : item.Id;
                var entity = new NetWorthProjection
                {
                    Id = newId,
                    ScenarioId = mappedScenarioId,
                    PeriodDate = item.PeriodDate,
                    TotalAssets = item.TotalAssets,
                    TotalLiabilities = item.TotalLiabilities,
                    NetWorth = item.NetWorth,
                    BreakdownByType = item.BreakdownByType ?? "{}",
                    BreakdownByCurrency = item.BreakdownByCurrency ?? "{}"
                };
                _context.NetWorthProjections.Add(entity);
                imported++;
            }
        }
        else
        {
            imported = items.Count;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportLongevitySnapshotsAsync(Guid userId, List<LongevitySnapshotExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            if (!dryRun)
            {
                var entity = new LongevitySnapshot
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Timestamp = item.CalculatedAt,
                    BaselineLifeExpectancyYears = item.BaselineLifeExpectancy,
                    TotalYearsAdded = item.EstimatedYearsAdded,
                    AdjustedLifeExpectancyYears = item.AdjustedLifeExpectancy,
                    RiskFactorCombined = 0m,  // Legacy imports don't have this
                    Breakdown = item.Breakdown ?? "[]",
                    Confidence = item.ConfidenceLevel ?? "medium"
                };
                _context.LongevitySnapshots.Add(entity);
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportAchievementsAsync(List<AchievementExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.Achievements.FirstOrDefaultAsync(a => a.Code == item.Code, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }

                if (!dryRun)
                {
                    existing.Name = item.Name;
                    existing.Description = item.Description;
                    existing.Icon = item.Icon;
                    existing.XpValue = item.XpValue;
                    existing.Category = item.Category;
                    existing.Tier = item.Tier;
                    existing.UnlockCondition = item.UnlockCondition;
                    existing.IsActive = item.IsActive;
                    existing.SortOrder = item.SortOrder;
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new Achievement
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        Icon = item.Icon,
                        XpValue = item.XpValue,
                        Category = item.Category,
                        Tier = item.Tier,
                        UnlockCondition = item.UnlockCondition,
                        IsActive = item.IsActive,
                        SortOrder = item.SortOrder
                    };
                    _context.Achievements.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportUserAchievementsAsync(Guid userId, List<UserAchievementExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            // Find achievement by code
            var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.Code == item.AchievementCode, ct);
            if (achievement == null)
            {
                skipped++;
                continue;
            }

            var existing = await _context.UserAchievements.FirstOrDefaultAsync(
                ua => ua.UserId == userId && ua.AchievementId == achievement.Id, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.UnlockedAt = item.UnlockedAt;
                    existing.Progress = item.Progress;
                    existing.UnlockContext = item.UnlockContext;
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new UserAchievement
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        AchievementId = achievement.Id,
                        UnlockedAt = item.UnlockedAt,
                        Progress = item.Progress,
                        UnlockContext = item.UnlockContext
                    };
                    _context.UserAchievements.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportUserXPAsync(Guid userId, UserXPExportDto? item, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        if (item == null)
        {
            return new ImportEntityResultDto { Imported = 0, Skipped = 0, Errors = 0 };
        }

        var existing = await _context.UserXPs.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        
        if (existing != null)
        {
            if (!isReplace) { skipped++; }
            else if (!dryRun)
            {
                existing.TotalXp = item.TotalXp;
                existing.Level = item.Level;
                existing.WeeklyXp = item.WeeklyXp;
                existing.WeekStartDate = item.WeekStartDate;
                imported++;
            }
            else { imported++; }
        }
        else
        {
            if (!dryRun)
            {
                var entity = new UserXP
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TotalXp = item.TotalXp,
                    Level = item.Level,
                    WeeklyXp = item.WeeklyXp,
                    WeekStartDate = item.WeekStartDate
                };
                _context.UserXPs.Add(entity);
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task<ImportEntityResultDto> ImportNetWorthSnapshotsAsync(Guid userId, List<NetWorthSnapshotExportDto> items, bool isReplace, bool dryRun, CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        foreach (var item in items)
        {
            var existing = await _context.NetWorthSnapshots.FirstOrDefaultAsync(
                n => n.UserId == userId && n.SnapshotDate == item.SnapshotDate, ct);
            
            if (existing != null)
            {
                if (!isReplace) { skipped++; continue; }
                
                if (!dryRun)
                {
                    existing.TotalAssets = item.TotalAssets;
                    existing.TotalLiabilities = item.TotalLiabilities;
                    existing.NetWorth = item.NetWorth;
                    existing.HomeCurrency = item.HomeCurrency;
                    existing.BreakdownByType = item.BreakdownByType;
                    existing.BreakdownByCurrency = item.BreakdownByCurrency;
                    existing.AccountCount = item.AccountCount;
                }
            }
            else
            {
                if (!dryRun)
                {
                    var entity = new NetWorthSnapshot
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        SnapshotDate = item.SnapshotDate,
                        TotalAssets = item.TotalAssets,
                        TotalLiabilities = item.TotalLiabilities,
                        NetWorth = item.NetWorth,
                        HomeCurrency = item.HomeCurrency,
                        BreakdownByType = item.BreakdownByType,
                        BreakdownByCurrency = item.BreakdownByCurrency,
                        AccountCount = item.AccountCount
                    };
                    _context.NetWorthSnapshots.Add(entity);
                }
            }
            imported++;
        }

        return new ImportEntityResultDto { Imported = imported, Skipped = skipped, Errors = 0 };
    }

    private async Task ImportProfileAsync(Guid userId, ProfileExportDto? profile, CancellationToken ct)
    {
        if (profile == null)
        {
            _logger.LogInformation("No profile data to import for user {UserId}", userId);
            return;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found during profile import", userId);
            return;
        }

        // Update basic properties
        user.HomeCurrency = profile.HomeCurrency;
        user.DateOfBirth = profile.DateOfBirth;
        user.LifeExpectancyBaseline = profile.LifeExpectancyBaseline;

        // Reconstruct DefaultAssumptions JSON
        var assumptions = new
        {
            inflationRateAnnual = profile.InflationRateAnnual ?? 0.05m,
            defaultGrowthRate = profile.DefaultGrowthRate ?? 0.07m,
            retirementAge = profile.RetirementAge ?? 65
        };
        user.DefaultAssumptions = JsonSerializer.Serialize(assumptions);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated profile for user {UserId}", userId);
    }
}
