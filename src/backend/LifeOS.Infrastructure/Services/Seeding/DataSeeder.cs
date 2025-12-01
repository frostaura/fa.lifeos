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
    }

    private async Task SeedDimensionsAsync(LifeOSDbContext context)
    {
        if (await context.Dimensions.AnyAsync())
        {
            _logger.LogInformation("Dimensions already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding 8 core dimensions...");

        var dimensions = new List<Dimension>
        {
            new Dimension
            {
                Code = "health",
                Name = "Health & Recovery",
                Description = "Physical health, sleep, exercise, and recovery",
                Icon = "🏃",
                DefaultWeight = 0.15m,
                SortOrder = 1,
                IsActive = true
            },
            new Dimension
            {
                Code = "relationships",
                Name = "Relationships",
                Description = "Family, friends, and meaningful connections",
                Icon = "❤️",
                DefaultWeight = 0.15m,
                SortOrder = 2,
                IsActive = true
            },
            new Dimension
            {
                Code = "work",
                Name = "Work & Contribution",
                Description = "Career, productivity, and professional impact",
                Icon = "💼",
                DefaultWeight = 0.15m,
                SortOrder = 3,
                IsActive = true
            },
            new Dimension
            {
                Code = "play",
                Name = "Play & Adventure",
                Description = "Fun, hobbies, travel, and leisure",
                Icon = "🎮",
                DefaultWeight = 0.10m,
                SortOrder = 4,
                IsActive = true
            },
            new Dimension
            {
                Code = "assets",
                Name = "Asset Care",
                Description = "Managing possessions, home, and environment",
                Icon = "💰",
                DefaultWeight = 0.15m,
                SortOrder = 5,
                IsActive = true
            },
            new Dimension
            {
                Code = "create",
                Name = "Create & Craft",
                Description = "Creative projects and making things",
                Icon = "🎨",
                DefaultWeight = 0.10m,
                SortOrder = 6,
                IsActive = true
            },
            new Dimension
            {
                Code = "growth",
                Name = "Growth & Mind",
                Description = "Learning, reading, and mental development",
                Icon = "📚",
                DefaultWeight = 0.10m,
                SortOrder = 7,
                IsActive = true
            },
            new Dimension
            {
                Code = "community",
                Name = "Community & Meaning",
                Description = "Purpose, spirituality, and giving back",
                Icon = "🤝",
                DefaultWeight = 0.10m,
                SortOrder = 8,
                IsActive = true
            }
        };

        context.Dimensions.AddRange(dimensions);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} dimensions", dimensions.Count);
    }

    private async Task SeedMetricDefinitionsAsync(LifeOSDbContext context)
    {
        if (await context.MetricDefinitions.AnyAsync())
        {
            _logger.LogInformation("Metric definitions already seeded, skipping");
            return;
        }

        _logger.LogInformation("Seeding metric definitions...");

        // Get dimension IDs
        var dimensions = await context.Dimensions.ToDictionaryAsync(d => d.Code, d => d.Id);

        var healthDimensionId = dimensions.GetValueOrDefault("health");
        var assetsDimensionId = dimensions.GetValueOrDefault("assets");

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
                TargetValue = 15.0m,
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
                AggregationType = AggregationType.Average,
                MinValue = 30,
                MaxValue = 200,
                TargetValue = 60.0m,
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
                AggregationType = AggregationType.Average,
                MinValue = 0,
                MaxValue = 300,
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
                AggregationType = AggregationType.Sum,
                MinValue = 0,
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
                AggregationType = AggregationType.Last,
                MinValue = 0,
                MaxValue = 1200,
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

        _logger.LogInformation("Seeding longevity models...");

        var models = new List<LongevityModel>
        {
            // Steps >= 10000 → +2.5 years (all-cause mortality reduction)
            new LongevityModel
            {
                Code = "exercise_steps",
                Name = "Daily Steps",
                Description = "High daily step count reduces all-cause mortality",
                InputMetrics = new[] { "steps" },
                ModelType = "threshold",
                Parameters = @"{
                    ""metricCode"": ""steps"",
                    ""threshold"": 10000,
                    ""direction"": ""above"",
                    ""maxYearsAdded"": 2.5
                }",
                OutputUnit = "years_added",
                SourceCitation = "Saint-Maurice PF, et al. JAMA. 2020",
                SourceUrl = "https://jamanetwork.com/journals/jama/fullarticle/2763292",
                IsActive = true,
                Version = 1
            },
            // Body fat 13-15% → +2.0 years (optimal body composition)
            new LongevityModel
            {
                Code = "body_composition",
                Name = "Optimal Body Fat",
                Description = "Optimal body fat percentage for longevity (13-15% for men)",
                InputMetrics = new[] { "body_fat_pct" },
                ModelType = "range",
                Parameters = @"{
                    ""metricCode"": ""body_fat_pct"",
                    ""optimalMin"": 13,
                    ""optimalMax"": 15,
                    ""maxYearsAdded"": 2.0
                }",
                OutputUnit = "years_added",
                SourceCitation = "Pischon T, et al. N Engl J Med. 2008",
                SourceUrl = "https://www.nejm.org/doi/full/10.1056/NEJMoa0801891",
                IsActive = true,
                Version = 1
            },
            // Sleep hours 7-9 → +1.5 years (optimal sleep duration)
            new LongevityModel
            {
                Code = "sleep_quality",
                Name = "Optimal Sleep Duration",
                Description = "Sleeping 7-9 hours per night is associated with lower mortality",
                InputMetrics = new[] { "sleep_hours" },
                ModelType = "range",
                Parameters = @"{
                    ""metricCode"": ""sleep_hours"",
                    ""optimalMin"": 7,
                    ""optimalMax"": 9,
                    ""maxYearsAdded"": 1.5
                }",
                OutputUnit = "years_added",
                SourceCitation = "Cappuccio FP, et al. Sleep. 2010",
                SourceUrl = "https://pubmed.ncbi.nlm.nih.gov/20469800/",
                IsActive = true,
                Version = 1
            },
            // Resting HR < 60 → +1.0 years (cardiovascular fitness)
            new LongevityModel
            {
                Code = "cardio_fitness",
                Name = "Cardiovascular Fitness",
                Description = "Low resting heart rate indicates cardiovascular fitness",
                InputMetrics = new[] { "resting_hr" },
                ModelType = "threshold",
                Parameters = @"{
                    ""metricCode"": ""resting_hr"",
                    ""threshold"": 60,
                    ""direction"": ""below"",
                    ""maxYearsAdded"": 1.0
                }",
                OutputUnit = "years_added",
                SourceCitation = "Zhang D, et al. CMAJ. 2016",
                SourceUrl = "https://pubmed.ncbi.nlm.nih.gov/27068421/",
                IsActive = true,
                Version = 1
            },
            // Smoke-free (12+ months) → +3.0 years
            new LongevityModel
            {
                Code = "smoke_free",
                Name = "Smoke-Free Lifestyle",
                Description = "Not smoking or quitting for 12+ months significantly increases lifespan",
                InputMetrics = new[] { "smoke_free_months" },
                ModelType = "threshold",
                Parameters = @"{
                    ""metricCode"": ""smoke_free_months"",
                    ""threshold"": 12,
                    ""direction"": ""above"",
                    ""maxYearsAdded"": 3.0
                }",
                OutputUnit = "years_added",
                SourceCitation = "Jha P, et al. N Engl J Med. 2013",
                SourceUrl = "https://www.nejm.org/doi/full/10.1056/NEJMsa1211128",
                IsActive = true,
                Version = 1
            }
        };

        context.LongevityModels.AddRange(models);
        await context.SaveChangesAsync();

        _logger.LogInformation("Successfully seeded {Count} longevity models", models.Count);
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
}
