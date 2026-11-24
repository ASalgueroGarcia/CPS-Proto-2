using System.Globalization;

/// <summary>
/// The wording of a save slot card, shared by every slot view (the Canvas cards today, an in-world
/// version later) so they all say the same thing. Dates are culture-independent: a Spanish Windows
/// must not turn "15 Oct" into "15 oct.".
/// </summary>
public static class SlotCardText
{
    private static readonly string[] ActNumerals = { "I", "II", "III" };

    public static string Title(SlotSummary summary) => $"SAVE {summary.Slot + 1}";

    public static string ActNumeral(int act) => act >= 0 && act < ActNumerals.Length ? ActNumerals[act] : "?";

    public static string Details(SlotSummary summary)
    {
        if (!summary.Exists) return "NEW GAME";
        if (summary.Locked) return "CAN'T OPEN THIS SAVE\nClose other programs and try again";
        if (summary.Unreadable && summary.LastSaved == null) return "DAMAGED SAVE\nPlaying it starts over";

        string text = $"ACT {ActNumeral(summary.CurrentAct)} · SCENE {summary.CurrentScene + 1}\n" +
                      $"{summary.Tickets} Tickets · {summary.BossTickets} Boss Tickets";
        if (summary.LastSaved.HasValue)
        {
            text += "\n" + summary.LastSaved.Value.ToString("dd MMM yyyy  HH:mm", CultureInfo.InvariantCulture);
        }

        return summary.Unreadable ? text + "\nREAD-ONLY" : text;
    }
}
