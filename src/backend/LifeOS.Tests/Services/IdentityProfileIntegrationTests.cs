using LifeOS.Application.Commands.Identity;
using LifeOS.Application.Common.Interfaces;
using LifeOS.Application.Queries.Identity;
using LifeOS.Domain.Entities;
using LifeOS.Domain.Enums;
using LifeOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LifeOS.Tests.Services;

/// <summary>
/// Integration tests for Identity Profile CRUD operations
/// Tests: GetIdentityProfileQuery, UpdateIdentityProfileCommand, UpdateIdentityTargetsCommand
/// </summary>
public class IdentityProfileIntegrationTests : IDisposable
{
    private readonly LifeOSDbContext _context;
    private readonly Guid _testUserId;
    private readonly Guid _milestone1Id;
    private readonly Guid _milestone2Id;

    public IdentityProfileIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LifeOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LifeOSDbContext(options);

        // Seed test data
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "test_hash",
            HomeCurrency = "USD"
        };
        _context.Users.Add(user);

        var dimension = new Dimension
        {
            Code = "health_recovery",
            Name = "Health & Recovery",
            IsActive = true
        };
        _context.Dimensions.Add(dimension);

        _context.SaveChanges();
        _testUserId = user.Id;

        var milestone1 = new Milestone
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Reach 74kg target weight",
            Status = MilestoneStatus.Active
        };
        var milestone2 = new Milestone
        {
            UserId = _testUserId,
            DimensionId = dimension.Id,
            Title = "Net worth 1M by 40",
            Status = MilestoneStatus.Active
        };

        _context.Milestones.Add(milestone1);
        _context.Milestones.Add(milestone2);
        _context.SaveChanges();

        _milestone1Id = milestone1.Id;
        _milestone2Id = milestone2.Id;
    }

    [Fact]
    public async Task GetIdentityProfile_ReturnsNull_ForNewUser()
    {
        // Arrange
        var handler = new GetIdentityProfileQueryHandler(_context);
        var query = new GetIdentityProfileQuery(_testUserId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateIdentityProfile_CreatesProfile_ForNewUser()
    {
        // Arrange
        var handler = new UpdateIdentityProfileCommandHandler(_context);
        var command = new UpdateIdentityProfileCommand(
            _testUserId,
            "God of Mind-Power",
            "A disciplined achiever focused on mental mastery",
            new List<string> { "discipline", "growth", "impact", "freedom" },
            new Dictionary<string, int>
            {
                { "strength", 75 },
                { "wisdom", 95 },
                { "charisma", 80 },
                { "composure", 90 },
                { "energy", 85 },
                { "influence", 80 },
                { "vitality", 85 }
            },
            new List<Guid> { _milestone1Id, _milestone2Id }
        );

        // Act
        var success = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(success);

        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == _testUserId);
        Assert.NotNull(profile);
        Assert.Equal("God of Mind-Power", profile.Archetype);
        Assert.Equal("A disciplined achiever focused on mental mastery", profile.ArchetypeDescription);
        Assert.Contains("\"discipline\"", profile.Values);
        Assert.Contains("\"strength\":75", profile.PrimaryStatTargets);
        Assert.Contains(_milestone1Id.ToString(), profile.LinkedMilestoneIds);
    }

    [Fact]
    public async Task UpdateIdentityProfile_UpdatesExisting_Profile()
    {
        // Arrange - Create initial profile
        var createHandler = new UpdateIdentityProfileCommandHandler(_context);
        var createCommand = new UpdateIdentityProfileCommand(
            _testUserId,
            "Initial Archetype",
            "Initial description",
            new List<string> { "value1" },
            new Dictionary<string, int> { { "strength", 50 } },
            new List<Guid>()
        );
        await createHandler.Handle(createCommand, CancellationToken.None);

        // Act - Update profile
        var updateCommand = new UpdateIdentityProfileCommand(
            _testUserId,
            "Updated Archetype",
            "Updated description",
            new List<string> { "value1", "value2" },
            new Dictionary<string, int> { { "strength", 80 }, { "wisdom", 90 } },
            new List<Guid> { _milestone1Id }
        );
        var success = await createHandler.Handle(updateCommand, CancellationToken.None);

        // Assert
        Assert.True(success);

        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == _testUserId);
        Assert.NotNull(profile);
        Assert.Equal("Updated Archetype", profile.Archetype);
        Assert.Equal("Updated description", profile.ArchetypeDescription);
        Assert.Contains("\"value2\"", profile.Values);
        Assert.Contains("\"wisdom\":90", profile.PrimaryStatTargets);
    }

    [Fact]
    public async Task GetIdentityProfile_ReturnsProfile_WithLinkedMilestones()
    {
        // Arrange - Create profile
        var createHandler = new UpdateIdentityProfileCommandHandler(_context);
        var createCommand = new UpdateIdentityProfileCommand(
            _testUserId,
            "Test Archetype",
            "Test description",
            new List<string> { "test" },
            new Dictionary<string, int> { { "strength", 75 } },
            new List<Guid> { _milestone1Id, _milestone2Id }
        );
        await createHandler.Handle(createCommand, CancellationToken.None);

        // Act
        var handler = new GetIdentityProfileQueryHandler(_context);
        var query = new GetIdentityProfileQuery(_testUserId);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Archetype", result.Data.Archetype);
        Assert.Equal("Test description", result.Data.ArchetypeDescription);
        Assert.Single(result.Data.Values);
        Assert.Equal("test", result.Data.Values[0]);
        Assert.Equal(75, result.Data.PrimaryStatTargets["strength"]);
        Assert.Equal(2, result.Data.LinkedMilestones.Count);
        Assert.Contains(result.Data.LinkedMilestones, m => m.Id == _milestone1Id && m.Title == "Reach 74kg target weight");
        Assert.Contains(result.Data.LinkedMilestones, m => m.Id == _milestone2Id && m.Title == "Net worth 1M by 40");
    }

    [Fact]
    public async Task UpdateIdentityTargets_UpdatesTargetsOnly()
    {
        // Arrange - Create initial profile
        var createHandler = new UpdateIdentityProfileCommandHandler(_context);
        var createCommand = new UpdateIdentityProfileCommand(
            _testUserId,
            "Initial Archetype",
            "Initial description",
            new List<string> { "value1" },
            new Dictionary<string, int> { { "strength", 50 }, { "wisdom", 60 } },
            new List<Guid> { _milestone1Id }
        );
        await createHandler.Handle(createCommand, CancellationToken.None);

        // Act - Update targets only
        var updateHandler = new UpdateIdentityTargetsCommandHandler(_context);
        var updateCommand = new UpdateIdentityTargetsCommand(
            _testUserId,
            new Dictionary<string, int> { { "strength", 85 }, { "wisdom", 95 }, { "charisma", 80 } }
        );
        var success = await updateHandler.Handle(updateCommand, CancellationToken.None);

        // Assert
        Assert.True(success);

        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == _testUserId);
        Assert.NotNull(profile);
        Assert.Equal("Initial Archetype", profile.Archetype); // Unchanged
        Assert.Equal("Initial description", profile.ArchetypeDescription); // Unchanged
        Assert.Contains("\"value1\"", profile.Values); // Unchanged
        Assert.Contains("\"strength\":85", profile.PrimaryStatTargets); // Updated
        Assert.Contains("\"wisdom\":95", profile.PrimaryStatTargets); // Updated
        Assert.Contains("\"charisma\":80", profile.PrimaryStatTargets); // Added
        Assert.Contains(_milestone1Id.ToString(), profile.LinkedMilestoneIds); // Unchanged
    }

    [Fact]
    public async Task UpdateIdentityTargets_ReturnsFalse_WhenProfileDoesNotExist()
    {
        // Arrange
        var handler = new UpdateIdentityTargetsCommandHandler(_context);
        var command = new UpdateIdentityTargetsCommand(
            _testUserId,
            new Dictionary<string, int> { { "strength", 75 } }
        );

        // Act
        var success = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task UpdateIdentityProfile_HandlesEmptyCollections()
    {
        // Arrange
        var handler = new UpdateIdentityProfileCommandHandler(_context);
        var command = new UpdateIdentityProfileCommand(
            _testUserId,
            "Minimal Archetype",
            null,
            new List<string>(),
            new Dictionary<string, int>(),
            new List<Guid>()
        );

        // Act
        var success = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(success);

        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == _testUserId);
        Assert.NotNull(profile);
        Assert.Equal("Minimal Archetype", profile.Archetype);
        Assert.Null(profile.ArchetypeDescription);
        Assert.Equal("[]", profile.Values);
        Assert.Equal("{}", profile.PrimaryStatTargets);
        Assert.Equal("[]", profile.LinkedMilestoneIds);
    }

    [Fact]
    public async Task JsonSerializationDeserialization_WorksCorrectly()
    {
        // Arrange
        var createHandler = new UpdateIdentityProfileCommandHandler(_context);
        var values = new List<string> { "discipline", "growth", "impact", "freedom" };
        var targets = new Dictionary<string, int>
        {
            { "strength", 75 },
            { "wisdom", 95 },
            { "charisma", 80 }
        };
        var milestones = new List<Guid> { _milestone1Id, _milestone2Id };

        var createCommand = new UpdateIdentityProfileCommand(
            _testUserId,
            "Test",
            null,
            values,
            targets,
            milestones
        );
        await createHandler.Handle(createCommand, CancellationToken.None);

        // Act
        var getHandler = new GetIdentityProfileQueryHandler(_context);
        var query = new GetIdentityProfileQuery(_testUserId);
        var result = await getHandler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Data.Values.Count);
        Assert.Contains("discipline", result.Data.Values);
        Assert.Contains("freedom", result.Data.Values);
        Assert.Equal(3, result.Data.PrimaryStatTargets.Count);
        Assert.Equal(75, result.Data.PrimaryStatTargets["strength"]);
        Assert.Equal(95, result.Data.PrimaryStatTargets["wisdom"]);
        Assert.Equal(2, result.Data.LinkedMilestones.Count);
    }

    [Fact]
    public async Task UpdateIdentityProfile_ValidatesTargetValues()
    {
        // Arrange
        var handler = new UpdateIdentityProfileCommandHandler(_context);
        
        // Test with valid targets (0-100)
        var command = new UpdateIdentityProfileCommand(
            _testUserId,
            "Test",
            null,
            new List<string>(),
            new Dictionary<string, int>
            {
                { "strength", 0 },    // Min valid
                { "wisdom", 100 },    // Max valid
                { "charisma", 50 }    // Mid valid
            },
            new List<Guid>()
        );

        // Act
        var success = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(success);

        var profile = await _context.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == _testUserId);
        Assert.NotNull(profile);
        Assert.Contains("\"strength\":0", profile.PrimaryStatTargets);
        Assert.Contains("\"wisdom\":100", profile.PrimaryStatTargets);
        Assert.Contains("\"charisma\":50", profile.PrimaryStatTargets);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
