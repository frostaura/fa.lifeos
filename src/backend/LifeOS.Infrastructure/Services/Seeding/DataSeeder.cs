using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeOS.Infrastructure.Services.Seeding;

public interface IDataSeeder
{
    Task SeedAsync();
}

public class DataSeeder : IDataSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LifeOSDbContext>();

        await SeedDimensionsAsync(context);
        await SeedMetricDefinitionsAsync(context);
        await SeedLongevityModelsAsync(context);
        await SeedDefaultTaxProfileAsync(context);
        await SeedPrimaryStatsAsync(context);
        await SeedDimensionPrimaryStatWeightsAsync(context);
    }

    private async Task SeedDimensionsAsync(LifeOSDbContext context)
    {
        if (await context.Dimensions.AnyAsync())
        {
            _logger.LogInformation("Dimensions already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding 8 core dimensions (v1.1)...");

        // v1.1 dimension codes: health_recovery, relationships, work_contribution, 
        // play_adventure, asset_care, create_craft, growth_mind, community_meaning
        var dimensions = new List<Dimension>
        {
            new Dimension
            {
                Code = "health_recovery",
                Name = "Health & Recovery",
                Description = "Physical health, sleep, exercise, and recovery. Contributes to: vitality, energy, composure.",
                Icon = "🏃",
                DefaultWeight = 0.15m,
                SortOrder = 1,
                IsActive = true
            },
            new Dimension
            {
                Code = "relationships",
                Name = "Relationships",
                Description = "Family, friends, and meaningful connections. Contributes to: charisma, influence, composure.",
                Icon = "❤️",
                DefaultWeight = 0.15m,
                SortOrder = 2,
                IsActive = true
            },
            new Dimension
            {
                Code = "work_contribution",
                Name = "Work & Contribution",
                Description = "Career, productivity, and professional impact. Contributes to: wisdom, influence, energy.",
                Icon = "💼",
                DefaultWeight = 0.15m,
                SortOrder = 3,
                IsActive = true
            },
            new Dimension
            {
                Code = "play_adventure",
                Name = "Play & Adventure",
                Description = "Fun, hobbies, travel, and leisure. Contributes to: energy, vitality, charisma.",
                Icon = "🎮",
                DefaultWeight = 0.10m,
                SortOrder = 4,
                IsActive = true
            },
            new Dimension
            {
                Code = "asset_care",
                Name = "Asset Care",
                Description = "Managing possessions, finances, and environment. Contributes to: wisdom, composure.",
                Icon = "💰",
                DefaultWeight = 0.15m,
                SortOrder = 5,
                IsActive = true
            },
            new Dimension
            {
                Code = "create_craft",
                Name = "Create & Craft",
                Description = "Creative projects and making things. Contributes to: wisdom, energy, influence.",
                Icon = "🎨",
                DefaultWeight = 0.10m,
                SortOrder = 6,
                IsActive = true
            },
            new Dimension
            {
                Code = "growth_mind",
                Name = "Growth & Mind",
                Description = "Learning, reading, and mental development. Contributes to: wisdom, strength, composure.",
                Icon = "📚",
                DefaultWeight = 0.10m,
                SortOrder = 7,
                IsActive = true
            },
            new Dimension
            {
                Code = "community_meaning",
                Name = "Community & Meaning",
                Description = "Purpose, spirituality, and giving back. Contributes to: charisma, influence, vitality.",
                Icon = "🤝",
                DefaultWeight = 0.10m,
                SortOrder = 8,
                IsActive = true
            }
        };

        context.Dimensions.AddRange(dimensions);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} dimensions (v1.1)", dimensions.Count);
    }

    private async Task SeedMetricDefinitionsAsync(LifeOSDbContext context)
    {
        if (await context.MetricDefinitions.AnyAsync())
        {
            _logger.LogInformation("Metric definitions already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding metric definitions (v1.1)...");

        // Get dimension IDs - use v1.1 codes with fallback to legacy
        var dimensions = await context.Dimensions.ToDictionaryAsync(d => d.Code, d => d.Id);

        Guid? healthDimensionId = dimensions.TryGetValue("health_recovery", out var hid) ? hid 
            : dimensions.TryGetValue("health", out hid) ? hid : null;
        Guid? assetsDimensionId = dimensions.TryGetValue("asset_care", out var aid) ? aid 
            : dimensions.TryGetValue("assets", out aid) ? aid : null;

        var metrics = new List<MetricDefinition>
        {
            // Health metrics
            new MetricDefinition
            {
                Code = "weight_kg",
                Name = "Body Weight",
                Description = "Body weight in kilograms",
                DimensionId = healthDimensionId,
                Unit = "kg",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 20,
                MaxValue = 300,
                TargetValue = 76.0m,
                TargetDirection = TargetDirection.AtOrBelow,  // Lower weight is often the goal
                Weight = 0.15m,  // 15% of Health Index
                Icon = "scale",
                Tags = new[] { "body", "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "body_fat_pct",
                Name = "Body Fat Percentage",
                Description = "Body fat percentage",
                DimensionId = healthDimensionId,
                Unit = "%",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 3,
                MaxValue = 50,
                TargetValue = 14.0m,  // Mid-point of optimal range
                TargetDirection = TargetDirection.Range,  // v3.0: 13-15% range is optimal
                Weight = 0.15m,  // 15% of Health Index
                Icon = "percent",
                Tags = new[] { "body", "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "steps",
                Name = "Daily Steps",
                Description = "Number of steps walked",
                DimensionId = healthDimensionId,
                Unit = "steps",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Sum,
                MinValue = 0,
                MaxValue = 100000,
                TargetValue = 10000m,
                TargetDirection = TargetDirection.AtOrAbove,  // Higher steps is better
                Weight = 0.10m,  // 10% of Health Index
                Icon = "footprints",
                Tags = new[] { "activity", "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "resting_hr",
                Name = "Resting Heart Rate",
                Description = "Resting heart rate in beats per minute",
                DimensionId = healthDimensionId,
                Unit = "bpm",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 30,
                MaxValue = 200,
                TargetValue = 60.0m,
                TargetDirection = TargetDirection.AtOrBelow,  // Lower HR is better for fitness
                Weight = 0.15m,  // 15% of Health Index
                Icon = "heart-pulse",
                Tags = new[] { "heart", "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "hrv_ms",
                Name = "Heart Rate Variability",
                Description = "Heart rate variability in milliseconds",
                DimensionId = healthDimensionId,
                Unit = "ms",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 0,
                MaxValue = 300,
                TargetValue = 50.0m,  // Typical good HRV
                TargetDirection = TargetDirection.AtOrAbove,  // Higher HRV is better
                Weight = 0.15m,  // 15% of Health Index
                Icon = "activity",
                Tags = new[] { "heart", "health", "recovery" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "sleep_hours",
                Name = "Sleep Duration",
                Description = "Hours of sleep",
                DimensionId = healthDimensionId,
                Unit = "hr",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Average,
                MinValue = 0,
                MaxValue = 24,
                TargetValue = 8.0m,
                TargetDirection = TargetDirection.Range,  // v3.0: 7-9 hours is optimal
                Weight = 0.10m,  // 10% of Health Index
                Icon = "moon",
                Tags = new[] { "sleep", "health", "recovery" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "bp_systolic",
                Name = "Blood Pressure (Systolic)",
                Description = "Systolic blood pressure",
                DimensionId = healthDimensionId,
                Unit = "mmHg",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 70,
                MaxValue = 250,
                TargetValue = 120.0m,
                TargetDirection = TargetDirection.AtOrBelow,  // Lower BP is healthier
                Weight = 0.10m,  // 10% of Health Index
                Icon = "stethoscope",
                Tags = new[] { "blood-pressure", "health" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "bp_diastolic",
                Name = "Blood Pressure (Diastolic)",
                Description = "Diastolic blood pressure",
                DimensionId = healthDimensionId,
                Unit = "mmHg",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 40,
                MaxValue = 150,
                TargetValue = 80.0m,
                TargetDirection = TargetDirection.AtOrBelow,  // Lower BP is healthier
                Weight = 0.10m,  // 10% of Health Index
                Icon = "stethoscope",
                Tags = new[] { "blood-pressure", "health" },
                IsActive = true
            },
            // Asset Care / Financial metrics
            new MetricDefinition
            {
                Code = "invested_amount",
                Name = "Investment Amount",
                Description = "Amount invested today",
                DimensionId = assetsDimensionId,
                Unit = "currency",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                MinValue = 0,
                TargetDirection = TargetDirection.AtOrAbove,  // More investment is better
                Icon = "trending-up",
                Tags = new[] { "finance", "investment" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "daily_spend",
                Name = "Daily Spending",
                Description = "Total spending for the day",
                DimensionId = assetsDimensionId,
                Unit = "currency",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Sum,
                MinValue = 0,
                TargetDirection = TargetDirection.AtOrBelow,  // Lower spending can be goal
                Icon = "credit-card",
                Tags = new[] { "finance", "expense" },
                IsActive = true
            },
            new MetricDefinition
            {
                Code = "net_worth",
                Name = "Net Worth",
                Description = "Total net worth snapshot",
                DimensionId = assetsDimensionId,
                Unit = "currency",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Last,
                TargetDirection = TargetDirection.AtOrAbove,  // Higher net worth is better
                Icon = "wallet",
                Tags = new[] { "finance", "wealth" },
                IsActive = true
            },
            // Longevity-related metric
            new MetricDefinition
            {
                Code = "smoke_free_months",
                Name = "Smoke-Free Duration",
                Description = "Months since last cigarette (0 if currently smoking)",
                DimensionId = healthDimensionId,
                Unit = "months",
                ValueType = MetricValueType.Number,
                AggregationType = AggregationType.Average,
                MinValue = 0,
                MaxValue = 1200,
                TargetValue = 12.0m,
                TargetDirection = TargetDirection.AtOrAbove,  // More smoke-free time is better
                Icon = "cigarette-off",
                Tags = new[] { "health", "longevity", "lifestyle" },
                IsActive = true
            }
        };

        context.MetricDefinitions.AddRange(metrics);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} metric definitions", metrics.Count);
    }

    private async Task SeedLongevityModelsAsync(LifeOSDbContext context)
    {
        if (await context.LongevityModels.AnyAsync())
        {
            _logger.LogInformation("Longevity models already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding longevity models (v3.0)...");

        var models = new List<LongevityModel>
        {
            // Steps >= 10000 → 15% risk reduction
            new LongevityModel
            {
                Code = "steps_10k",
                Name = "Daily Steps (10k+ threshold)",
                Description = "High daily step count (>=10,000 steps) reduces all-cause mortality by approximately 15%",
                InputMetrics = System.Text.Json.JsonSerializer.Serialize(new[] { "steps" }),
                ModelType = LongevityModelType.Threshold,
                Parameters = @"{""threshold"": 10000, ""belowValue"": 0, ""aboveValue"": 0.15}",
                MaxRiskReduction = 0.15m,
                SourceCitation = "Saint-Maurice PF, et al. Association of Daily Step Count and Step Intensity With Mortality Among US Adults. JAMA. 2020;323(12):1151-1160.",
                SourceUrl = "https://jamanetwork.com/journals/jama/fullarticle/2763292",
                IsActive = true
            },
            // Body fat 13-15% → 20% risk reduction (optimal range)
            new LongevityModel
            {
                Code = "bf_optimal",
                Name = "Optimal Body Fat Percentage",
                Description = "Optimal body fat percentage (13-15% for men) associated with ~20% mortality risk reduction",
                InputMetrics = System.Text.Json.JsonSerializer.Serialize(new[] { "body_fat_pct" }),
                ModelType = LongevityModelType.Range,
                Parameters = @"{""minOptimal"": 13, ""maxOptimal"": 15, ""minValue"": 5, ""maxValue"": 40}",
                MaxRiskReduction = 0.20m,
                SourceCitation = "Pischon T, et al. General and Abdominal Adiposity and Risk of Death in Europe. N Engl J Med. 2008;359:2105-2120.",
                SourceUrl = "https://www.nejm.org/doi/full/10.1056/NEJMoa0801891",
                IsActive = true
            },
            // Sleep 7-9 hours → 10% risk reduction
            new LongevityModel
            {
                Code = "sleep_optimal",
                Name = "Optimal Sleep Duration",
                Description = "Sleeping 7-9 hours per night is associated with ~10% lower mortality risk",
                InputMetrics = System.Text.Json.JsonSerializer.Serialize(new[] { "sleep_hours" }),
                ModelType = LongevityModelType.Range,
                Parameters = @"{""minOptimal"": 7, ""maxOptimal"": 9, ""minValue"": 4, ""maxValue"": 12}",
                MaxRiskReduction = 0.10m,
                SourceCitation = "Cappuccio FP, et al. Sleep Duration and All-Cause Mortality: A Systematic Review and Meta-Analysis of Prospective Studies. Sleep. 2010;33(5):585-592.",
                SourceUrl = "https://pubmed.ncbi.nlm.nih.gov/20469800/",
                IsActive = true
            },
            // Resting HR < 60 → 12% risk reduction (cardiovascular fitness)
            new LongevityModel
            {
                Code = "rhr_low",
                Name = "Low Resting Heart Rate",
                Description = "Resting heart rate below 60 bpm indicates cardiovascular fitness, ~12% mortality reduction",
                InputMetrics = System.Text.Json.JsonSerializer.Serialize(new[] { "resting_hr" }),
                ModelType = LongevityModelType.Threshold,
                Parameters = @"{""threshold"": 60, ""belowValue"": 0.12, ""aboveValue"": 0}",
                MaxRiskReduction = 0.12m,
                SourceCitation = "Zhang D, et al. Resting Heart Rate and All-Cause and Cardiovascular Mortality in the General Population: A Meta-analysis. CMAJ. 2016;188(3):E53-E63.",
                SourceUrl = "https://pubmed.ncbi.nlm.nih.gov/27068421/",
                IsActive = true
            },
            // Smoke-free (12+ months) → 25% risk reduction
            new LongevityModel
            {
                Code = "smoke_free_12m",
                Name = "Smoke-Free Lifestyle",
                Description = "Non-smoking or quitting for 12+ months significantly reduces mortality risk by ~25%",
                InputMetrics = System.Text.Json.JsonSerializer.Serialize(new[] { "smoke_free_months" }),
                ModelType = LongevityModelType.Threshold,
                Parameters = @"{""threshold"": 12, ""belowValue"": 0, ""aboveValue"": 0.25}",
                MaxRiskReduction = 0.25m,
                SourceCitation = "Jha P, et al. 21st-Century Hazards of Smoking and Benefits of Cessation in the United States. N Engl J Med. 2013;368:341-350.",
                SourceUrl = "https://www.nejm.org/doi/full/10.1056/NEJMsa1211128",
                IsActive = true
            },
            // Exercise 150+ minutes/week → 18% risk reduction
            new LongevityModel
            {
                Code = "exercise_150min",
                Name = "Recommended Weekly Exercise",
                Description = "Meeting WHO guidelines of 150+ minutes moderate-intensity exercise per week reduces mortality by ~18%",
                InputMetrics = System.Text.Json.JsonSerializer.Serialize(new[] { "weekly_exercise_min" }),
                ModelType = LongevityModelType.Threshold,
                Parameters = @"{""threshold"": 150, ""belowValue"": 0, ""aboveValue"": 0.18}",
                MaxRiskReduction = 0.18m,
                SourceCitation = "Arem H, et al. Leisure Time Physical Activity and Mortality: A Detailed Pooled Analysis of the Dose-Response Relationship. JAMA Intern Med. 2015;175(6):959-967.",
                SourceUrl = "https://jamanetwork.com/journals/jamainternalmedicine/fullarticle/2212268",
                IsActive = true
            }
        };

        context.LongevityModels.AddRange(models);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} longevity models (v3.0)", models.Count);
    }

    private async Task SeedDefaultTaxProfileAsync(LifeOSDbContext context)
    {
        // Seed tax profiles for all users (upsert pattern)
        var users = await context.Users.ToListAsync();
        if (!users.Any())
        {
            _logger.LogInformation("No users found, skipping tax profile seeding");
            return;
        }

        _logger.LogInformation("Seeding/updating South African tax profiles for {Count} users...", users.Count);

        // South African 2024/2025 tax brackets (SARS official rates)
        var saTaxBrackets = @"[
            {""min"": 0, ""max"": 237100, ""rate"": 0.18, ""baseTax"": 0},
            {""min"": 237101, ""max"": 370500, ""rate"": 0.26, ""baseTax"": 42678},
            {""min"": 370501, ""max"": 512800, ""rate"": 0.31, ""baseTax"": 77362},
            {""min"": 512801, ""max"": 673000, ""rate"": 0.36, ""baseTax"": 121475},
            {""min"": 673001, ""max"": 857900, ""rate"": 0.39, ""baseTax"": 179147},
            {""min"": 857901, ""max"": 1817000, ""rate"": 0.41, ""baseTax"": 251258},
            {""min"": 1817001, ""max"": null, ""rate"": 0.45, ""baseTax"": 644489}
        ]";

        // Tax rebates for 2024/2025 tax year
        var taxRebates = @"{
            ""primary"": 17235,
            ""secondary"": 9444,
            ""tertiary"": 3145
        }";

        foreach (var user in users)
        {
            // Check if SA tax profile already exists for this user
            var existingProfile = await context.TaxProfiles
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.CountryCode == "ZA" && t.TaxYear == 2024);

            if (existingProfile != null)
            {
                // Update existing profile
                existingProfile.Name = "SA Tax 2024/2025";
                existingProfile.Brackets = saTaxBrackets;
                existingProfile.UifRate = 0.01m;
                existingProfile.UifCap = 177.12m;
                existingProfile.VatRate = 0.15m;
                existingProfile.TaxRebates = taxRebates;
                existingProfile.IsActive = true;
                existingProfile.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Updated SA tax profile for user {UserId}", user.Id);
            }
            else
            {
                // Create new profile
                var taxProfile = new TaxProfile
                {
                    UserId = user.Id,
                    Name = "SA Tax 2024/2025",
                    TaxYear = 2024,
                    CountryCode = "ZA",
                    Brackets = saTaxBrackets,
                    UifRate = 0.01m,
                    UifCap = 177.12m, // Monthly contribution cap (R17,712 income ceiling × 1%)
                    VatRate = 0.15m,
                    IsVatRegistered = false,
                    TaxRebates = taxRebates,
                    IsActive = true
                };

                context.TaxProfiles.Add(taxProfile);
                _logger.LogInformation("Created SA tax profile for user {UserId}", user.Id);
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Successfully seeded/updated South African tax profiles");
    }

    private async Task SeedPrimaryStatsAsync(LifeOSDbContext context)
    {
        if (await context.PrimaryStats.AnyAsync())
        {
            _logger.LogInformation("Primary stats already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding 7 primary stats (v4.0)...");

        var primaryStats = new List<PrimaryStat>
        {
            new PrimaryStat
            {
                Code = "strength",
                Name = "Strength",
                Description = "Physical power, resilience, and health. Represents your body's capacity to endure and perform.",
                Icon = "💪",
                SortOrder = 1,
                IsActive = true
            },
            new PrimaryStat
            {
                Code = "wisdom",
                Name = "Wisdom",
                Description = "Knowledge, learning, and mental clarity. Represents your intellectual capacity and decision-making ability.",
                Icon = "🧠",
                SortOrder = 2,
                IsActive = true
            },
            new PrimaryStat
            {
                Code = "charisma",
                Name = "Charisma",
                Description = "Social influence and relationships. Represents your ability to connect with and inspire others.",
                Icon = "🗣️",
                SortOrder = 3,
                IsActive = true
            },
            new PrimaryStat
            {
                Code = "composure",
                Name = "Composure",
                Description = "Emotional control and stress management. Represents your ability to remain calm and focused under pressure.",
                Icon = "🧘",
                SortOrder = 4,
                IsActive = true
            },
            new PrimaryStat
            {
                Code = "energy",
                Name = "Energy",
                Description = "Vitality, stamina, and drive. Represents your capacity to take action and maintain momentum.",
                Icon = "⚡",
                SortOrder = 5,
                IsActive = true
            },
            new PrimaryStat
            {
                Code = "influence",
                Name = "Influence",
                Description = "Impact on others and leadership. Represents your ability to effect change and guide others.",
                Icon = "👑",
                SortOrder = 6,
                IsActive = true
            },
            new PrimaryStat
            {
                Code = "vitality",
                Name = "Vitality",
                Description = "Overall life force and longevity. Represents your fundamental health and life expectancy.",
                Icon = "❤️",
                SortOrder = 7,
                IsActive = true
            }
        };

        context.PrimaryStats.AddRange(primaryStats);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} primary stats (v4.0)", primaryStats.Count);
    }

    private async Task SeedDimensionPrimaryStatWeightsAsync(LifeOSDbContext context)
    {
        if (await context.DimensionPrimaryStatWeights.AnyAsync())
        {
            _logger.LogInformation("Dimension primary stat weights already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding dimension → primary stat weights (v4.0)...");

        // Get dimension IDs by code
        var dimensions = await context.Dimensions.ToDictionaryAsync(d => d.Code, d => d.Id);

        var weights = new List<DimensionPrimaryStatWeight>();

        // Health & Recovery → Vitality (40%), Strength (30%), Energy (30%)
        if (dimensions.TryGetValue("health_recovery", out var healthId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = healthId, PrimaryStatCode = "vitality", Weight = 0.40m },
                new DimensionPrimaryStatWeight { DimensionId = healthId, PrimaryStatCode = "strength", Weight = 0.30m },
                new DimensionPrimaryStatWeight { DimensionId = healthId, PrimaryStatCode = "energy", Weight = 0.30m }
            });
        }

        // Relationships → Charisma (50%), Influence (30%), Composure (20%)
        if (dimensions.TryGetValue("relationships", out var relationshipsId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = relationshipsId, PrimaryStatCode = "charisma", Weight = 0.50m },
                new DimensionPrimaryStatWeight { DimensionId = relationshipsId, PrimaryStatCode = "influence", Weight = 0.30m },
                new DimensionPrimaryStatWeight { DimensionId = relationshipsId, PrimaryStatCode = "composure", Weight = 0.20m }
            });
        }

        // Work & Contribution → Energy (35%), Influence (35%), Wisdom (30%)
        if (dimensions.TryGetValue("work_contribution", out var workId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = workId, PrimaryStatCode = "energy", Weight = 0.35m },
                new DimensionPrimaryStatWeight { DimensionId = workId, PrimaryStatCode = "influence", Weight = 0.35m },
                new DimensionPrimaryStatWeight { DimensionId = workId, PrimaryStatCode = "wisdom", Weight = 0.30m }
            });
        }

        // Play & Adventure → Energy (50%), Vitality (30%), Charisma (20%)
        if (dimensions.TryGetValue("play_adventure", out var playId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = playId, PrimaryStatCode = "energy", Weight = 0.50m },
                new DimensionPrimaryStatWeight { DimensionId = playId, PrimaryStatCode = "vitality", Weight = 0.30m },
                new DimensionPrimaryStatWeight { DimensionId = playId, PrimaryStatCode = "charisma", Weight = 0.20m }
            });
        }

        // Asset Care → Wisdom (40%), Composure (35%), Influence (25%)
        if (dimensions.TryGetValue("asset_care", out var assetId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = assetId, PrimaryStatCode = "wisdom", Weight = 0.40m },
                new DimensionPrimaryStatWeight { DimensionId = assetId, PrimaryStatCode = "composure", Weight = 0.35m },
                new DimensionPrimaryStatWeight { DimensionId = assetId, PrimaryStatCode = "influence", Weight = 0.25m }
            });
        }

        // Create & Craft → Energy (40%), Wisdom (35%), Charisma (25%)
        if (dimensions.TryGetValue("create_craft", out var createId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = createId, PrimaryStatCode = "energy", Weight = 0.40m },
                new DimensionPrimaryStatWeight { DimensionId = createId, PrimaryStatCode = "wisdom", Weight = 0.35m },
                new DimensionPrimaryStatWeight { DimensionId = createId, PrimaryStatCode = "charisma", Weight = 0.25m }
            });
        }

        // Growth & Mind → Wisdom (50%), Composure (30%), Energy (20%)
        if (dimensions.TryGetValue("growth_mind", out var growthId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = growthId, PrimaryStatCode = "wisdom", Weight = 0.50m },
                new DimensionPrimaryStatWeight { DimensionId = growthId, PrimaryStatCode = "composure", Weight = 0.30m },
                new DimensionPrimaryStatWeight { DimensionId = growthId, PrimaryStatCode = "energy", Weight = 0.20m }
            });
        }

        // Community & Meaning → Influence (40%), Charisma (35%), Composure (25%)
        if (dimensions.TryGetValue("community_meaning", out var communityId))
        {
            weights.AddRange(new[]
            {
                new DimensionPrimaryStatWeight { DimensionId = communityId, PrimaryStatCode = "influence", Weight = 0.40m },
                new DimensionPrimaryStatWeight { DimensionId = communityId, PrimaryStatCode = "charisma", Weight = 0.35m },
                new DimensionPrimaryStatWeight { DimensionId = communityId, PrimaryStatCode = "composure", Weight = 0.25m }
            });
        }

        context.DimensionPrimaryStatWeights.AddRange(weights);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} dimension → primary stat weight mappings (v4.0)", weights.Count);
    }
}
