using LifeOS.Application.Services;
using LifeOS.Domain.Entities;
using Xunit;

namespace LifeOS.Tests.Services;

public class StreakServiceTests
{
    private readonly IStreakService _service;

    public StreakServiceTests()
    {
        _service = new StreakService();
    }

    #region First Miss (Forgiving) Tests

    [Fact]
    public void FirstMiss_SetsConsecutiveMissesTo1_NoRiskPenalty()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 5,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(1, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore); // Forgiving first miss
        Assert.Equal(0, streak.CurrentStreakLength); // Streak resets on miss
    }

    [Fact]
    public void FirstMiss_PreservesLongestStreak()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 10,
            LongestStreakLength = 8,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(10, streak.LongestStreakLength); // Should update to current
        Assert.Equal(0, streak.CurrentStreakLength);
    }

    [Fact]
    public void FirstMiss_WithExistingPenalty_PreservesPenalty()
    {
        // Arrange: User had previous penalties but recovered
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 3,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 8 // Previous penalty not fully decayed
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(1, streak.ConsecutiveMisses);
        Assert.Equal(8, streak.RiskPenaltyScore); // Forgiving first miss keeps existing penalty
    }

    #endregion

    #region Second Miss Tests

    [Fact]
    public void SecondMiss_AppliesPenaltyOf5()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 1,
            RiskPenaltyScore = 0
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(2, streak.ConsecutiveMisses);
        Assert.Equal(5, streak.RiskPenaltyScore);
    }

    [Fact]
    public void SecondMiss_OverwritesExistingPenalty()
    {
        // Arrange: Had penalty, first miss forgiven, now second miss
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 1,
            RiskPenaltyScore = 12 // From previous violations
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(2, streak.ConsecutiveMisses);
        Assert.Equal(5, streak.RiskPenaltyScore); // Penalty set to 5, not added
    }

    #endregion

    #region Escalating Penalty Tests (Third+ Miss)

    [Fact]
    public void ThirdMiss_AppliesPenaltyOf20()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 2,
            RiskPenaltyScore = 5
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(3, streak.ConsecutiveMisses);
        Assert.Equal(20, streak.RiskPenaltyScore); // 10 × (3-1) = 20
    }

    [Fact]
    public void FourthMiss_AppliesPenaltyOf30()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 3,
            RiskPenaltyScore = 20
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(4, streak.ConsecutiveMisses);
        Assert.Equal(30, streak.RiskPenaltyScore); // 10 × (4-1) = 30
    }

    [Fact]
    public void FifthMiss_AppliesPenaltyOf40()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 4,
            RiskPenaltyScore = 30
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(5, streak.ConsecutiveMisses);
        Assert.Equal(40, streak.RiskPenaltyScore); // 10 × (5-1) = 40
    }

    [Fact]
    public void TenthMiss_AppliesPenaltyOf90()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 9,
            RiskPenaltyScore = 80
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(10, streak.ConsecutiveMisses);
        Assert.Equal(90, streak.RiskPenaltyScore); // 10 × (10-1) = 90
    }

    #endregion

    #region Success After Miss Tests

    [Fact]
    public void SuccessAfterFirstMiss_ResetsConsecutiveMissesToZero()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 1,
            RiskPenaltyScore = 0,
            LongestStreakLength = 5
        };

        // Act
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(1, streak.CurrentStreakLength);
        Assert.Equal(0, streak.RiskPenaltyScore); // Was 0, decay applies but stays 0
    }

    [Fact]
    public void SuccessAfterSecondMiss_DecaysPenaltyFrom5To3()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 2,
            RiskPenaltyScore = 5
        };

        // Act
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(1, streak.CurrentStreakLength);
        Assert.Equal(3, streak.RiskPenaltyScore); // 5 - 2 = 3
    }

    [Fact]
    public void SuccessAfterThirdMiss_DecaysPenaltyFrom20To18()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 3,
            RiskPenaltyScore = 20
        };

        // Act
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(1, streak.CurrentStreakLength);
        Assert.Equal(18, streak.RiskPenaltyScore); // 20 - 2 = 18
    }

    #endregion

    #region Multiple Success Decay Tests

    [Fact]
    public void MultipleSuccesses_ContinuesDecayingPenalty()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 1,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 10
        };

        // Act: 3 successes
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));

        // Assert
        Assert.Equal(4, streak.CurrentStreakLength);
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(4, streak.RiskPenaltyScore); // 10 - 2 - 2 - 2 = 4
    }

    [Fact]
    public void MultipleSuccesses_DecayStopsAtZero()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 1,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 3
        };

        // Act: 3 successes (more than enough to reach 0)
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));

        // Assert
        Assert.Equal(4, streak.CurrentStreakLength);
        Assert.Equal(0, streak.RiskPenaltyScore); // 3 - 2 = 1, 1 - 2 = 0 (capped), 0 - 2 = 0
    }

    [Fact]
    public void FiveSuccesses_FullyDecaysPenaltyOf10()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 10
        };

        // Act: 5 successes (exactly enough to decay 10 points)
        for (int i = 0; i < 5; i++)
        {
            _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i)));
        }

        // Assert
        Assert.Equal(5, streak.CurrentStreakLength);
        Assert.Equal(0, streak.RiskPenaltyScore); // 10 - 2×5 = 0
    }

    #endregion

    #region Longest Streak Tracking Tests

    [Fact]
    public void Success_UpdatesLongestStreak_WhenCurrentExceeds()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 9,
            LongestStreakLength = 9,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(10, streak.CurrentStreakLength);
        Assert.Equal(10, streak.LongestStreakLength); // Updated to match current
    }

    [Fact]
    public void Success_DoesNotUpdateLongestStreak_WhenCurrentBelow()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 3,
            LongestStreakLength = 15,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(4, streak.CurrentStreakLength);
        Assert.Equal(15, streak.LongestStreakLength); // Unchanged
    }

    [Fact]
    public void Miss_UpdatesLongestStreak_WhenCurrentWasHigher()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 20,
            LongestStreakLength = 15,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(0, streak.CurrentStreakLength); // Reset on miss
        Assert.Equal(20, streak.LongestStreakLength); // Updated before reset
    }

    #endregion

    #region EvaluateStreakStatus Integration Tests

    [Fact]
    public void EvaluateStreakStatus_WithSuccess_CallsSuccessLogic()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 5,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 10
        };

        // Act
        _service.EvaluateStreakStatus(streak, success: true);

        // Assert
        Assert.Equal(6, streak.CurrentStreakLength);
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(8, streak.RiskPenaltyScore); // Decayed by 2
    }

    [Fact]
    public void EvaluateStreakStatus_WithMiss_CallsMissLogic()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 5,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act
        _service.EvaluateStreakStatus(streak, success: false);

        // Assert
        Assert.Equal(0, streak.CurrentStreakLength);
        Assert.Equal(1, streak.ConsecutiveMisses); // First miss
        Assert.Equal(0, streak.RiskPenaltyScore); // Forgiving
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void ComplexScenario_Miss_Success_Miss_Miss_Success()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 10,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0,
            LongestStreakLength = 10
        };

        // Act & Assert

        // Miss 1: Forgiving
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(0, streak.CurrentStreakLength);
        Assert.Equal(1, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore);
        Assert.Equal(10, streak.LongestStreakLength);

        // Success: Recovery
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        Assert.Equal(1, streak.CurrentStreakLength);
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore);

        // Miss 2: First of new sequence (forgiving again)
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        Assert.Equal(0, streak.CurrentStreakLength);
        Assert.Equal(1, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore);

        // Miss 3: Second consecutive (penalty applies)
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        Assert.Equal(0, streak.CurrentStreakLength);
        Assert.Equal(2, streak.ConsecutiveMisses);
        Assert.Equal(5, streak.RiskPenaltyScore);

        // Success: Decay penalty
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)));
        Assert.Equal(1, streak.CurrentStreakLength);
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(3, streak.RiskPenaltyScore); // 5 - 2 = 3
    }

    [Fact]
    public void ComplexScenario_EscalatingMisses_ThenRecovery()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 5,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Miss 1: Forgiving
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(1, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore);

        // Miss 2: Penalty = 5
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
        Assert.Equal(2, streak.ConsecutiveMisses);
        Assert.Equal(5, streak.RiskPenaltyScore);

        // Miss 3: Penalty = 20
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        Assert.Equal(3, streak.ConsecutiveMisses);
        Assert.Equal(20, streak.RiskPenaltyScore);

        // Miss 4: Penalty = 30
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
        Assert.Equal(4, streak.ConsecutiveMisses);
        Assert.Equal(30, streak.RiskPenaltyScore);

        // Now recovery: 15 successes to fully clear penalty
        for (int i = 0; i < 15; i++)
        {
            _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4 + i)));
        }

        Assert.Equal(15, streak.CurrentStreakLength);
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore); // 30 - 2×15 = 0
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void PenaltyDecay_NeverGoesNegative()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 5,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 1
        };

        // Act
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(0, streak.RiskPenaltyScore); // Should be 0, not -1
    }

    [Fact]
    public void NewStreak_StartsWithZeroPenalty()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0
        };

        // Act: First success
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.Equal(1, streak.CurrentStreakLength);
        Assert.Equal(0, streak.ConsecutiveMisses);
        Assert.Equal(0, streak.RiskPenaltyScore);
    }

    [Fact]
    public void LastPenaltyCalculatedAt_UpdatedOnBothSuccessAndMiss()
    {
        // Arrange
        var streak = new Streak
        {
            UserId = Guid.NewGuid(),
            CurrentStreakLength = 0,
            ConsecutiveMisses = 0,
            RiskPenaltyScore = 0,
            LastPenaltyCalculatedAt = null
        };

        // Act: Success
        _service.UpdateStreakOnSuccess(streak, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(streak.LastPenaltyCalculatedAt);
        var firstTimestamp = streak.LastPenaltyCalculatedAt;

        // Act: Miss (after a small delay to ensure different timestamp)
        System.Threading.Thread.Sleep(10);
        _service.UpdateStreakOnMiss(streak, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

        // Assert
        Assert.NotNull(streak.LastPenaltyCalculatedAt);
        Assert.NotEqual(firstTimestamp, streak.LastPenaltyCalculatedAt);
    }

    #endregion
}
