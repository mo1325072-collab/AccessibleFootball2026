using System.Numerics;
using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Input;
namespace AccessibleFootball2026.Core.Simulation;
public partial class MatchEngine
{
    private void UpdatePlayersAI(float deltaTime)
    {
        ObserveTacticalPossession();
        Vector2 ball2D = new(Ball.Position.X, Ball.Position.Y);

        // تحديث التمريرة النشطة واعتراض المستلم أولاً قبل أي شيء
        if (ActivePassTransit != null && ActivePassTransit.IsActive)
        {
            UpdatePassTransit(deltaTime, ball2D);
        }

        // إذا كانت فترة حماية ركلة البداية جارية والكرة مع المستخدم، يقف الخصم في مناطقه لإعطاء فرصة للتوجيه والتمرير
        if (_kickoffProtectionTimer > 0.0f && Ball.Owner != null && HomeTeam.Players.Contains(Ball.Owner))
        {
            foreach (var opponent in AwayTeam.Players)
            {
                opponent.Position2D = Vector2.Lerp(opponent.Position2D, opponent.HomePosition, deltaTime * 1.2f);
            }
            return;
        }

        // 1. حارس مرمى ولاعبو الخصم (الكمبيوتر)
        foreach (var opponent in AwayTeam.Players)
        {
            if (opponent.IsSentOff || opponent.IsOffPitch) continue;

            if (opponent.Role == PlayerRole.Goalkeeper)
            {
                float targetY = Math.Clamp(ball2D.Y, Pitch.GoalYMin + 1.0f, Pitch.GoalYMax - 1.0f);
                float gkX = UserAttackingRight ? Pitch.Length - 4.0f : 4.0f;
                if(Ball.Owner!=opponent && Math.Abs(ball2D.X-gkX)<14 && Math.Abs(ball2D.Y-Pitch.Width/2)<16 && Ball.Position.Z<2.6f)
                    gkX=Math.Clamp(ball2D.X,UserAttackingRight?Pitch.Length-14:1,UserAttackingRight?Pitch.Length-1:14);
                opponent.Position2D = Vector2.Lerp(opponent.Position2D, new Vector2(gkX, targetY), deltaTime * 2.5f);

                if (Ball.Owner == opponent)
                {
                    _gkHoldTimer += deltaTime;
                    if (_gkHoldTimer >= 1.2f)
                    {
                        _gkHoldTimer = 0.0f;
                        Vector2 clearDir = UserAttackingRight 
                            ? new Vector2(-1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f)
                            : new Vector2(1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f);
                        SnapshotOffsideAtKick(opponent);
                        Ball.Kick(clearDir, 26.0f, 4.0f, opponent);
                        RecordBallTouch(opponent);
                        Publish(new MatchEvent(MatchEventType.BallKicked, $"حارس مرمى {AwayTeam.Name} يوزع الكرة عالية للأمام.", CurrentZone, spatialPan: CalculateBallPan(),subjectPlayer:opponent),("action.kind","pass"),("pass.height","high"));
                    }
                }
                else if (!_isShotInProgress && Vector2.Distance(opponent.Position2D, ball2D) < 2.0f && Ball.Position.Z < Pitch.GoalHeight)
                {
                    // التقاط الكرات العادية فقط: التسديدات تُحسم بمواجهة حارس واحدة مبنية
                    // على المهارات، لا بالتقاط تلقائي مؤكد 100%
                    _gkHoldTimer = 0.0f;
                    bool takenFromOpponent=Ball.Owner!=null&&HomeTeam.Players.Contains(Ball.Owner);
                    bool keeperRushed=Math.Abs(opponent.Position2D.X-(UserAttackingRight?Pitch.Length:0))>6;
                    Ball.CatchOrTackle(opponent);
                    RecordBallTouch(opponent);
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }
                    Publish(new MatchEvent(MatchEventType.BallTrapped,"حارس الخصم يمسك بالكرة",CurrentZone,subjectPlayer:opponent),
                        ("keeper.rushedOut",keeperRushed),("control.fromOpponent",takenFromOpponent),("save.held",true));
                }
                continue;
            }

            if (Ball.Owner == opponent)
            {
                UpdateCpuCarrier(opponent, deltaTime);
                continue;
            }

            float distToBall = Vector2.Distance(opponent.Position2D, ball2D);
            var diff = MatchSettings.Current.Difficulty;
            float speedMult = diff switch
            {
                DifficultyLevel.Easy => 0.045f,
                DifficultyLevel.Normal => 0.062f,
                DifficultyLevel.Hard => 0.085f,
                DifficultyLevel.Legendary => 0.105f,
                _ => 0.062f
            };

            float pressRange = diff switch
            {
                DifficultyLevel.Easy => 10.0f,
                DifficultyLevel.Normal => 13.5f,
                DifficultyLevel.Hard => 16.5f,
                DifficultyLevel.Legendary => 20.0f,
                _ => 13.5f
            };

            // توازن الذكاء الاصطناعي: أثناء التمريرة، يضغط فقط المدافع القريب في مسار التمريرة دون تكالب المدافعين البعيدين
            if (ActivePassTransit != null && ActivePassTransit.IsActive)
            {
                pressRange = Math.Min(pressRange, 8.0f);
            }

            if (ShouldTacticalPress(AwayTeam,opponent,pressRange))
            {
                var toBall = ball2D - opponent.Position2D;
                Vector2 pressDir = toBall.LengthSquared() > .000001f ? Vector2.Normalize(toBall) : Vector2.Zero;
                opponent.Position2D += pressDir * (opponent.Speed * speedMult * DefensiveReactionMultiplier(opponent) * FatigueMovementMultiplier(opponent)) * deltaTime;
                opponent.FacingDirection = pressDir;

                if (distToBall < 1.3f && Ball.Owner != opponent && (Ball.Owner == null || !AwayTeam.Players.Contains(Ball.Owner)))
                {
                    // فحص حماية الاستحواذ وفترة تهدئة قطع الكرة لمنع قطع الكرة المتكرر بسرعة غير بشرية
                    if (_possessionGraceTimer > 0.0f || _aiTackleCooldownTimer > 0.0f)
                    {
                        // الخصم يضغط ويلاحق فقط دون أن يقطع الكرة فورياً
                    }
                    else
                    {
                        var carrier = Ball.Owner;
                        if (carrier == null)
                        {
                            // كرة حرة يستحوذ عليها الخصم عند وصوله إليها
                            Ball.CatchOrTackle(opponent);
                            RecordBallTouch(opponent);
                            _aiTackleCooldownTimer = 1.6f;
                            if (ActivePassTransit != null)
                            {
                                ActivePassTransit.IsActive = false;
                                ActivePassTransit = null;
                            }
                            string desc = $"استحوذ {opponent.Name} على الكرة في {CurrentZone.ToArabicAnnouncement()}.";
                            Publish(new MatchEvent(MatchEventType.OpponentTackleSuccessful, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent));
                        }
                        else
                        {
                            // صراع استخلاص الكرة التكتيكي والبدني المبني على الطاقات والتموضع وشراسة العرقلة
                            var duel = EvaluateTackleDuel(
                                tackler: opponent,
                                carrier: carrier,
                                ballPos2D: ball2D,
                                tacklerTeam: AwayTeam,
                                carrierTeam: HomeTeam,
                                diff: diff,
                                counterTransitionActive:IsCounterTransition(HomeTeam)
                            );

                            _aiTackleCooldownTimer = 1.5f;

                            if (duel.TacklerWon)
                            {
                                Ball.CatchOrTackle(opponent);
                                RecordBallTouch(opponent);
                                _aiTackleCooldownTimer = 2.0f;
                                if (ActivePassTransit != null)
                                {
                                    ActivePassTransit.IsActive = false;
                                    ActivePassTransit = null;
                                }
                                string desc = $"قطع {opponent.Name} الكرة من {carrier.Name}! ({duel.TacticalReason})";
                                Publish(new MatchEvent(MatchEventType.OpponentTackleSuccessful, desc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent));
                            }
                            else if (duel.IsFoul)
                            {
                                // ارتكب الخصم خطأ لصالحه أثناء محاولة التدخل غير الشرعي
                                _aiTackleCooldownTimer = 2.5f;
                                Ball.CatchOrTackle(carrier);
                                Ball.Velocity = Vector3.Zero;
                                string foulDesc = $"خطأ لصالح {carrier.Name} بعد عرقلة من {opponent.Name}!";
                                Publish(new MatchEvent(MatchEventType.FoulCommitted, foulDesc, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: opponent), ("foul.intentional", false));
                                HandleFoulSanction(opponent, carrier, isSlideTackle: duel.SlidingCommitment, isFromBehind: duel.FromBehind);

                            }
                            else
                            {
                                // حامل الكرة احتفظ بكرته بنجاح بفضل مهارته أو قوته البدنية أو حجز الكرة
                                _aiTackleCooldownTimer = 1.3f;
                                _dribblesWon++;
                                Publish(new MatchEvent(MatchEventType.DribbleSuccessful, $"{carrier.Name} يتجاوز محاولة الاستخلاص", CurrentZone, subjectPlayer: carrier),
                                    ("attack.defenderBeaten", true), ("attack.dribblesWon", _dribblesWon));
                            }
                        }
                    }
                }
            }
            else
            {
                opponent.Position2D = Vector2.Lerp(opponent.Position2D, GetFormationSupportTarget(AwayTeam, opponent), Math.Clamp(deltaTime * .8f * FatigueMovementMultiplier(opponent) * (.7f+AwayTeam.Tactics.Tempo*.006f),0,1));
            }
        }

