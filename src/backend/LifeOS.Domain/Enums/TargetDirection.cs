namespace LifeOS.Domain.Enums;

public enum TargetDirection
{
    AtOrAbove,  // >= target is good (e.g., steps, sleep hours)
    AtOrBelow,  // <= target is good (e.g., weight loss, resting heart rate)
    Range       // within min-max range is optimal (e.g., body fat 13-15%)
}
