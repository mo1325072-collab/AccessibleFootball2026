using System.Numerics;

namespace AccessibleFootball2026.Core.Models;

public enum TeamSide
{
    Home,
    Away
}

public class Team
{
    public string Id { get; set; }
    public string Name { get; set; }
    public TeamSide Side { get; set; }
    public bool? MatchAttackingRight { get; set; }
    public bool IsNationalTeam { get; set; } = false; // تمييز المنتخبات عن الأندية للسلام الوطني ومنع المنتخبات في وضع الـ 4 فرق
    public List<Player> Players { get; set; } = new(); // التشكيل الأساسي الـ 11
    public List<Player> Bench { get; set; } = new();   // دكة البدلاء
    public int Score { get; set; } = 0;
    public FormationType CurrentFormation { get; set; } = FormationType.Formation433_Attack;
    public TeamTactics Tactics { get; set; } = new();

    // قانون IFAB 3: التبديلات وفترات التوقف (5 تبديلات في 3 فترات كحد أقصى)
    public int SubstitutionsUsed { get; set; } = 0;
    public int StoppageWindowsUsed { get; set; } = 0;
    public bool IsExtraTime { get; set; } = false;
    public int MaxSubstitutions => IsExtraTime ? 6 : 5;
    public int MaxStoppageWindows => IsExtraTime ? 4 : 3;
    public int RemainingSubstitutions => Math.Max(0, MaxSubstitutions - SubstitutionsUsed);
    public int RemainingStoppageWindows => Math.Max(0, MaxStoppageWindows - StoppageWindowsUsed);
    public bool WindowConsumedThisStoppage { get; set; } = false;

    private readonly HashSet<Player> _substitutedOff = new();
    public bool CanEnterAsSubstitute(Player player) => Bench.Contains(player) && !player.IsSentOff &&
        !player.IsOffPitch && player.InjuryStatus != PlayerInjuryStatus.SevereInjury && !_substitutedOff.Contains(player);

    public bool CanMakeSubstitution(bool isHalfTime = false)
    {
        if (SubstitutionsUsed >= MaxSubstitutions)
            return false;

        // تبديلات استراحة بين الشوطين لا تستهلك أي فترة توقف إطلاقاً
        if (isHalfTime)
            return true;

        // إذا تم استهلاك فترة توقف في هذا التوقف الحالي، فالتبديلات الإضافية في نفس التوقف مسموحة
        if (WindowConsumedThisStoppage)
            return true;

        // يلزم توفر فترة توقف متبقية
        return StoppageWindowsUsed < MaxStoppageWindows;
    }

    public void ResetStoppageWindowFlag()
    {
        WindowConsumedThisStoppage = false;
    }

    // أدوار اللاعبين التكتيكية (Player Roles)
    public Player? Captain { get; set; }
    public Player? PenaltyTaker { get; set; }
    public Player? FreeKickTaker { get; set; }
    public Player? CornerTaker { get; set; }

    public Team(string id, string name, TeamSide side)
    {
        Id = id;
        Name = name;
        Side = side;
    }

    public Team Clone(TeamSide? newSide = null)
    {
        var copy = new Team(Id, Name, newSide ?? Side)
        {
            IsNationalTeam = IsNationalTeam,
            CurrentFormation = CurrentFormation,
            Tactics = Tactics.Clone(),
            Score = 0,
            SubstitutionsUsed = 0,
            StoppageWindowsUsed = 0,
            IsExtraTime = false,
            WindowConsumedThisStoppage = false
        };
        foreach (var p in Players) copy.Players.Add(p.Clone());
        foreach (var b in Bench) copy.Bench.Add(b.Clone());

        if (Captain != null) copy.Captain = copy.Players.FirstOrDefault(p => p.Id == Captain.Id) ?? copy.Bench.FirstOrDefault(p => p.Id == Captain.Id);
        if (PenaltyTaker != null) copy.PenaltyTaker = copy.Players.FirstOrDefault(p => p.Id == PenaltyTaker.Id) ?? copy.Bench.FirstOrDefault(p => p.Id == PenaltyTaker.Id);
        if (FreeKickTaker != null) copy.FreeKickTaker = copy.Players.FirstOrDefault(p => p.Id == FreeKickTaker.Id) ?? copy.Bench.FirstOrDefault(p => p.Id == FreeKickTaker.Id);
        if (CornerTaker != null) copy.CornerTaker = copy.Players.FirstOrDefault(p => p.Id == CornerTaker.Id) ?? copy.Bench.FirstOrDefault(p => p.Id == CornerTaker.Id);

        return copy;
    }

