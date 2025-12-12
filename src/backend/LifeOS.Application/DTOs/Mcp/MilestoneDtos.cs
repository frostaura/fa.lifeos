using System.ComponentModel;

namespace LifeOS.Application.DTOs.Mcp;

#region List Milestones

/// <summary>
/// Request to list milestones with filtering options.
/// </summary>
public class ListMilestonesRequest
{
    [Description("Filter by status: 'active' for incomplete, 'completed' for done, or leave empty for all")]
    public string StatusFilter { get; set; } = string.Empty;
}

/// <summary>
/// Response containing list of milestones.
/// </summary>
public class ListMilestonesResponse
{
    [Description("Array of milestones matching the filter criteria")]
    public List<MilestoneSummary> Milestones { get; set; } = new();

    [Description("Total number of milestones returned")]
    public int TotalCount { get; set; }

    [Description("Number of active (incomplete) milestones")]
    public int ActiveCount { get; set; }

    [Description("Number of completed milestones")]
    public int CompletedCount { get; set; }
}

/// <summary>
/// Summary of a milestone for list views.
/// </summary>
public class MilestoneSummary
{
    [Description("Unique identifier for the milestone")]
    public Guid Id { get; set; }

    [Description("Milestone title")]
    public string Title { get; set; } = string.Empty;

    [Description("Target completion date")]
    public DateTime? TargetDate { get; set; }

    [Description("Progress percentage (0-100)")]
    public decimal ProgressPercent { get; set; }

    [Description("Whether the milestone is complete")]
    public bool IsCompleted { get; set; }

    [Description("Category or area of life this milestone belongs to")]
    public string Category { get; set; } = string.Empty;
}

#endregion

#region Get Milestone

/// <summary>
/// Request to get a single milestone by ID.
/// </summary>
public class GetMilestoneRequest
{
    [Description("The unique identifier of the milestone to retrieve")]
    public Guid MilestoneId { get; set; }
}

/// <summary>
/// Response for a single milestone with full details.
/// </summary>
public class GetMilestoneResponse
{
    [Description("The requested milestone details")]
    public MilestoneDetail Milestone { get; set; } = new();
}

/// <summary>
/// Full milestone details.
/// </summary>
public class MilestoneDetail
{
    [Description("Unique identifier for the milestone")]
    public Guid Id { get; set; }

    [Description("Milestone title")]
    public string Title { get; set; } = string.Empty;

    [Description("Detailed milestone description")]
    public string Description { get; set; } = string.Empty;

    [Description("Target completion date")]
    public DateTime? TargetDate { get; set; }

    [Description("Progress percentage (0-100)")]
    public decimal ProgressPercent { get; set; }

    [Description("Whether the milestone is complete")]
    public bool IsCompleted { get; set; }

    [Description("Category or area of life")]
    public string Category { get; set; } = string.Empty;

    [Description("When the milestone was created")]
    public DateTime CreatedAt { get; set; }

    [Description("When the milestone was completed (if applicable)")]
    public DateTime? CompletedAt { get; set; }

    [Description("Additional notes")]
    public string Notes { get; set; } = string.Empty;
}

#endregion

#region Create Milestone

/// <summary>
/// Request to create a new milestone.
/// </summary>
public class CreateMilestoneRequest
{
    [Description("Milestone title (required)")]
    public string Title { get; set; } = string.Empty;

    [Description("Detailed milestone description")]
    public string Description { get; set; } = string.Empty;

    [Description("Target completion date")]
    public DateTime? TargetDate { get; set; }

    [Description("Category or area of life (e.g., 'Health', 'Career', 'Finance')")]
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Response after creating a milestone.
/// </summary>
public class CreateMilestoneResponse
{
    [Description("The ID of the newly created milestone")]
    public Guid MilestoneId { get; set; }

    [Description("Whether the creation was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;
}

#endregion

#region Update Milestone

/// <summary>
/// Request to update an existing milestone.
/// </summary>
public class UpdateMilestoneRequest
{
    [Description("The unique identifier of the milestone to update")]
    public Guid MilestoneId { get; set; }

    [Description("New milestone title")]
    public string Title { get; set; } = string.Empty;

    [Description("New milestone description")]
    public string Description { get; set; } = string.Empty;

    [Description("New target date")]
    public DateTime? TargetDate { get; set; }

    [Description("New progress percentage (0-100)")]
    public decimal ProgressPercent { get; set; }

    [Description("Category or area of life")]
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Response after updating a milestone.
/// </summary>
public class UpdateMilestoneResponse
{
    [Description("Whether the update was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("The updated milestone summary")]
    public MilestoneSummary UpdatedMilestone { get; set; } = new();
}

#endregion

#region Delete Milestone

/// <summary>
/// Request to delete a milestone.
/// </summary>
public class DeleteMilestoneRequest
{
    [Description("The unique identifier of the milestone to delete")]
    public Guid MilestoneId { get; set; }
}

/// <summary>
/// Response after deleting a milestone.
/// </summary>
public class DeleteMilestoneResponse
{
    [Description("Whether the deletion was successful")]
    public bool Success { get; set; }

    [Description("Message describing the result")]
    public string Message { get; set; } = string.Empty;

    [Description("ID of the deleted milestone")]
    public Guid DeletedMilestoneId { get; set; }
}

#endregion

#region Complete Milestone

/// <summary>
/// Request to mark a milestone as complete.
/// </summary>
public class CompleteMilestoneRequest
{
    [Description("The unique identifier of the milestone to complete")]
    public Guid MilestoneId { get; set; }
}

#endregion
