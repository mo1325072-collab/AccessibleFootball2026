using System.Numerics;

namespace AccessibleFootball2026.Core.Models;

public enum PlayerRole
{
    Goalkeeper,
    Defender,
    Midfielder,
    Forward
}

public enum SpecificPosition
{
    GK,
    CB,
    LB,
    RB,
    LWB,
    RWB,
    CDM,
    CM,
    CAM,
    LM,
    RM,
    LW,
    RW,
    ST,
    CF
}

public enum PlayerInjuryStatus
{
    None,
    MinorKnock,
    SevereInjury
}

public enum PlayerCondition
{
    Excellent, // سهم أزرق لأعلى (+10% طاقة)
    Good,      // سهم أخضر مائل للأعلى (+5% طاقة)
    Normal,    // سهم أصفر أفقي (طاقة طبيعية 100%)
    Poor,      // سهم برتقالي مائل للأسفل (-5% طاقة)
    Terrible   // سهم أحمر للأسفل (-15% طاقة)
}

public enum PreferredFoot
{
    Right,
    Left,
    Both
}

[Flags]
public enum PlayerTacticalRole
{
    None = 0,
    Captain = 1,
    PenaltyTaker = 2,
    FreeKickTaker = 4,
    CornerTaker = 8
}


/// <summary>
/// نموذج اللاعب ببيانات وطاقات صيف 2026 الواقعية
/// متضمنة أسهم الحالة البدنية والنفسية ومحاكاة كفاءة التمركز (Out-of-Position Penalty)
/// وتطبيق القانون 12 للإنذارات والطرد ونظام الإصابات
/// </summary>
public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ShirtNumber { get; set; }
    public int KitNumber { get => ShirtNumber; set => ShirtNumber = value; }
    public int Pace => Speed;
    public int BasePace { get => BaseSpeed; set => BaseSpeed = value; }
    public int OverallRating => EffectiveOverallRating;

    // ربط ملفات نداءات اللاعبين الصوتية من PES 2013 CRI ADX
    public int CallnameId { get; set; } = 0;
    public string? CallnameKey { get; set; }

    // الأدوار التكتيكية
    public PlayerTacticalRole TacticalRole { get; set; } = PlayerTacticalRole.None;
    public bool IsCaptain
    {
        get => TacticalRole.HasFlag(PlayerTacticalRole.Captain);
        set
        {
            if (value) TacticalRole |= PlayerTacticalRole.Captain;
            else TacticalRole &= ~PlayerTacticalRole.Captain;
        }
    }
    public bool IsPenaltyTaker
    {
        get => TacticalRole.HasFlag(PlayerTacticalRole.PenaltyTaker);
        set
        {
            if (value) TacticalRole |= PlayerTacticalRole.PenaltyTaker;
            else TacticalRole &= ~PlayerTacticalRole.PenaltyTaker;
        }
    }
    public bool IsFreeKickTaker
    {
        get => TacticalRole.HasFlag(PlayerTacticalRole.FreeKickTaker);
        set
        {
            if (value) TacticalRole |= PlayerTacticalRole.FreeKickTaker;
            else TacticalRole &= ~PlayerTacticalRole.FreeKickTaker;
        }
    }
    public bool IsCornerTaker
    {
        get => TacticalRole.HasFlag(PlayerTacticalRole.CornerTaker);
        set
        {
            if (value) TacticalRole |= PlayerTacticalRole.CornerTaker;
            else TacticalRole &= ~PlayerTacticalRole.CornerTaker;
        }
    }

    // السمات الواقعية للاعب
    public PreferredFoot PreferredFoot { get; set; } = PreferredFoot.Right;
    public int Age { get; set; } = 25;
    public int HeightCm { get; set; } = 180;
    public string ClubOrigin { get; set; } = string.Empty; // النادي الأصلي المستدعى منه في وضع الاختيار العشوائي

    
    // أهداف المباراة الحالية (لتتبع الهاتريك)
    public int MatchGoalsCount { get; set; } = 0;

    // القانون 12: البطاقات التأديبية والطرد
    public int YellowCards { get; set; } = 0;
    public bool IsSentOff { get; set; } = false;
    public bool IsOffPitch { get; set; } = false;

    // نظام الإصابات
    public PlayerInjuryStatus InjuryStatus { get; set; } = PlayerInjuryStatus.None;
    public float InjuryMultiplier => InjuryStatus switch
    {
        PlayerInjuryStatus.MinorKnock => 0.80f,
        _ => 1.0f
    };

    public bool IsActiveOnPitch => !IsSentOff && !IsOffPitch;

    // المراكز الأساسية والثانوية
    public SpecificPosition PrimaryPosition { get; set; }
    public List<SpecificPosition> SecondaryPositions { get; set; } = new();
    public SpecificPosition CurrentAssignedPosition { get; set; }
    public SpecificPosition Position
    {
        get => CurrentAssignedPosition != default ? CurrentAssignedPosition : PrimaryPosition;
        set
        {
            PrimaryPosition = value;
            CurrentAssignedPosition = value;
        }
    }

    // سهم الحالة البدنية (Condition Arrow) على طريقة بيس
    public PlayerCondition Condition { get; set; } = PlayerCondition.Normal;

    public float ConditionMultiplier => Condition switch
    {
        PlayerCondition.Excellent => 1.10f,
        PlayerCondition.Good => 1.05f,
        PlayerCondition.Normal => 1.00f,
        PlayerCondition.Poor => 0.95f,
        PlayerCondition.Terrible => 0.85f,
        _ => 1.0f
    };

    public string ConditionDescription => Condition switch
    {
        PlayerCondition.Excellent => "سهم أزرق مرتفع (حالة ممتازة +10%)",
        PlayerCondition.Good => "سهم أخضر (حالة جيدة +5%)",
        PlayerCondition.Normal => "سهم أصفر (حالة طبيعية)",
        PlayerCondition.Poor => "سهم برتقالي نازل (إرهاق خفيف -5%)",
        PlayerCondition.Terrible => "سهم أحمر ساقط (هبوط حاد في اللياقة -15%)",
        _ => "طبيعي"
    };

    public PlayerRole Role => CurrentAssignedPosition switch
    {
        SpecificPosition.GK => PlayerRole.Goalkeeper,
        SpecificPosition.CB or SpecificPosition.LB or SpecificPosition.RB or SpecificPosition.LWB or SpecificPosition.RWB => PlayerRole.Defender,
        SpecificPosition.CDM or SpecificPosition.CM or SpecificPosition.CAM or SpecificPosition.LM or SpecificPosition.RM => PlayerRole.Midfielder,
        _ => PlayerRole.Forward
    };

    // الطاقات الأساسية (Base Stats)
    public int BaseSpeed { get; set; } = 75;
    public int BasePassing { get; set; } = 75;
    public int BaseShooting { get; set; } = 75;
    public int BaseDefending { get; set; } = 75;
    public int BasePhysical { get; set; } = 75;
    public int BaseStamina { get; set; } = 100;

    // الطاقات الفعلية بعد حساب كفاءة المركز والحالة البدنية وتأثير الإصابات (20% جزاء للكدمات)
    public int Speed => (int)(BaseSpeed * PositionEfficiencyMultiplier * ConditionMultiplier * InjuryMultiplier);
    public int Passing => (int)(BasePassing * PositionEfficiencyMultiplier * ConditionMultiplier);
    public int Shooting => (int)(BaseShooting * PositionEfficiencyMultiplier * ConditionMultiplier);
    public int Defending => (int)(BaseDefending * PositionEfficiencyMultiplier * ConditionMultiplier);
    public int Physical => (int)(BasePhysical * PositionEfficiencyMultiplier * ConditionMultiplier);
    public int Stamina => (int)(BaseStamina * InjuryMultiplier);
    public int Dribbling => (int)(((BasePassing + BaseSpeed) / 2.0f) * PositionEfficiencyMultiplier * ConditionMultiplier);

    public int BaseOverallRating => (BaseSpeed + BasePassing + BaseShooting + BaseDefending + BasePhysical) / 5;
    public int EffectiveOverallRating => (Speed + Passing + Shooting + Defending + Physical) / 5;

    /// <summary>
    /// معامل كفاءة التمركز: 1.0 في مركزه، 0.92 في مركز ثانوي، 0.65 إذا كان خارج مركزه تماماً
    /// </summary>
    public float PositionEfficiencyMultiplier
    {
        get
        {
            if (CurrentAssignedPosition == PrimaryPosition) return 1.0f;
            if (SecondaryPositions.Contains(CurrentAssignedPosition)) return 0.92f;
            
            // خارج مركزه تماماً (مثلاً مدافع يلعب مهاجم أو العكس)
            return 0.65f;
        }
    }

    public string PositionSuitabilityMessage
    {
        get
        {
            if (CurrentAssignedPosition == PrimaryPosition)
                return $"في مركزه الأساسي ({CurrentAssignedPosition}) بطاقة كاملة {EffectiveOverallRating} - {ConditionDescription}";
            
            if (SecondaryPositions.Contains(CurrentAssignedPosition))
                return $"في مركز ثانوي ({CurrentAssignedPosition}) بطاقة {EffectiveOverallRating} بدل {BaseOverallRating} - {ConditionDescription}";

            return $"⚠️ خارج مركزه تماماً ({CurrentAssignedPosition})! تراجع حاد في الطاقات إلى {EffectiveOverallRating} بدل {BaseOverallRating} - {ConditionDescription}";
        }
    }

    public Vector2 Position2D { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 HomePosition { get; set; }
    public Vector2 FacingDirection { get; set; } = new(1.0f, 0.0f);
    public bool HasBall { get; set; } = false;

    public Player() { }

    public Player(
        int id, 
        string name, 
        int shirtNumber, 
        SpecificPosition pos, 
        int spd, 
        int pas, 
        int sho, 
        int def, 
        int phy, 
        List<SpecificPosition>? secondary = null, 
        string club = "",
        int callnameId = 0,
        string? callnameKey = null,
        PreferredFoot preferredFoot = PreferredFoot.Right,
        int age = 25,
        int heightCm = 180,
        PlayerTacticalRole tacticalRole = PlayerTacticalRole.None)
    {
        Id = id;
        Name = name;
        ShirtNumber = shirtNumber;
        PrimaryPosition = pos;
        CurrentAssignedPosition = pos;
        BaseSpeed = spd;
        BasePassing = pas;
        BaseShooting = sho;
        BaseDefending = def;
        BasePhysical = phy;
        ClubOrigin = club;
        if (secondary != null) SecondaryPositions = secondary;
        CallnameId = callnameId;
        CallnameKey = callnameKey;
        PreferredFoot = preferredFoot;
        Age = age;
        HeightCm = heightCm;
        TacticalRole = tacticalRole;
    }
}