    public string? GetMatchStartIssue()
    {
        int active = Players.Count(p => p.IsActiveOnPitch);
        if (active < 7) return LocalizationService.Text($"لا يمكن بدء مباراة {Name}: التشكيلة تحتوي على {active} لاعبين متاحين فقط، والحد الأدنى سبعة. بيانات الفريق تحتاج إلى استكمال.", $"Cannot start {Name}: only {active} active players, minimum seven. Source roster needs completion.");
        int keepers = Players.Count(p => p.IsActiveOnPitch && p.Role == PlayerRole.Goalkeeper);
        if (keepers != 1) return LocalizationService.Text($"لا يمكن بدء مباراة {Name}: يجب تعيين حارس مرمى واحد في التشكيلة الأساسية. راجع بيانات الفريق أو خطة اللعب.", $"Cannot start {Name}: exactly one starting goalkeeper is required. Check roster or formation.");
        return null;
    }

    public void SetupFormation433()
    {
        CurrentFormation = FormationType.Formation433_Attack;
        FormationManager.ApplyFormation(this, FormationType.Formation433_Attack);
        AssignDefaultRoles();
    }

    public void ChangeFormation(FormationType newFormation)
    {
        CurrentFormation = newFormation;
        FormationManager.ApplyFormation(this, newFormation, assignPositions: true);
    }

    public void AssignDefaultRoles()
    {
        if (Players.Count > 0)
        {
            Captain ??= Players.FirstOrDefault(p => p.Role == PlayerRole.Defender) ?? Players[0];
            PenaltyTaker ??= Players.FirstOrDefault(p => p.Role == PlayerRole.Forward) ?? Players[0];
            FreeKickTaker ??= Players.FirstOrDefault(p => p.Role == PlayerRole.Midfielder) ?? Players[0];
            CornerTaker ??= Players.FirstOrDefault(p => p.Role == PlayerRole.Midfielder) ?? Players[0];
        }
    }

    /// <summary>
    /// تبديل المراكز بين لاعبين في التشكيلة الأساسية (مثل نقل الجناح الأيمن للأيسر)
    /// </summary>
    public bool SwapPositions(int index1, int index2)
    {
        if (index1 >= 0 && index1 < Players.Count && index2 >= 0 && index2 < Players.Count && index1 != index2)
        {
            var p1 = Players[index1];
            var p2 = Players[index2];

            // IFAB Law 3 & 12: منع تبديل مراكز اللاعبين المطرودين أو الخارجين للإصابة
            if (p1.IsSentOff || p1.IsOffPitch || p2.IsSentOff || p2.IsOffPitch)
            {
                return false;
            }

            // تبديل التمركز الميداني وتعيين المركز
            var tempPos = p1.CurrentAssignedPosition;
            p1.CurrentAssignedPosition = p2.CurrentAssignedPosition;
            p2.CurrentAssignedPosition = tempPos;

            var tempCoord = p1.HomePosition;
            p1.HomePosition = p2.HomePosition;
            p2.HomePosition = tempCoord;

            p1.Position2D = p1.HomePosition;
            p2.Position2D = p2.HomePosition;

            // تبديل مكانهما في مصفوفة اللاعبين
            Players[index1] = p2;
            Players[index2] = p1;

            return true;
        }
        return false;
    }

    /// <summary>
    /// إجراء تبديل تكتيكي بإخراج لاعب أساسي وإدخال لاعب من الدكة وفقاً لقانون IFAB 3
    /// </summary>
    public bool MakeSubstitution(Player playerOut, Player playerIn, bool isHalfTime = false, bool isMatchActive = true)
    {
        if (playerOut == null || playerIn == null)
        {
            return false;
        }

        // IFAB Law 3 & 12: اللاعب المطرود لا يمكن استبداله
        if (playerOut.IsSentOff || playerIn.IsSentOff)
        {
            return false;
        }

        if (isMatchActive && (!CanMakeSubstitution(isHalfTime) || !CanEnterAsSubstitute(playerIn)))
        {
            return false;
        }

        int outIndex = Players.IndexOf(playerOut);
        int inIndex = Bench.IndexOf(playerIn);

        if (outIndex != -1 && inIndex != -1)
        {
            // نقل موضع اللاعب وتمركزه للاعب الجديد
            playerIn.CurrentAssignedPosition = playerOut.CurrentAssignedPosition;
            playerIn.Position2D = playerOut.Position2D;
            playerIn.HomePosition = playerOut.HomePosition;
            playerIn.HasBall = playerOut.HasBall;
            playerIn.FacingDirection = playerOut.FacingDirection;
            playerIn.Velocity = playerOut.Velocity;
            playerOut.HasBall = false;
            if (isMatchActive) _substitutedOff.Add(playerOut);

            Players[outIndex] = playerIn;
            Bench[inIndex] = playerOut;

            if (Captain == playerOut) Captain = playerIn;
            if (PenaltyTaker == playerOut) PenaltyTaker = playerIn;
            if (FreeKickTaker == playerOut) FreeKickTaker = playerIn;
            if (CornerTaker == playerOut) CornerTaker = playerIn;

            if (isMatchActive)
            {
                SubstitutionsUsed++;
                if (!isHalfTime && !WindowConsumedThisStoppage)
                {
                    StoppageWindowsUsed++;
                    WindowConsumedThisStoppage = true;
                }
            }

            return true;
        }

        return false;
    }
}
