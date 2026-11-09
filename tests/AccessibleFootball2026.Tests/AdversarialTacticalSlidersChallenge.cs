using System;
using System.IO;
using System.Linq;
using System.Numerics;
using AccessibleFootball2026.Core;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Simulation;
using AccessibleFootball2026.Input;

namespace AccessibleFootball2026.Tests;

public static class AdversarialTacticalSlidersChallenge
{
    public static void RunAll(Action<string, Action> runner)
    {
        Console.WriteLine("\n--- EMPIRICAL CHALLENGER (TACTICAL SLIDERS & DUEL ALGORITHM) ---");

        runner("19A. Zero-Dice-Roll: 100% Elimination of flat chance switch from MatchEngine", TestZeroDiceRoll_NoFlatProbabilityInSimulation);
        runner("19B. Attribute & Tactical Duel: Elite carrier shielding ball retains possession against weak tackler", TestDuel_EliteCarrierShielding_RetainsPossession);
        runner("19C. Attribute & Tactical Duel: Dominant defender face-on dispossesses exhausted carrier", TestDuel_EliteDefenderFaceOn_WinsTackle);
        runner("19D. Age Fatigue Curve: Under-19 player unaffected by 85% high press across 45 mins", TestAgeCurve_Under19_UnaffectedByHighPress);
        runner("19E. Age Fatigue Curve: 19-24 youth player withstands press until final 10 minutes", TestAgeCurve_Youth22_WithstandsUntilLast10Minutes);
        runner("19F. Age Fatigue Curve: 24-29 prime player burns out around minute 60 under 85% press", TestAgeCurve_Prime27_BurnsOutAtMinute60);
        runner("19G. Age Fatigue Curve: 30+ veteran burns out by end of 1st half under 85% press", TestAgeCurve_Veteran35_BurnsOutByHalfTime);
        runner("19H. Granular Slider: Low Pressing (0-25) conserves veteran stamina (0.70x decay)", TestGranularSlider_LowPressConservesVeteranStamina);
        runner("19I. Tactical Synergy: Detects contradictions (LongBall + ShortPassing) and penalizes advantage", TestTacticalSynergy_DetectsContradictionsAndPenalizes);
        runner("19J. Tactical Synergy: Rewards harmonious counter-attack setup (LowBlock + Direct + FastTempo)", TestTacticalSynergy_RewardsHarmoniousCounterAttackSetup);
        runner("19K. Tackling Commitment: Reckless elevates foul risk to 55%+ and triggers severe injury risk", TestTacklingCommitment_RecklessVsCautious);
        runner("19L. Offside Trap: Coordinated step-up catches attacking through ball runner offside", TestOffsideTrap_SuccessfulTrapCatchesRunner);
        runner("19M. Half-Time Rest: Restores +10 stamina points for all active players across both teams", TestHalfTimeRest_RestoresStaminaPoints);
    }

    private static void TestZeroDiceRoll_NoFlatProbabilityInSimulation()
    {
        string matchEnginePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "AccessibleFootball2026.Core", "Simulation", "MatchEngine.cs");
        if (!File.Exists(matchEnginePath))
        {
            matchEnginePath = Path.GetFullPath("AccessibleFootball2026.Core/Simulation/MatchEngine.cs");
        }

