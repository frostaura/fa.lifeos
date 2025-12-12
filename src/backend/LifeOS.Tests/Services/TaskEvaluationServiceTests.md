# Task Evaluation Service - Test Documentation

## Overview
Integration tests for TaskEvaluationService that validates task auto-completion based on metric conditions.

## Test Setup
```csharp
public class TaskEvaluationServiceTests
{
    private readonly ILifeOSDbContext _context;
    private readonly IMetricAggregationService _metricAggregationService;
    private readonly ITaskEvaluationService _evaluationService;
    
    // Setup: Create in-memory database or use test database
    // Mock MetricAggregationService for controlled metric values
}
```

## Test Cases

### 1. Task with GreaterThanOrEqual condition met → creates completion
```csharp
[Fact]
public async Task EvaluateTask_ConditionMet_GreaterThanOrEqual_CreatesCompletion()
{
    // Arrange: Task with MetricCode="steps", TargetValue=10000, Comparison=GreaterThanOrEqual
    // Mock: MetricAggregationService returns 12000
    
    // Act: EvaluateTaskAsync
    
    // Assert: TaskCompletion created with CompletionType=AutoMetric, ValueNumber=12000
}
```

### 2. Task with condition not met → no completion
```csharp
[Fact]
public async Task EvaluateTask_ConditionNotMet_NoCompletion()
{
    // Arrange: Task with MetricCode="steps", TargetValue=10000, Comparison=GreaterThanOrEqual
    // Mock: MetricAggregationService returns 8000
    
    // Act: EvaluateTaskAsync
    
    // Assert: No TaskCompletion created
}
```

### 3. Duplicate completion prevention → no duplicate created
```csharp
[Fact]
public async Task EvaluateTask_CompletionExists_PreventsDuplicate()
{
    // Arrange: Task with metric linkage, existing TaskCompletion for today
    // Mock: MetricAggregationService returns value meeting condition
    
    // Act: EvaluateTaskAsync
    
    // Assert: No new TaskCompletion created, count remains 1
}
```

### 4. Task without metric linkage → skipped
```csharp
[Fact]
public async Task EvaluateTask_NoMetricCode_Skipped()
{
    // Arrange: Task with MetricCode=null
    
    // Act: EvaluateTaskAsync
    
    // Assert: Service logs warning, no completion created
}
```

### 5. Missing metric data → no completion
```csharp
[Fact]
public async Task EvaluateTask_NoMetricData_NoCompletion()
{
    // Arrange: Task with MetricCode="steps"
    // Mock: MetricAggregationService returns null (no data)
    
    // Act: EvaluateTaskAsync
    
    // Assert: No TaskCompletion created, debug log recorded
}
```

### 6. All comparison operators work correctly
```csharp
[Theory]
[InlineData(TaskTargetComparison.GreaterThanOrEqual, 10000, 10000, true)]
[InlineData(TaskTargetComparison.GreaterThanOrEqual, 10000, 10001, true)]
[InlineData(TaskTargetComparison.GreaterThanOrEqual, 10000, 9999, false)]
[InlineData(TaskTargetComparison.LessThanOrEqual, 5000, 5000, true)]
[InlineData(TaskTargetComparison.LessThanOrEqual, 5000, 4999, true)]
[InlineData(TaskTargetComparison.LessThanOrEqual, 5000, 5001, false)]
[InlineData(TaskTargetComparison.Equal, 100, 100, true)]
[InlineData(TaskTargetComparison.Equal, 100, 100.00005, true)] // Within tolerance
[InlineData(TaskTargetComparison.Equal, 100, 101, false)]
public async Task EvaluateCondition_AllOperators_WorkCorrectly(
    TaskTargetComparison comparison, 
    decimal targetValue, 
    decimal actualValue, 
    bool expectedResult)
{
    // Test EvaluateCondition private method via reflection or make it internal/testable
}
```

