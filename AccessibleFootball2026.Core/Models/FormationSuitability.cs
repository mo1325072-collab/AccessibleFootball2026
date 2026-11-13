namespace AccessibleFootball2026.Core.Models;

/// <summary>Report actual current assignments; do not invent a hidden team-quality percentage.</summary>
public static class FormationSuitability
{
    public static string Describe(Team team)
    {
        var active = team.Players.Where(p => p.IsActiveOnPitch).ToArray();
        int primary = active.Count(p => p.CurrentAssignedPosition == p.PrimaryPosition);
        int secondary = active.Count(p => p.CurrentAssignedPosition != p.PrimaryPosition && p.SecondaryPositions.Contains(p.CurrentAssignedPosition));
        var outside = active.Where(p => p.CurrentAssignedPosition != p.PrimaryPosition && !p.SecondaryPositions.Contains(p.CurrentAssignedPosition)).ToArray();
        string result = LocalizationService.Text($"مراكز أساسية: {primary}، ثانوية: {secondary}، خارج المركز: {outside.Length}.", $"Primary positions: {primary}, secondary: {secondary}, out of position: {outside.Length}.");
        if (outside.Length > 0) result += " " + string.Join(LocalizationService.Text("، ", ", "), outside.Select(p => $"{p.Name}: {p.CurrentAssignedPosition}"));
        return result;
    }
    public static string Requirements(FormationType formation, UiLanguage? language = null)
    {
        bool english = (language ?? MatchSettings.Current.Language) == UiLanguage.English;
        if (!english) return FormationManager.GetRequirement(formation).RequirementDescription;
        return formation switch
        {
            FormationType.Formation433_Attack => "Holding midfielder, linking eight and advanced playmaker; wingers support one striker. Full-backs need cover when advancing.",
            FormationType.Formation433_Defend => "One holding midfielder screens the defence; two eights link play, two wingers stretch the back line, one striker holds or threatens behind.",
            FormationType.Formation4231_Wide => "A covering double pivot, a number ten between the lines, two wide attackers and one supported centre-forward.",
            FormationType.Formation442_Flat => "Two central midfielders and two hard-working wide midfielders support a strike pair. Avoid leaving central midfield outnumbered.",
            FormationType.Formation41212_Diamond => "A holding midfielder, two mobile eights and a number ten support two strikers. Full-backs provide width; protect wide transitions.",
            FormationType.Formation352_Wings => "Three centre-backs, fit wing-backs working both ways, three central midfielders and two strikers. Wide centre-backs cover behind wing-backs.",
            FormationType.Formation343_AllOut => "Three centre-backs, two covering midfielders, two fit wing-backs and three forwards. Wing-backs recover into the back line.",
            FormationType.Formation532_Counter => "Low-block preset: three centre-backs, two recovering wing-backs, a screening midfield three and two counter-attacking outlets.",
            FormationType.Formation541_Bus => "Low-block preset: a back five with a midfield four. The lone striker needs support after regaining possession.",
            FormationType.Formation4141 => "A holding midfielder screens behind four midfielders. Wide midfielders support full-backs without the ball and advance around the lone striker with it.",
            FormationType.Formation3421 => "Three centre-backs, two covering midfielders, fit wing-backs for width and two number tens behind a lone striker.",
            FormationType.Formation4222 => "A covering double pivot, two number tens and two strikers. Full-backs supply width; the central pair cover their forward runs.",
            _ => "Unknown formation requirements."
        };
    }
}