        // 2. لاعبو فريق المستخدم (HomeTeam)
        foreach (var teammate in HomeTeam.Players)
        {
            if (teammate.IsSentOff || teammate.IsOffPitch) continue;

            if (teammate == ActiveUserPlayer && (ActivePassTransit == null || !ActivePassTransit.IsActive || teammate != ActivePassTransit.IntendedReceiver))
            {
                continue;
            }

            if (ActivePassTransit != null && ActivePassTransit.IsActive && teammate == ActivePassTransit.IntendedReceiver)
            {
                // المستلم يتم توجيهه واعتراضه عبر UpdatePassTransit
                continue;
            }

            if (teammate.Role == PlayerRole.Goalkeeper)
            {
                float targetY = Math.Clamp(ball2D.Y, Pitch.GoalYMin + 1.0f, Pitch.GoalYMax - 1.0f);
                float gkX = UserAttackingRight ? 4.0f : Pitch.Length - 4.0f;
                if(Ball.Owner!=teammate && Math.Abs(ball2D.X-gkX)<14 && Math.Abs(ball2D.Y-Pitch.Width/2)<16 && Ball.Position.Z<2.6f)
                    gkX=Math.Clamp(ball2D.X,UserAttackingRight?1:Pitch.Length-14,UserAttackingRight?14:Pitch.Length-1);
                teammate.Position2D = Vector2.Lerp(teammate.Position2D, new Vector2(gkX, targetY), deltaTime * 2.5f);

                if (Ball.Owner == teammate)
                {
                    _gkHoldTimer += deltaTime;
                    if (_gkHoldTimer >= 3.0f)
                    {
                        _gkHoldTimer = 0.0f;
                        Vector2 clearDir = UserAttackingRight 
                            ? new Vector2(1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f)
                            : new Vector2(-1.0f, ((float)Random.Shared.NextDouble() - 0.5f) * 0.4f);
                        SnapshotOffsideAtKick(teammate);
                        Ball.Kick(clearDir, 26.0f, 4.0f, teammate);
                        RecordBallTouch(teammate);
                        Publish(new MatchEvent(MatchEventType.BallKicked, $"حارسك {teammate.Name} يوزع الكرة عالية للأمام.", CurrentZone, spatialPan: CalculateBallPan(),subjectPlayer:teammate),("action.kind","pass"),("pass.height","high"));
                    }
                }
                else if (!_isShotInProgress && Vector2.Distance(teammate.Position2D, ball2D) < 2.5f && Ball.Position.Z < Pitch.GoalHeight && (Ball.Owner == null || AwayTeam.Players.Contains(Ball.Owner)))
                {
                    _gkHoldTimer = 0.0f;
                    bool takenFromOpponent=Ball.Owner!=null&&AwayTeam.Players.Contains(Ball.Owner);
                    bool keeperRushed=Math.Abs(teammate.Position2D.X-(UserAttackingRight?0:Pitch.Length))>6;
                    Ball.CatchOrTackle(teammate);
                    RecordBallTouch(teammate);
                    ActiveUserPlayer = teammate;
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }
                    Publish(new MatchEvent(MatchEventType.BallTrapped, $"حارس مرماك {teammate.Name} يمسك بالكرة بأمان.", CurrentZone, spatialPan: CalculateBallPan(),subjectPlayer:teammate),
                        ("keeper.rushedOut",keeperRushed),("control.fromOpponent",takenFromOpponent),("save.held",true));
                }
                continue;
            }