        if (File.Exists(matchEnginePath))
        {
            string code = File.ReadAllText(matchEnginePath);
            Assert(!code.Contains("int aiTackleChance = diff switch"), "MatchEngine must NEVER contain the old flat random switch 'aiTackleChance = diff switch'");
            Assert(!code.Contains("new Random().Next(100) < aiTackleChance"), "MatchEngine must NEVER contain flat random dice roll 'Next(100) < aiTackleChance'");
        }
    }

    private static void TestDuel_EliteCarrierShielding_RetainsPossession()
    {
        var carrier = new Player(1, "محمد صلاح", 11, SpecificPosition.RW, spd: 93, pas: 88, sho: 90, def: 45, phy: 84)
        {
            Position2D = new Vector2(50.0f, 30.0f),
            FacingDirection = new Vector2(1.0f, 0.0f),
            CurrentStamina = 95.0f
        };

        var weakTackler = new Player(2, "مدافع ضعيف", 4, SpecificPosition.CB, spd: 60, pas: 55, sho: 40, def: 60, phy: 58)
        {
            Position2D = new Vector2(48.8f, 30.0f),
            FacingDirection = new Vector2(1.0f, 0.0f),
            CurrentStamina = 35.0f // مرهق
        };

        var carrierTeam = new Team("H", "Home", TeamSide.Home);
        carrierTeam.Players.Add(carrier);
        var tacklerTeam = new Team("A", "Away", TeamSide.Away);
        tacklerTeam.Players.Add(weakTackler);

        Vector2 ballPos = new Vector2(50.35f, 30.0f); // الكرة أمام صلاح المحمي بظهره

        var duel = MatchEngine.EvaluateTackleDuel(
            tackler: weakTackler,
            carrier: carrier,
            ballPos2D: ballPos,
            tacklerTeam: tacklerTeam,
            carrierTeam: carrierTeam,
            diff: DifficultyLevel.Normal,
            forcedRandomRoll: 50 // فحص النتيجة الحتمية
        );

        Assert(!duel.TacklerWon, "Weak exhausted tackler behind shielding elite carrier must NOT win possession");
        Assert(duel.TacklerWinProbability < 20.0f, $"Win probability for weak tackler against shielding Salah must be < 20%. Got: {duel.TacklerWinProbability:F1}%");
        Assert(duel.TacticalReason.Contains("حجز جسدي"), $"Tactical reason must acknowledge body shielding. Got: '{duel.TacticalReason}'");
    }

    private static void TestDuel_EliteDefenderFaceOn_WinsTackle()
    {
        var eliteDefender = new Player(1, "فان دايك", 4, SpecificPosition.CB, spd: 82, pas: 75, sho: 60, def: 94, phy: 92)
        {
            Position2D = new Vector2(50.5f, 30.0f),
            FacingDirection = new Vector2(-1.0f, 0.0f), // مواجهة وجه لوجه
            CurrentStamina = 95.0f
        };

        var exhaustedAttacker = new Player(2, "مهاجم مرهق", 9, SpecificPosition.ST, spd: 65, pas: 60, sho: 70, def: 35, phy: 62)
        {
            Position2D = new Vector2(50.0f, 30.0f),
            FacingDirection = new Vector2(1.0f, 0.0f), // وشه في وش فان دايك
            CurrentStamina = 30.0f // مرهق جداً
        };

        var tacklerTeam = new Team("A", "Liverpool", TeamSide.Away);
        tacklerTeam.Players.Add(eliteDefender);
        var carrierTeam = new Team("H", "Home", TeamSide.Home);
        carrierTeam.Players.Add(exhaustedAttacker);

        Vector2 ballPos = new Vector2(50.2f, 30.0f);

        var duel = MatchEngine.EvaluateTackleDuel(
            tackler: eliteDefender,
            carrier: exhaustedAttacker,
            ballPos2D: ballPos,
            tacklerTeam: tacklerTeam,
            carrierTeam: carrierTeam,
            diff: DifficultyLevel.Normal,
            forcedRandomRoll: 50
        );

        Assert(duel.TacklerWon, "Elite Van Dijk face-on against exhausted weak forward must win tackle decisively");
        Assert(duel.TacklerWinProbability > 75.0f, $"Win probability for Van Dijk face-on must exceed 75%. Got: {duel.TacklerWinProbability:F1}%");
        Assert(duel.TacticalReason.Contains("مواجهة رأس برأس"), $"Reason must recognize head-on face-to-face duel. Got: '{duel.TacticalReason}'");
    }

    private static void TestAgeCurve_Under19_UnaffectedByHighPress()
    {
        var youngPlayer = new Player(1, "لامين يامال", 19, SpecificPosition.RW, spd: 88, pas: 85, sho: 82, def: 40, phy: 70, age: 17);
        var tactics = new TeamTactics { PressingIntensity = 85 };

        float mult = tactics.GetStaminaDecayMultiplier(youngPlayer);
        Assert(Math.Abs(mult - 1.00f) < 0.01f, $"Under-19 player multiplier under 85% press must be exactly 1.00x (unaffected). Got: {mult:F2}x");

        var teamH = TeamDatabase.CreateAlAhly();
        teamH.Players[0] = youngPlayer;
        teamH.Tactics = tactics;
        var teamA = TeamDatabase.CreateZamalek();

        var match = new MatchEngine(teamH, teamA, new MatchEventBus(), new KeyboardInputHandler());
        match.UpdateStaminaDecay(simulatedMinutesPassed: 45.0f); // نهاية الشوط الأول

        Assert(youngPlayer.CurrentStamina > 80.0f, $"Under-19 stamina must remain fresh (> 80) after 45 mins of high press. Got: {youngPlayer.CurrentStamina:F1}");
    }

    private static void TestAgeCurve_Youth22_WithstandsUntilLast10Minutes()
    {
        var youthPlayer = new Player(1, "كامافينجا", 12, SpecificPosition.CM, spd: 84, pas: 82, sho: 75, def: 80, phy: 82, age: 22);
        var tactics = new TeamTactics { PressingIntensity = 85 };

        float mult = tactics.GetStaminaDecayMultiplier(youthPlayer);
        Assert(mult >= 1.05f && mult <= 1.20f, $"Youth 19-24 multiplier under 85% press must be between 1.05x and 1.20x. Got: {mult:F2}x");

        var teamH = TeamDatabase.CreateAlAhly();
        teamH.Players[0] = youthPlayer;
        teamH.Tactics = tactics;

        var match = new MatchEngine(teamH, TeamDatabase.CreateZamalek(), new MatchEventBus(), new KeyboardInputHandler());
        
        // عند الدقيقة 45
        match.UpdateStaminaDecay(simulatedMinutesPassed: 45.0f);
        Assert(youthPlayer.CurrentStamina > 70.0f, $"Youth 22 stamina must be > 70 at half-time. Got: {youthPlayer.CurrentStamina:F1}");

        // استراحة بين الشوطين
        match.TriggerHalfTime();

        // عند الدقيقة 75 (قبل آخر 10 دقائق)
        match.UpdateStaminaDecay(simulatedMinutesPassed: 30.0f);
        Assert(youthPlayer.CurrentStamina > 55.0f, $"Youth 22 stamina must withstand until min 80 (> 55 at min 75). Got: {youthPlayer.CurrentStamina:F1}");
    }

    private static void TestAgeCurve_Prime27_BurnsOutAtMinute60()
    {
        var primePlayer = new Player(1, "برونو فرنانديز", 8, SpecificPosition.CAM, spd: 78, pas: 89, sho: 84, def: 65, phy: 75, age: 27);
        var tactics = new TeamTactics { PressingIntensity = 85 };

        float mult = tactics.GetStaminaDecayMultiplier(primePlayer);
        Assert(mult >= 1.25f && mult <= 1.55f, $"Prime 24-29 multiplier under 85% press must be between 1.25x and 1.55x. Got: {mult:F2}x");

        var teamH = TeamDatabase.CreateAlAhly();
        teamH.Players[0] = primePlayer;
        teamH.Tactics = tactics;

        var match = new MatchEngine(teamH, TeamDatabase.CreateZamalek(), new MatchEventBus(), new KeyboardInputHandler());
        match.UpdateStaminaDecay(simulatedMinutesPassed: 60.0f);

        Assert(primePlayer.CurrentStamina < 60.0f, $"Prime 27 player stamina must burn down by minute 60 (< 60). Got: {primePlayer.CurrentStamina:F1}");
    }

    private static void TestAgeCurve_Veteran35_BurnsOutByHalfTime()
    {
        var veteranPlayer = new Player(1, "لوكا مودريتش", 10, SpecificPosition.CM, spd: 72, pas: 91, sho: 78, def: 72, phy: 66, age: 36);
        var tactics = new TeamTactics { PressingIntensity = 90 }; // ضغط خانق شديد

        float mult = tactics.GetStaminaDecayMultiplier(veteranPlayer);
        Assert(mult >= 1.80f, $"Veteran 30+ multiplier under 90% press must exceed 1.80x. Got: {mult:F2}x");

        var teamH = TeamDatabase.CreateAlAhly();
        teamH.Players[0] = veteranPlayer;
        teamH.Tactics = tactics;

        var match = new MatchEngine(teamH, TeamDatabase.CreateZamalek(), new MatchEventBus(), new KeyboardInputHandler());
        
        // محاكاة الشوط الأول (45 دقيقة) تحت ضغط خانق
        match.UpdateStaminaDecay(simulatedMinutesPassed: 45.0f);

        Assert(veteranPlayer.CurrentStamina < 55.0f, $"Veteran 35 must burn out ('يجيب جاز') by end of 1st half under 90% press. Got: {veteranPlayer.CurrentStamina:F1}");
    }

    private static void TestGranularSlider_LowPressConservesVeteranStamina()
    {
        var veteran = new Player(1, "علي معلول", 21, SpecificPosition.LB, spd: 76, pas: 84, sho: 74, def: 72, phy: 70, age: 35);
        var lowTactics = new TeamTactics { PressingIntensity = 20 }; // دفاع متراجع هادئ

        float mult = lowTactics.GetStaminaDecayMultiplier(veteran);
        Assert(mult <= 0.75f, $"Under Low Press (0-25), veteran multiplier must be 0.70x (conserving energy). Got: {mult:F2}x");

        var teamH = TeamDatabase.CreateAlAhly();
        teamH.Players[0] = veteran;
        teamH.Tactics = lowTactics;

        var match = new MatchEngine(teamH, TeamDatabase.CreateZamalek(), new MatchEventBus(), new KeyboardInputHandler());
        match.UpdateStaminaDecay(simulatedMinutesPassed: 45.0f);

        Assert(veteran.CurrentStamina > 85.0f, $"Low press must conserve veteran stamina (> 85) across first half. Got: {veteran.CurrentStamina:F1}");
    }

    private static void TestTacticalSynergy_DetectsContradictionsAndPenalizes()
    {
        var tactics = new TeamTactics
        {
            Focus = AttackingFocus.LongBall,
            PassingDirectness = 20 // تناقض فج: كرات طويلة مع شريط باص قصير من الـ 18
        };

        var synergy = tactics.CalculateSynergy();
        Assert(synergy.SynergyScore <= 75, $"Contradictory tactics must lower synergy score <= 75. Got: {synergy.SynergyScore}");
        Assert(synergy.AdvantageMultiplier < 1.0f, $"Advantage multiplier must be < 1.0 (tactical penalty). Got: {synergy.AdvantageMultiplier:F2}");
        Assert(synergy.Conflicts.Any(c => c.Contains("الكرات الطويلة")), "Conflicts list must explicitly report LongBall contradiction");
    }

    private static void TestTacticalSynergy_RewardsHarmoniousCounterAttackSetup()
    {
        var tactics = new TeamTactics
        {
            DefensiveLineDepth = 25,          // تكتل تحت الكرة Low Block
            Focus = AttackingFocus.CounterAttack,// هجمات مرتدة
            Tempo = 75,                       // ريتم سريع ون تاتش
            PassingDirectness = 65            // باص مباشر للأمام
        };

        var synergy = tactics.CalculateSynergy();
        Assert(synergy.SynergyScore >= 90, $"Harmonious counter setup must score >= 90. Got: {synergy.SynergyScore}");
        Assert(synergy.AdvantageMultiplier >= 1.15f, $"Harmonious counter setup must yield >= 1.15x advantage. Got: {synergy.AdvantageMultiplier:F2}x");
        Assert(synergy.PositiveSynergies.Any(s => s.Contains("التحول الخاطف")), "Positive synergies must mention rapid transition advantage");
    }

    private static void TestTacklingCommitment_RecklessVsCautious()
    {
        var tackler = new Player(1, "مدافع", 4, SpecificPosition.CB, spd: 75, pas: 70, sho: 50, def: 75, phy: 75);
        var carrier = new Player(2, "مهاجم", 9, SpecificPosition.ST, spd: 75, pas: 70, sho: 75, def: 40, phy: 75);

        var recklessTeam = new Team("R", "Reckless", TeamSide.Away)
        {
            Tactics = new TeamTactics { Tackling = TacklingAggression.Reckless }
        };
        recklessTeam.Players.Add(tackler);

        var cautiousTeam = new Team("C", "Cautious", TeamSide.Away)
        {
            Tactics = new TeamTactics { Tackling = TacklingAggression.Cautious }
        };
        cautiousTeam.Players.Add(tackler);

        var homeTeam = new Team("H", "Home", TeamSide.Home);
        homeTeam.Players.Add(carrier);

        Vector2 ballPos = new Vector2(50.0f, 30.0f);
        carrier.Position2D = new Vector2(50.0f, 30.0f);
        tackler.Position2D = new Vector2(49.0f, 30.0f);

        // صراع متهور (foulRoll: 20 يؤدي لفاول ومخاطرة إصابة بالغة عند التهور)
        var recklessDuel = MatchEngine.EvaluateTackleDuel(tackler, carrier, ballPos, recklessTeam, homeTeam, DifficultyLevel.Normal, forcedRandomRoll: 85, forcedFoulRoll: 20);
        // صراع حذر (foulRoll: 20 أعلى من نسبة فاول الحذر 2%، فلا يرتكب خطأ ولا يسبب إصابة)
        var cautiousDuel = MatchEngine.EvaluateTackleDuel(tackler, carrier, ballPos, cautiousTeam, homeTeam, DifficultyLevel.Normal, forcedRandomRoll: 85, forcedFoulRoll: 20);

        Assert(recklessDuel.TacklerWinProbability > cautiousDuel.TacklerWinProbability, 
            $"Reckless tackle probability ({recklessDuel.TacklerWinProbability:F1}%) must exceed Cautious ({cautiousDuel.TacklerWinProbability:F1}%)");
        Assert(recklessDuel.IsSevereInjuryRisk, "Reckless tackling must carry severe injury risk on failed/reckless interventions");
        Assert(!cautiousDuel.IsSevereInjuryRisk, "Cautious tackling must NEVER carry severe injury risk");
    }

    private static void TestOffsideTrap_SuccessfulTrapCatchesRunner()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        awayTeam.Tactics.OffsideTrap = true; // تفعيل مصيدة التسلل

        var eventBus = new MatchEventBus();
        bool offsideFired = false;
        eventBus.OnMatchEvent += evt =>
        {
            if (evt.Type == MatchEventType.Offside)
            {
                offsideFired = true;
            }
        };

        var match = new MatchEngine(homeTeam, awayTeam, eventBus, new KeyboardInputHandler());
        var receiver = homeTeam.Players[9];

        // وضع خط دفاع الخصم عند X = 65.0m مع وعي تكتيكي ودفاع كامل 100%
        foreach (var def in awayTeam.Players.Where(p => p.Role == PlayerRole.Defender))
        {
            def.Position2D = new Vector2(65.0f, def.Position2D.Y);
            def.BaseDefending = 100;
            def.CurrentStamina = 100.0f; // غير مرهقين، تركيز وتناغم كامل
        }

        // تمرير بينية لمستلم متقدم خلف خط الدفاع عند X = 70.0m
        Vector2 targetPos = new Vector2(70.0f, 34.0f);

        bool isOffside = match.CheckOffsideTrap(homeTeam, awayTeam, receiver, targetPos);

        Assert(isOffside, "Coordinated offside trap must catch runner beyond the last defender line");
        Assert(offsideFired, "MatchEventType.Offside must be published on successful offside trap");
    }

    private static void TestHalfTimeRest_RestoresStaminaPoints()
    {
        var homeTeam = TeamDatabase.CreateAlAhly();
        var awayTeam = TeamDatabase.CreateZamalek();
        var match = new MatchEngine(homeTeam, awayTeam, new MatchEventBus(), new KeyboardInputHandler());

        var player = homeTeam.Players[0];
        player.CurrentStamina = 60.0f; // استنزف 40 نقطة خلال الشوط الأول

        match.TriggerHalfTime();

        Assert(player.CurrentStamina >= 70.0f, $"Half-time rest must replenish stamina (+10 points). Expected >= 70, got: {player.CurrentStamina:F1}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"[ASSERTION FAILED] {message}");
        }
    }
}
