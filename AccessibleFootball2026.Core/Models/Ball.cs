using System.Numerics;

namespace AccessibleFootball2026.Core.Models;

public enum BallState
{
    Free,
    Controlled,
    InFlight,
    OutOfBounds,
    InGoal
}

/// <summary>
/// فيزياء الكرة ثلاثية الأبعاد (X, Y على أرضية الملعب، و Z الارتفاع في الهواء)
/// </summary>
public class Ball
{
    public Vector3 Position { get; set; } = new(52.5f, 34.0f, 0.0f); // مركز الملعب
    public Vector2 Position2D => new(Position.X, Position.Y);
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public Player? Owner { get; set; } = null;
    public BallState State { get; set; } = BallState.Free;

    // معاملات فيزيائية واقعية
    public const float GroundDeceleration = 2.5f; // تباطؤ خطي لاحتكاك العشب (متر/ثانية²)
    private const float AirResistance = 0.985f;    // مقاومة الهواء
    private const float Gravity = 9.81f;          // الجاذبية الأرضية
    private const float BounceRestitution = 0.6f; // ارتداد الكرة من الأرض

    public void UpdatePhysics(float deltaTime) => Update(deltaTime);

    public void Update(float deltaTime)
    {
        if (Owner != null)
        {
            // الكرة ملتصقة بقدم اللاعب المستحوذ
            Position = new Vector3(Owner.Position2D.X, Owner.Position2D.Y, 0.0f);
            Velocity = Vector3.Zero;
            State = BallState.Controlled;
            return;
        }

        // تحديث الموضع بناءً على السرعة
        Position += Velocity * deltaTime;

        // تطبيق الجاذبية ومقاومة الهواء في الهواء
        if (Position.Z > 0.0f)
        {
            Velocity = new Vector3(
                Velocity.X * AirResistance,
                Velocity.Y * AirResistance,
                Velocity.Z - (Gravity * deltaTime)
            );
        }

        // فحص الارتطام بالأرض عندما تصبح Z <= 0
        if (Position.Z <= 0.0f)
        {
            Position = new Vector3(Position.X, Position.Y, 0.0f);

            if (Math.Abs(Velocity.Z) > 1.0f)
            {
                // ارتداد الكرة من الأرض
                Velocity = new Vector3(
                    Velocity.X * 0.85f,
                    Velocity.Y * 0.85f,
                    -Velocity.Z * BounceRestitution
                );
            }
            else
            {
                // استقرار الكرة على الأرض
                Velocity = new Vector3(Velocity.X, Velocity.Y, 0.0f);
                if (State == BallState.InFlight)
                {
                    State = BallState.Free;
                }

                // تدحرج على العشب مع تباطؤ فيزيائي خطي مستقل عن معدل الإطارات
                float speed = Velocity.Length();
                if (speed > 0.05f)
                {
                    float newSpeed = Math.Max(0.0f, speed - (GroundDeceleration * deltaTime));
                    Velocity = (Velocity / speed) * newSpeed;
                }
                else
                {
                    Velocity = Vector3.Zero;
                    State = BallState.Free;
                }

                if (Velocity.Length() < 0.1f)
                {
                    State = BallState.Free;
                }
            }
        }
    }

    public void Kick(Vector2 direction, float horizontalSpeed, float verticalSpeed, Player? kicker = null)
    {
        if (direction.Length() > 0)
        {
            direction = Vector2.Normalize(direction);
        }

        if (Owner != null)
        {
            Owner.HasBall = false;
            Owner = null;
        }

        Velocity = new Vector3(direction.X * horizontalSpeed, direction.Y * horizontalSpeed, verticalSpeed);
        State = verticalSpeed > 0.5f ? BallState.InFlight : BallState.Free;
    }

    public void CatchOrTackle(Player newOwner)
    {
        if (Owner != null)
        {
            Owner.HasBall = false;
        }

        Owner = newOwner;
        newOwner.HasBall = true;
        State = BallState.Controlled;
        Velocity = Vector3.Zero;
        Position = new Vector3(newOwner.Position2D.X, newOwner.Position2D.Y, 0.0f);
    }
}
