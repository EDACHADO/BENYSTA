namespace Integration.BusinessLogics.Nps.Models;

/// <summary>Outcome of fetching the NPS participants list.</summary>
/// <param name="Success">Whether the switch answered the lookup.</param>
/// <param name="Json">The raw participants payload as returned by the switch, when successful.</param>
/// <param name="Error">Why the lookup failed, when <paramref name="Success"/> is false.</param>
public sealed record NpsParticipantsResult(
    bool Success,
    string? Json,
    string? Error);
