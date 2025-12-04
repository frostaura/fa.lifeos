namespace LifeOS.Domain.Enums;

/// <summary>
/// Defines when a recurring expense should stop being applied
/// </summary>
public enum EndConditionType
{
    /// <summary>
    /// No end condition - expense continues indefinitely
    /// </summary>
    None,
    
    /// <summary>
    /// Stop when a specific account balance reaches zero (e.g., loan paid off)
    /// </summary>
    UntilAccountSettled,
    
    /// <summary>
    /// Stop after a specific date
    /// </summary>
    UntilDate,
    
    /// <summary>
    /// Stop after a cumulative amount has been paid
    /// </summary>
    UntilAmount
}
