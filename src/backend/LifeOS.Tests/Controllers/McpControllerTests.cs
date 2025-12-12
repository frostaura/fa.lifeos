using System.Security.Claims;
using System.Text.Json;
using LifeOS.Api.Controllers;
using LifeOS.Application.DTOs.Mcp;
using LifeOS.Application.Interfaces.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LifeOS.Tests.Controllers;

/// <summary>
/// Integration tests for McpController MCP Tools API infrastructure.
/// Tests tool registration, execution, error handling, and discovery.
/// </summary>
public class McpControllerTests
{
    private readonly McpController _controller;
    private readonly Mock<IMcpToolHandler> _mockHandler1;
    private readonly Mock<IMcpToolHandler> _mockHandler2;
    private readonly Guid _testUserId = Guid.NewGuid();

    public McpControllerTests()
    {
        // Setup mock handlers
        _mockHandler1 = new Mock<IMcpToolHandler>();
        _mockHandler1.Setup(h => h.ToolName).Returns("lifeos.testTool1");
        _mockHandler1.Setup(h => h.Description).Returns("Test tool 1 for testing");

        _mockHandler2 = new Mock<IMcpToolHandler>();
        _mockHandler2.Setup(h => h.ToolName).Returns("lifeos.testTool2");
        _mockHandler2.Setup(h => h.Description).Returns("Test tool 2 for testing");

        var handlers = new List<IMcpToolHandler> { _mockHandler1.Object, _mockHandler2.Object };

        _controller = new McpController(handlers, NullLogger<McpController>.Instance);

        // Setup HttpContext with authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void ListTools_ReturnsAllRegisteredTools()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        // Use reflection to get the tools property
        var toolsProperty = value?.GetType().GetProperty("tools");
        Assert.NotNull(toolsProperty);
        
        var tools = toolsProperty.GetValue(value) as IEnumerable<object>;
        Assert.NotNull(tools);
        
        var toolList = tools.ToList();
        Assert.Equal(2, toolList.Count);
    }

    [Fact]
    public async Task ExecuteTool_WithValidTool_ReturnsSuccess()
    {
        // Arrange
        var testData = new { result = "test data" };
        var expectedResponse = McpToolResponse<object>.Ok(testData);
        
        _mockHandler1
            .Setup(h => h.HandleAsync(It.IsAny<string?>(), _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var input = JsonSerializer.SerializeToElement(new { param = "value" });

        // Act
        var result = await _controller.ExecuteTool("lifeos.testTool1", input, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<McpToolResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task ExecuteTool_WithUnknownTool_ReturnsNotFound()
    {
        // Arrange
        var input = JsonSerializer.SerializeToElement(new { param = "value" });

        // Act
        var result = await _controller.ExecuteTool("lifeos.unknownTool", input, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<McpToolResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteTool_WithToolException_ReturnsInternalServerError()
    {
        // Arrange
        _mockHandler1
            .Setup(h => h.HandleAsync(It.IsAny<string?>(), _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var input = JsonSerializer.SerializeToElement(new { param = "value" });

        // Act
        var result = await _controller.ExecuteTool("lifeos.testTool1", input, CancellationToken.None);

        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
        
        var response = Assert.IsType<McpToolResponse<object>>(errorResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Internal error", response.Error);
    }

    [Fact]
    public async Task ExecuteTool_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - Remove authentication
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        _controller.ControllerContext.HttpContext.User = user;

        var input = JsonSerializer.SerializeToElement(new { param = "value" });

        // Act
        var result = await _controller.ExecuteTool("lifeos.testTool1", input, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<McpToolResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("authentication", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteTool_WithNullInput_CallsHandlerWithNull()
    {
        // Arrange
        var expectedResponse = McpToolResponse<object>.Ok(new { status = "ok" });
        
        _mockHandler1
            .Setup(h => h.HandleAsync(null, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ExecuteTool("lifeos.testTool1", null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<McpToolResponse<object>>(okResult.Value);
        Assert.True(response.Success);

        _mockHandler1.Verify(h => h.HandleAsync(null, _testUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteTool_CaseInsensitiveToolName_FindsHandler()
    {
        // Arrange
        var expectedResponse = McpToolResponse<object>.Ok(new { status = "ok" });
        
        _mockHandler1
            .Setup(h => h.HandleAsync(It.IsAny<string?>(), _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var input = JsonSerializer.SerializeToElement(new { param = "value" });

        // Act - Use different case
        var result = await _controller.ExecuteTool("LIFEOS.TESTTOOL1", input, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<McpToolResponse<object>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ExecuteTool_WithFailedToolExecution_ReturnsFailureResponse()
    {
        // Arrange
        var expectedResponse = McpToolResponse<object>.Fail("Tool-specific error occurred");
        
        _mockHandler1
            .Setup(h => h.HandleAsync(It.IsAny<string?>(), _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var input = JsonSerializer.SerializeToElement(new { param = "value" });

        // Act
        var result = await _controller.ExecuteTool("lifeos.testTool1", input, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<McpToolResponse<object>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Tool-specific error occurred", response.Error);
        Assert.Null(response.Data);
    }

    [Fact]
    public void ListTools_ReturnsToolsInAlphabeticalOrder()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        
        var toolsProperty = value?.GetType().GetProperty("tools");
        Assert.NotNull(toolsProperty);
        
        var tools = toolsProperty.GetValue(value) as IEnumerable<object>;
        Assert.NotNull(tools);
        
        var toolList = tools.ToList();
        
        // Extract names using reflection
        var nameProperty = toolList[0].GetType().GetProperty("name");
        Assert.NotNull(nameProperty);
        
        var name1 = nameProperty.GetValue(toolList[0]) as string;
        var name2 = nameProperty.GetValue(toolList[1]) as string;
        
        Assert.Equal("lifeos.testTool1", name1);
        Assert.Equal("lifeos.testTool2", name2);
    }
}