### 7. Evaluation windows calculated correctly for all frequencies
```csharp
[Theory]
[InlineData(Frequency.Daily, "2024-01-15", "2024-01-15 00:00:00", "2024-01-15 23:59:59.9999999")]
[InlineData(Frequency.Weekly, "2024-01-15", "2024-01-15 00:00:00", "2024-01-21 23:59:59.9999999")] // Mon-Sun
[InlineData(Frequency.Monthly, "2024-01-15", "2024-01-01 00:00:00", "2024-01-31 23:59:59.9999999")]
[InlineData(Frequency.Quarterly, "2024-02-15", "2024-01-01 00:00:00", "2024-03-31 23:59:59.9999999")] // Q1
[InlineData(Frequency.Yearly, "2024-06-15", "2024-01-01 00:00:00", "2024-12-31 23:59:59.9999999")]
public async Task GetEvaluationWindow_AllFrequencies_ReturnsCorrectWindow(
    Frequency frequency, 
    string evaluationDateStr, 
    string expectedStartStr, 
    string expectedEndStr)
{
    // Test GetEvaluationWindow private method via reflection or make it internal/testable
}
```

### 8. EvaluateAllTasksAsync processes multiple tasks
```csharp
[Fact]
public async Task EvaluateAllTasks_MultipleTasksWithMetrics_ProcessesAll()
{
    // Arrange: 3 tasks with metric linkage, 1 without
    // Mock: MetricAggregationService returns different values for each
    
    // Act: EvaluateAllTasksAsync
    
    // Assert: 3 tasks evaluated (logged), appropriate completions created
}
```

### 9. Transaction rollback on SaveChanges failure
```csharp
[Fact]
public async Task EvaluateTask_SaveChangesFails_NoCompletionCreated()
{
    // Arrange: Task with metric linkage, mock DbContext to throw on SaveChangesAsync
    
    // Act & Assert: Exception thrown, no partial data saved
}
```

### 10. Inactive tasks are not evaluated
```csharp
[Fact]
public async Task EvaluateAllTasks_InactiveTasks_Skipped()
{
    // Arrange: Task with IsActive=false, MetricCode set
    
    // Act: EvaluateAllTasksAsync
    
    // Assert: Task not evaluated (not in query results)
}
```

### 11. Weekly evaluation handles week boundaries correctly
```csharp
[Theory]
[InlineData("2024-01-15", DayOfWeek.Monday, "2024-01-15")] // Monday
[InlineData("2024-01-16", DayOfWeek.Tuesday, "2024-01-15")] // Tuesday -> Monday start
[InlineData("2024-01-21", DayOfWeek.Sunday, "2024-01-15")] // Sunday -> Monday start
public async Task GetEvaluationWindow_Weekly_HandlesWeekBoundaries(
    string evaluationDateStr, 
    DayOfWeek dayOfWeek, 
    string expectedStartDateStr)
{
    // Test week start always begins on Monday
}
```

### 12. Logging levels used appropriately
```csharp
[Fact]
public async Task EvaluateTask_VariousScenarios_LogsAppropriately()
{
    // Test cases:
    // - Info: Task completed
    // - Debug: Condition not met
    // - Debug: No metric data
    // - Warning: Invalid configuration
    // - Error: Exception during evaluation
}
```

### 13. Notes field contains evaluation details
```csharp
[Fact]
public async Task EvaluateTask_CreatesCompletion_IncludesEvaluationDetails()
{
    // Arrange: Task with steps >= 10000
    // Mock: Returns 12000
    
    // Act: EvaluateTaskAsync
    
    // Assert: Notes = "Auto-evaluated: steps >= 10000"
}
```

## Implementation Notes

### Test Database Setup
- Use in-memory EF Core database or test container
- Seed users, dimensions, tasks, metric definitions, metric records
- Use UTC timestamps consistently

### Mocking Strategy
- Mock IMetricAggregationService for controlled metric values
- Use real EF Core context for integration testing
- Mock ILogger to verify log messages

### Test Data Patterns
```csharp
private LifeTask CreateTestTask(string metricCode, decimal targetValue, TaskTargetComparison comparison, Frequency frequency = Frequency.Daily)
{
    return new LifeTask
    {
        Id = Guid.NewGuid(),
        UserId = _testUserId,
        Title = "Test Task",
        IsActive = true,
        MetricCode = metricCode,
        TargetValue = targetValue,
        TargetComparison = comparison,
        Frequency = frequency
    };
}

private void MockMetricAggregation(string metricCode, decimal? returnValue)
{
    _mockMetricAggregationService
        .Setup(x => x.AggregateMetricAsync(metricCode, It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(returnValue);
}
```

## Running Tests
```bash
cd src/backend/LifeOS.Tests
dotnet test --filter "FullyQualifiedName~TaskEvaluationServiceTests"
```

## Coverage Target
- Line coverage: ≥80%
- Branch coverage: ≥75%
- All public methods covered
- All edge cases tested
