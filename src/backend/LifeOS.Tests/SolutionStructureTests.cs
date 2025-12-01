using FluentAssertions;

namespace LifeOS.Tests;

public class SolutionStructureTests
{
    [Fact]
    public void Solution_ShouldHaveCorrectNamespace()
    {
        // Arrange & Act
        var domainAssembly = typeof(Domain.Common.BaseEntity).Assembly;
        
        // Assert
        domainAssembly.Should().NotBeNull();
        domainAssembly.GetName().Name.Should().Be("LifeOS.Domain");
    }

    [Fact]
    public void ApplicationLayer_ShouldHaveDependencyInjection()
    {
        // Arrange & Act
        var applicationAssembly = typeof(Application.DependencyInjection).Assembly;
        
        // Assert
        applicationAssembly.Should().NotBeNull();
        applicationAssembly.GetName().Name.Should().Be("LifeOS.Application");
    }

    [Fact]
    public void InfrastructureLayer_ShouldHaveDependencyInjection()
    {
        // Arrange & Act
        var infrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
        
        // Assert
        infrastructureAssembly.Should().NotBeNull();
        infrastructureAssembly.GetName().Name.Should().Be("LifeOS.Infrastructure");
    }
}
