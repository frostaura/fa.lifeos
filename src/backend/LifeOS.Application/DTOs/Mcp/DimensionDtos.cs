using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

#region List Dimensions

/// <summary>
/// Request to list all dimensions.
/// </summary>
public class ListDimensionsRequest
{
    // No filters needed - returns all dimensions
}

/// <summary>
/// Response containing list of dimensions.
/// </summary>
public class ListDimensionsResponse
{
    [Description("Array of all life dimensions")]
    public List<DimensionSummary> Dimensions { get; set; } = new();

    [Description("Total number of dimensions")]
    public int TotalCount { get; set; }

    [Description("Average score across all dimensions")]
    public decimal AverageScore { get; set; }
}

/// <summary>
/// Summary of a dimension.
/// </summary>
public class DimensionSummary
{
    [Description("Unique identifier for the dimension")]
    public Guid Id { get; set; }

    [Description("Display name for the dimension")]
    public string Name { get; set; } = string.Empty;

    [Description("Description of the dimension")]
    public string Description { get; set; } = string.Empty;

    [Description("User's weight for this dimension (0-100, affects overall score)")]
    public decimal Weight { get; set; }

    [Description("Current score for this dimension (0-100)")]
    public decimal CurrentScore { get; set; }

    [Description("Display color for UI (hex format)")]
    public string Color { get; set; } = string.Empty;
}

#endregion

#region Get Dimension

/// <summary>
/// Request to get a single dimension by ID.
/// </summary>
public class GetDimensionRequest
{
    [Description("The unique identifier of the dimension to retrieve")]
    public Guid DimensionId { get; set; }
}

/// <summary>
/// Response for a single dimension with full details.
/// </summary>
public class GetDimensionResponse
{
    [Description("The requested dimension details")]
    public DimensionDetail Dimension { get; set; } = new();
}

/// <summary>
/// Full dimension details.
/// </summary>
public class DimensionDetail
{
    [Description("Unique identifier for the dimension")]
    public Guid Id { get; set; }

    [Description("Display name")]
    public string Name { get; set; } = string.Empty;

    [Description("Description")]
    public string Description { get; set; } = string.Empty;

    [Description("User's weight for this dimension (0-100)")]
    public decimal Weight { get; set; }

    [Description("Current score (0-100)")]
    public decimal CurrentScore { get; set; }

    [Description("Display color (hex format)")]
    public string Color { get; set; } = string.Empty;

    [Description("When the dimension was created")]
    public DateTime CreatedAt { get; set; }

    [Description("When the dimension was last updated")]
    public DateTime UpdatedAt { get; set; }

    [Description("Number of metrics linked to this dimension")]
    public int LinkedMetricCount { get; set; }
}

#endregion

#region Create Dimension

/// <summary>
/// Request to create a new dimension.
/// </summary>
public class CreateDimensionRequest
{
    [Description("Display name for the dimension")]
    public string Name { get; set; } = string.Empty;

    [Description("Description of the dimension")]
    public string Description { get; set; } = string.Empty;

    [Description("Initial weight for this dimension (0-100)")]
    public decimal Weight { get; set; }

    [Description("Display color in hex format (e.g., #6366f1)")]
    public string Color { get; set; } = "#6366f1";
}

/// <summary>
/// Response after creating a dimension.
/// </summary>
public class CreateDimensionResponse
{
    [Description("Unique identifier of the created dimension")]
    public Guid DimensionId { get; set; }

    [Description("Whether the creation was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;
}

#endregion

#region Update Dimension Weight

/// <summary>
/// Request to update a dimension's weight.
/// </summary>
public class UpdateDimensionWeightRequest
{
    [Description("The ID of the dimension to update")]
    public Guid DimensionId { get; set; }

    [Description("New weight value (0-100)")]
    public decimal NewWeight { get; set; }
}

/// <summary>
/// Response after updating dimension weight.
/// </summary>
public class UpdateDimensionWeightResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("ID of the updated dimension")]
    public Guid DimensionId { get; set; }

    [Description("Name of the updated dimension")]
    public string DimensionName { get; set; } = string.Empty;

    [Description("Previous weight value")]
    public decimal PreviousWeight { get; set; }

    [Description("New weight value")]
    public decimal NewWeight { get; set; }
}

#endregion

#region Delete Dimension

/// <summary>
/// Request to delete a dimension.
/// </summary>
public class DeleteDimensionRequest
{
    [Description("The unique identifier of the dimension to delete")]
    public Guid DimensionId { get; set; }
}

/// <summary>
/// Response after deleting a dimension.
/// </summary>
public class DeleteDimensionResponse
{
    [Description("Whether the deletion was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("ID of the deleted dimension")]
    public Guid DeletedDimensionId { get; set; }
}

#endregion
