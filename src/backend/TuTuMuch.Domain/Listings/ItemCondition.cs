namespace TuTuMuch.Domain.Listings;

/// <summary>The five conditions of L2-016.</summary>
public enum ItemCondition
{
    NewWithTags,
    NewWithoutTags,
    UsedExcellent,
    UsedGood,
    UsedFair,
}

/// <summary>Display strings of <see cref="ItemCondition"/>, which are also the API values.</summary>
public static class ItemConditions
{
    private static readonly (ItemCondition Value, string Label)[] Labels =
    [
        (ItemCondition.NewWithTags, "New with tags"),
        (ItemCondition.NewWithoutTags, "New without tags"),
        (ItemCondition.UsedExcellent, "Used - Excellent"),
        (ItemCondition.UsedGood, "Used - Good"),
        (ItemCondition.UsedFair, "Used - Fair"),
    ];

    public static IReadOnlyList<string> All { get; } = Labels.Select(l => l.Label).ToArray();

    public static string ToLabel(this ItemCondition condition) => Labels.Single(l => l.Value == condition).Label;

    public static ItemCondition? FromLabel(string? label) =>
        Labels.Where(l => l.Label == label).Select(l => (ItemCondition?)l.Value).FirstOrDefault();
}