            Vector2 shiftedHome = ShouldTacticalPress(HomeTeam,teammate,13.5f) ? ball2D : GetFormationSupportTarget(HomeTeam, teammate);
            teammate.Position2D = Vector2.Lerp(teammate.Position2D, shiftedHome, Math.Clamp(deltaTime * DefensiveReactionMultiplier(teammate) * FatigueMovementMultiplier(teammate) * (.7f+HomeTeam.Tactics.Tempo*.006f),0,1));

            if (Ball.State == BallState.Free && Ball.Position.Z <= 1.5f && !IsPassInTransit && Vector2.Distance(teammate.Position2D, ball2D) < 1.8f)
            {
                if (!TryControlIncomingBall(teammate)) continue;
                ActiveUserPlayer = teammate;
                if (ActivePassTransit != null)
                {
                    ActivePassTransit.IsActive = false;
                    ActivePassTransit = null;
                }
                string zoneDesc = CurrentZone.ToArabicAnnouncement();
                string receptionMsg = $"استلم {teammate.Name} الكرة في {zoneDesc}.";
                Publish(new MatchEvent(MatchEventType.BallTrapped, receptionMsg, CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: teammate));
            }
        }

        // 3. منع تجمد الكرة الحرة إطلاقاً: إذا سقطت الكرة في أي مكان بدون صاحب يجري أقرب لاعب إليها فوراً
        if (Ball.Owner == null && !IsPassInTransit && !_isShotInProgress)
        {
            _ballFreeTimer += deltaTime;
            Player? closestPlayer = null;
            float closestDist = float.MaxValue;
            var allOutfield = HomeTeam.Players.Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch)
                .Concat(AwayTeam.Players.Where(p => p.Role != PlayerRole.Goalkeeper && !p.IsSentOff && !p.IsOffPitch));

            foreach (var p in allOutfield)
            {
                // إذا كانت هناك تسديدة أو تمريرة، يستبعد الراكل أثناء انطلاق الكرة السريعة لمنعه من استردادها فور ركلها
                if (p == LastKicker && Ball.Velocity.Length() > 2.0f)
                {
                    continue;
                }

                // إذا كانت هناك تمريرة جارية، يتم استبعاد مرسل التمريرة أثناء انطلاق الكرة لمنعه من استردادها فور ركلها
                if (IsPassInTransit && p == ActivePassTransit?.Sender && (ActivePassTransit.ElapsedTime < 0.35f || Ball.Velocity.Length() > 0.5f))
                {
                    continue;
                }

                // أثناء التمريرة الجارية، لا يستولي زميل آخر على الكرة الموجهة للمستلم المستهدف
                if (IsPassInTransit && HomeTeam.Players.Contains(p) && p != ActivePassTransit?.IntendedReceiver)
                {
                    continue;
                }

                float d = Vector2.Distance(p.Position2D, ball2D);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestPlayer = p;
                }
            }

            if (closestPlayer != null)
            {
                Vector2 toBall = ball2D - closestPlayer.Position2D;
                if (toBall.Length() > 0.1f)
                {
                    Vector2 chaseDir = Vector2.Normalize(toBall);
                    closestPlayer.Position2D += chaseDir * (closestPlayer.Speed * 0.09f * FatigueMovementMultiplier(closestPlayer)) * deltaTime;
                }

                if (closestDist < 1.6f && Ball.Position.Z <= 1.5f)
                {
                    _ballFreeTimer = 0.0f;
                    if (!TryControlIncomingBall(closestPlayer)) return;
                    if (ActivePassTransit != null)
                    {
                        ActivePassTransit.IsActive = false;
                        ActivePassTransit = null;
                    }

                    if (HomeTeam.Players.Contains(closestPlayer))
                    {
                        ActiveUserPlayer = closestPlayer;
                        Publish(new MatchEvent(MatchEventType.BallTrapped, $"الكرة أصبحت مع لاعبك {closestPlayer.Name} في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone, spatialPan: CalculateBallPan()));
                    }
                    else
                    {
                        Publish(new MatchEvent(MatchEventType.BallTrapped, $"الكرة مع {closestPlayer.Name} لاعب {AwayTeam.Name} في {CurrentZone.ToArabicAnnouncement()}.", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: closestPlayer));
                    }
                }
            }
        }
        else
        {
            _ballFreeTimer = 0.0f;
        }
    }

    private void CheckPitchZone()
    {
        var newZone = Pitch.CalculateZone(Ball.Position, attackingTowardsRight: UserAttackingRight);
        if (newZone != CurrentZone)
        {
            CurrentZone = newZone;
            Publish(new MatchEvent(MatchEventType.BallZoneChanged, "تغير منطقة اللعب", CurrentZone));
        }
    }

    public void TriggerGoalkeeperSave(string desc = "تصدي رائع وخيالي من حارس المرمى!") =>
        Publish(new MatchEvent(MatchEventType.GoalkeeperSave, desc, CurrentZone, spatialPan: CalculateBallPan()));

    public void TriggerWoodworkHit(string desc = "في القائم! العارضة تمنع هدفاً محققاً!") =>
        Publish(new MatchEvent(MatchEventType.WoodworkHit, desc, CurrentZone, spatialPan: CalculateBallPan()));

    public void TriggerPenaltyAwarded(string desc = "ركلة جزاء!") =>
        Publish(new MatchEvent(MatchEventType.PenaltyAwarded, desc, CurrentZone, spatialPan: CalculateBallPan()));

    private void CheckRefereeRules()
    {
        // 0. فحص تصدي الحارس أو ارتطام الكرة بالقائم/العارضة أثناء التسديد
        if (_isShotInProgress)
        {
            float distToGoalX = Math.Abs(Ball.Position.X - _shotTargetGoalX);

            // فحص ارتطام القائم أو العارضة (Woodwork Hit)
            if (distToGoalX <= 1.2f)
            {
                bool hitLeftPost = Math.Abs(Ball.Position.Y - Pitch.GoalYMin) <= 0.45f && Ball.Position.Z <= Pitch.GoalHeight + 0.15f;
                bool hitRightPost = Math.Abs(Ball.Position.Y - Pitch.GoalYMax) <= 0.45f && Ball.Position.Z <= Pitch.GoalHeight + 0.15f;
                bool hitCrossbar = Ball.Position.Y >= Pitch.GoalYMin - 0.2f && Ball.Position.Y <= Pitch.GoalYMax + 0.2f && Math.Abs(Ball.Position.Z - Pitch.GoalHeight) <= 0.25f;

                if (hitLeftPost || hitRightPost || hitCrossbar)
                {
                    _isShotInProgress = false;
                    Ball.Velocity = new Vector3(-Ball.Velocity.X * 0.75f, Ball.Velocity.Y * 0.8f, Math.Max(1.5f, Ball.Velocity.Z * 0.5f));
                    Ball.Position = new Vector3(Math.Clamp(Ball.Position.X, 1.0f, Pitch.Length - 1.0f), Ball.Position.Y, Math.Clamp(Ball.Position.Z, 0.0f, Pitch.GoalHeight));
                    _lastShotWoodwork = hitCrossbar ? "crossbar" : "post";
                    Publish(new MatchEvent(MatchEventType.WoodworkHit, hitCrossbar ? "في العارضة!" : "في القائم!", CurrentZone, spatialPan: CalculateBallPan()), ("woodwork.part", _lastShotWoodwork));
                    return;
                }
            }

            // فحص تصدي حارس المرمى: مواجهة واحدة لكل تسديدة تُحسم بلفة واحدة تعتمد
            // على مهارة المسدد وقدرة الحارس، لا لفة 35% متكررة بكل إطار (~82% تصدٍ حتمي)
            if (!_shotGkResolved && distToGoalX <= 3.0f && Ball.Position.Y >= Pitch.GoalYMin - 0.5f && Ball.Position.Y <= Pitch.GoalYMax + 0.5f && Ball.Position.Z <= Pitch.GoalHeight)
            {
                Player? defendingGk = GetDefendingGoalkeeperForShot();
                if (defendingGk != null)
                {
                    float gkDist = Vector2.Distance(new Vector2(Ball.Position.X, Ball.Position.Y), defendingGk.Position2D);
                    if (gkDist <= 4.2f)
                    {
                        _shotGkResolved = true;
                        Player shooter = _shotShooter ?? LastKicker ?? defendingGk;
                        if (ResolveGoalkeeperSaveAttempt(shooter, defendingGk))
                        {
                            RememberShotDeflection();
                            _isShotInProgress = false;
                            float incomingSpeed = Ball.Velocity.Length();
                            bool held = incomingSpeed <= 25 && gkDist <= 2.4f;
                            bool hardSave=incomingSpeed>30 || gkDist>1.7f || Ball.Position.Z>1.3f;
                            bool rushedOut = Math.Abs(defendingGk.Position2D.X - _shotTargetGoalX) > 6;
                            bool keeperDived=gkDist>1.2f||Ball.Position.Z>1.2f;
                            if(keeperDived)defendingGk.Position2D=Ball.Position2D;
                            bool keeperPressured = (TeamOf(defendingGk) == HomeTeam ? AwayTeam : HomeTeam).Players
                                .Any(p => p.IsActiveOnPitch && Vector2.Distance(p.Position2D, defendingGk.Position2D) < 4);
                            var shotFacts = _lastShotContext;
                            if (held)
                            {
                                Ball.CatchOrTackle(defendingGk);
                                RecordBallTouch(defendingGk, deliberatelyControlled:false);
                                _gkHoldTimer = 1.0f;
                            }
                            else
                            {
                                Ball.Velocity = new Vector3(-Ball.Velocity.X * 0.35f, ((float)Random.Shared.NextDouble() - 0.5f) * 6.0f, 0.5f);
                                RecordBallTouch(defendingGk, deliberatelyControlled:false);
                            }
                            Publish(new MatchEvent(MatchEventType.GoalkeeperSave, $"تصدي من {defendingGk.Name}!", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: defendingGk) { Context = shotFacts },
                                ("save.dived",keeperDived),("save.keeperCount",PerformanceOf(defendingGk).Saves+1),("save.held", held), ("save.dangerContinues", !held), ("save.difficulty", hardSave ? "hard" : "easy"),
                                ("keeper.rushedOut", rushedOut), ("keeper.underPressure", keeperPressured), ("shot.onTarget", true));
                            return;
                        }
                        // الحارس مُتغلَّب عليه: الكرة تكمل نحو المرمى أو الخشبات
                    }
                }
            }

            // تسديدة ميتة: فقدت الكرة سرعتها قبل بلوغ المرمى -> عودة للعب المفتوح
            if (Ball.Velocity.Length() < 1.0f && Ball.Position.Z <= 0.05f)
            {
                _isShotInProgress = false;
            }
        }

        // 1. فحص الهدف
        if (Pitch.IsGoal(Ball.Position, out bool forTeamAttackingRight))
        {
            if(_offsideOffence!=null)
            {
                if(BeginVideoReview(VideoReviewCategory.Goal,()=>PenaliseOffside(true),T("قرار الفار: الهدف غير محتسب بسبب التسلل.","VAR decision: no goal, offside."),"offside"))return;
                PenaliseOffside(true);return;
            }
            if(BeginVideoReview(VideoReviewCategory.Goal,CheckRefereeRules,T("قرار الفار: الهدف صحيح ويحتسب.","VAR decision: goal confirmed."),"goal"))return;
            _isShotInProgress = false;
            if (ActivePassTransit != null)
            {
                ActivePassTransit.IsActive = false;
                ActivePassTransit = null;
            }
            bool forHomeTeam = UserAttackingRight ? forTeamAttackingRight : !forTeamAttackingRight;
            Team scoringTeam = forHomeTeam ? HomeTeam : AwayTeam;
            scoringTeam.Score++;

            // حساب المسجل والأسيست والهدف العكسي
            Player scorer = LastKicker ?? (forHomeTeam ? HomeTeam : AwayTeam).Players.First(p => p.IsActiveOnPitch);
            bool deflectedOnTarget=_onTargetDeflectedShooter is {} originalShooter && TeamOf(originalShooter)==scoringTeam && _elapsedSeconds-_deflectionRecordedAt<=10;
            if(deflectedOnTarget)scorer=_onTargetDeflectedShooter!;
            bool isOwnGoal = TeamOf(scorer) is { } scorerTeam && scorerTeam != scoringTeam;

            Player? assistCandidate=deflectedOnTarget?_onTargetDeflectedAssister:PreviousKicker;
            Player? assister = !isOwnGoal && assistCandidate != null && assistCandidate != scorer &&
                                TeamOf(assistCandidate) == scoringTeam ? assistCandidate : null;

            if (!isOwnGoal) scorer.MatchGoalsCount++;

            var goalDetails = new GoalDetails(
                scorer: scorer,
                assister: assister,
                isOwnGoal: isOwnGoal,
                minute: Math.Max(1, (int)MatchTimeMinutes),
                homeScore: HomeTeam.Score,
                awayScore: AwayTeam.Score,
                scoringTeam: scoringTeam
            );

            // تجميد اللعب فوراً للاحتفال بالهدف لمدة 4 ثوانٍ ثم استئناف تلقائي
            State = MatchState.GoalCelebration;
            _celebrationTimer = 4.0f;

            Publish(new MatchEvent(
                MatchEventType.GoalScored,
                $"هدف لصالح {scoringTeam.Name}!",
                CurrentZone,
                goalDetails
            ));

            // 1. فحص الهاتريك (Hat-Trick)
            if (!isOwnGoal)
            {
                if (scorer.MatchGoalsCount == 3)
                {
                    Publish(new MatchEvent(
                        MatchEventType.HatTrickAchieved,
                        $"هاتريك تاريخي للاعب {scorer.Name}!",
                        CurrentZone,
                        goalDetails
                    ));
                }
            }

            // 2. فحص الريمونتادا / العودة الدرامية (Dramatic Comeback)
            if (forHomeTeam && _homeTeamWasTrailing && HomeTeam.Score > AwayTeam.Score && MatchTimeMinutes >= 70.0f)
            {
                _homeTeamWasTrailing = false;
                Publish(new MatchEvent(
                    MatchEventType.DramaticComeback,
                    $"ريمونتادا جنونية وغير متوقعة لصالح {HomeTeam.Name}!",
                    CurrentZone,
                    goalDetails
                ));
            }
            else if (!forHomeTeam && _awayTeamWasTrailing && AwayTeam.Score > HomeTeam.Score && MatchTimeMinutes >= 70.0f)
            {
                _awayTeamWasTrailing = false;
                Publish(new MatchEvent(
                    MatchEventType.DramaticComeback,
                    $"ريمونتادا جنونية وغير متوقعة لصالح {AwayTeam.Name}!",
                    CurrentZone,
                    goalDetails
                ));
            }

            // تحديث حالة التأخر في النتيجة للفريقين
            if (HomeTeam.Score < AwayTeam.Score) _homeTeamWasTrailing = true;
            if (AwayTeam.Score < HomeTeam.Score) _awayTeamWasTrailing = true;

            return;
        }

        // 2. خروج الكرة خارج حدود الملعب (ضربات مرمى، ركنيات، رميات تماس واقعية)
        if (Pitch.IsOutOfBounds(Ball.Position, out bool isTouchline, out bool isGoalLine))
        {
            if (ActivePassTransit != null)
            {
                ActivePassTransit.IsActive = false;
                ActivePassTransit = null;
            }

            if (LastKicker == _lastMiscontroller && _elapsedSeconds <= _miscontrolUntil + 2)
            {
                Publish(new MatchEvent(MatchEventType.ControlLost,"اللمسة الأخيرة أخرجت الكرة",CurrentZone,subjectPlayer:_lastMiscontroller),
                    ("control.pressured",_miscontrolPressured),("control.outOfBounds",true));
                _lastMiscontroller=null;
            }
            _lastOutWasShot=_isShotInProgress;
            Publish(new MatchEvent(MatchEventType.BallOut,"الكرة خارج الملعب",CurrentZone));
            if (isGoalLine)
            {
                if (_isShotInProgress)
                {
                    _isShotInProgress = false;
                    Publish(new MatchEvent(MatchEventType.MissedChance, Ball.Position.Z > Pitch.GoalHeight ? "تسديدة فوق المرمى" : "تسديدة خارج المرمى", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: _shotShooter),
                        ("miss.direction", Ball.Position.Z > Pitch.GoalHeight ? "over" : "wide"),
                        ("miss.margin", Math.Max(0, Ball.Position.Z > Pitch.GoalHeight ? Ball.Position.Z - Pitch.GoalHeight : Math.Max(Pitch.GoalYMin - Ball.Position.Y, Ball.Position.Y - Pitch.GoalYMax))));
                    RequestAnalysisReplay(MatchEventType.MissAnalysis,Snapshot().With(("attack.dribblesWon",_dribblesWon)));
                }

                bool crossedNearHomeGoal = UserAttackingRight ? (Ball.Position.X <= 0) : (Ball.Position.X >= Pitch.Length);
                bool lastTouchHome = TeamOf(LastKicker) == HomeTeam;

                if (crossedNearHomeGoal)
                {
                    if (lastTouchHome)
                    {
                        AwardCornerKick(forHomeTeam: false);
                    }
                    else
                    {
                        AwardGoalKick(forHomeTeam: true);
                    }
                }
                else
                {
                    if (lastTouchHome)
                    {
                        AwardGoalKick(forHomeTeam: false);
                    }
                    else
                    {
                        AwardCornerKick(forHomeTeam: true);
                    }
                }
            }
            else if (isTouchline)
            {
                _isShotInProgress = false;
                bool lastTouchHome = TeamOf(LastKicker) == HomeTeam;
                AwardThrowIn(forHomeTeam: !lastTouchHome);
            }
        }
    }

    private bool _lastOutWasShot;
    private void AwardGoalKick(bool forHomeTeam)
    {
        _isShotInProgress = false;
        bool homeAtLeft = UserAttackingRight;
        if (forHomeTeam)
        {
            var gk = HomeTeam.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role == PlayerRole.Goalkeeper) ?? HomeTeam.Players.First(p => p.IsActiveOnPitch);
            float gkX = homeAtLeft ? 5.0f : Pitch.Length - 5.0f;
            gk.Position2D = new Vector2(gkX, Pitch.Width / 2.0f);
            Ball.Position = new Vector3(gkX, Pitch.Width / 2.0f, 0.0f);
            Ball.CatchOrTackle(gk);
            RecordBallTouch(gk);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=gk;
            ActiveUserPlayer = gk;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            Restart = new PendingRestart(RestartKind.GoalKick, HomeTeam, gk, gk.Position2D);
            _restartReadySeconds = 0;
            Publish(new MatchEvent(MatchEventType.GoalKick, $"ضربة مرمى: {gk.Name}", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: gk),("restart.cause","goalLineExit"),("restart.missedShot",_lastOutWasShot));
        }
        else
        {
            var gk = AwayTeam.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role == PlayerRole.Goalkeeper) ?? AwayTeam.Players.First(p => p.IsActiveOnPitch);
            float gkX = homeAtLeft ? Pitch.Length - 5.0f : 5.0f;
            gk.Position2D = new Vector2(gkX, Pitch.Width / 2.0f);
            Ball.Position = new Vector3(gkX, Pitch.Width / 2.0f, 0.0f);
            Ball.CatchOrTackle(gk);
            RecordBallTouch(gk);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=gk;
            _gkHoldTimer = 0.5f;
            Restart = new PendingRestart(RestartKind.GoalKick, AwayTeam, gk, gk.Position2D);
            _restartReadySeconds = 0;
            Publish(new MatchEvent(MatchEventType.GoalKick, $"ضربة مرمى لصالح {AwayTeam.Name}.", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: gk),("restart.cause","goalLineExit"),("restart.missedShot",_lastOutWasShot));
        }
    }

    private void AwardCornerKick(bool forHomeTeam)
    {
        _isShotInProgress = false;
        float cornerY = Ball.Position.Y > Pitch.Width / 2.0f ? Pitch.Width - 1.0f : 1.0f;
        bool homeAttacksRight = UserAttackingRight;
        if (forHomeTeam)
        {
            float cornerX = homeAttacksRight ? Pitch.Length - 1.0f : 1.0f;
            var taker = HomeTeam.CornerTaker?.IsActiveOnPitch == true ? HomeTeam.CornerTaker : HomeTeam.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper) ?? HomeTeam.Players.First(p => p.IsActiveOnPitch);

            taker.Position2D = new Vector2(cornerX, cornerY);
            Ball.Position = new Vector3(cornerX, cornerY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            ActiveUserPlayer = taker;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            _attackOrigin = "corner";
            Restart=new PendingRestart(RestartKind.Corner,HomeTeam,taker,taker.Position2D);_restartReadySeconds=0;
            Publish(new MatchEvent(MatchEventType.CornerKick, $"ضربة ركنية: {taker.Name}", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: taker), ("restart.phase", "ready"), ("restart.cause", "defensiveTouch"));
        }
        else
        {
            float cornerX = homeAttacksRight ? 1.0f : Pitch.Length - 1.0f;
            var taker = AwayTeam.CornerTaker?.IsActiveOnPitch == true ? AwayTeam.CornerTaker : AwayTeam.Players.FirstOrDefault(p => p.IsActiveOnPitch && p.Role != PlayerRole.Goalkeeper) ?? AwayTeam.Players.First(p => p.IsActiveOnPitch);

            taker.Position2D = new Vector2(cornerX, cornerY);
            Ball.Position = new Vector3(cornerX, cornerY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            _attackOrigin="corner";
            Restart=new PendingRestart(RestartKind.Corner,AwayTeam,taker,taker.Position2D);_restartReadySeconds=0;
            Publish(new MatchEvent(MatchEventType.CornerKick,$"ضربة ركنية لصالح {AwayTeam.Name}",CurrentZone,subjectPlayer:taker),
                ("restart.phase","ready"),("restart.cause","defensiveTouch"));
        }
    }

    private void AwardThrowIn(bool forHomeTeam)
    {
        _isShotInProgress = false;
        float touchX = Math.Clamp(Ball.Position.X, 5.0f, Pitch.Length - 5.0f);
        float touchY = Ball.Position.Y > Pitch.Width / 2.0f ? Pitch.Width - 0.5f : 0.5f;

        if (forHomeTeam)
        {
            var taker = HomeTeam.Players.Where(p => !p.IsSentOff && !p.IsOffPitch)
                .OrderBy(p => Vector2.Distance(p.Position2D, new Vector2(touchX, touchY)))
                .FirstOrDefault() ?? HomeTeam.Players[0];
            taker.Position2D = new Vector2(touchX, touchY);
            Ball.Position = new Vector3(touchX, touchY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            ActiveUserPlayer = taker;
            _kickoffProtectionTimer = 2.5f;
            GrantPossessionGrace(2.0f);
            Restart = new PendingRestart(RestartKind.ThrowIn, HomeTeam, taker, taker.Position2D);
            _restartReadySeconds = 0;
            Publish(new MatchEvent(MatchEventType.ThrowIn, $"رمية تماس: {taker.Name}", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: taker));
        }
        else
        {
            var taker = AwayTeam.Players.Where(p => !p.IsSentOff && !p.IsOffPitch)
                .OrderBy(p => Vector2.Distance(p.Position2D, new Vector2(touchX, touchY)))
                .FirstOrDefault() ?? AwayTeam.Players[0];
            taker.Position2D = new Vector2(touchX, touchY);
            Ball.Position = new Vector3(touchX, touchY, 0.0f);
            Ball.CatchOrTackle(taker);
            RecordBallTouch(taker);
            _offsidePositions.Clear(); _offsideOffence=null; _offsideExemptKicker=taker;
            Restart = new PendingRestart(RestartKind.ThrowIn, AwayTeam, taker, taker.Position2D);
            _restartReadySeconds = 0;
            Publish(new MatchEvent(MatchEventType.ThrowIn, $"رمية تماس لصالح {AwayTeam.Name}.", CurrentZone, spatialPan: CalculateBallPan(), subjectPlayer: taker));
        }
    }

    /// <summary>
    /// استئناف اللعب وركلة البداية (Kickoff) فقط بعد انتهاء صرخة المعلق وإعلان قارئ الشاشة لمسجل الهدف
    /// </summary>
    public void ResumePlayAfterGoalCelebration(Team kickoffTeam)
    {
        PlaceKickoff(kickoffTeam);
        State = MatchState.InPlay;
    }
}
