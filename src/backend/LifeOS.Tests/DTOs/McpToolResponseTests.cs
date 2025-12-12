using LifeOS.Application.DTOs.Mcp;

namespace LifeOS.Tests.DTOs;

/// <summary>
/// Unit tests for McpToolResponse wrapper class.
/// Validates MCP response format compliance.
/// </summary>
public class McpToolResponseTests
{
    [Fact]
    public void Ok_CreatesSuccessfulResponse()
    {
        // Arrange
        var testData = new { value = 42, name = "test" };

        // Act
        var response = McpToolResponse<object>.Ok(testData);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Error);
        Assert.Equal(testData, response.Data);
    }

    [Fact]
    public void Fail_CreatesFailedResponse()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var response = McpToolResponse<object>.Fail(errorMessage);

        // Assert
        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.NotNull(response.Error);
        Assert.Equal(errorMessage, response.Error);
    }

    [Fact]
    public void Ok_WithNullData_AllowsNull()
    {
        // Act
        var response = McpToolResponse<string?>.Ok(null);

        // Assert
        Assert.True(response.Success);
        Assert.Null(response.Data);
        Assert.Null(response.Error);
    }

    [Fact]
    public void Fail_WithEmptyString_AllowsEmptyError()
    {
        // Act
        var response = McpToolResponse<object>.Fail(string.Empty);

        // Assert
        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal(string.Empty, response.Error);
    }

    [Fact]
    public void Ok_WithComplexType_PreservesData()
    {
        // Arrange
        var complexData = new
        {
            id = Guid.NewGuid(),
            name = "Test User",
            scores = new[] { 85, 90, 95 },
            metadata = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 123
            }
        };

        // Act
        var response = McpToolResponse<object>.Ok(complexData);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(complexData, response.Data);
    }

    [Fact]
    public void Fail_WithDetailedError_PreservesErrorMessage()
    {
        // Arrange
        var detailedError = "ValidationError: Field 'email' is required. Field 'age' must be positive.";

        // Act
        var response = McpToolResponse<object>.Fail(detailedError);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(detailedError, response.Error);
    }

    [Fact]
    public void GenericType_WithDifferentTypes_MaintainsTypeInfo()
    {
        // Arrange & Act
        var stringResponse = McpToolResponse<string>.Ok("test");
        var intResponse = McpToolResponse<int>.Ok(42);
        var guidResponse = McpToolResponse<Guid>.Ok(Guid.NewGuid());

        // Assert
        Assert.IsType<string>(stringResponse.Data);
        Assert.IsType<int>(intResponse.Data);
        Assert.IsType<Guid>(guidResponse.Data);
    }
}
