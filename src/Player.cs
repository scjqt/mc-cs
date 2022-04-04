using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace Minecraft_Clone
{
    static class States
    {
        public const int Walk = 1;
        public const int Fly = 2 | (1 << 2) | (1 << 3);
        public const int Ghost = 3 | (1 << 2) | (1 << 3);

        public const int Airborne = 1 << 2;
        public const int Grounded = 0;

        public const int NoCrouch = 1 << 3;
        public const int Crouch = 1 << 4;

        public const int NoSprint = 1 << 4;
        public const int Sprint = 1 << 3;

        public static readonly Dictionary<int, MovementStats> Stats = new Dictionary<int, MovementStats>
        {
            { Walk | Grounded | NoCrouch | NoSprint, new MovementStats(4, 0.3, 78.4, 0.9604) },
            { Walk | Airborne | NoCrouch | NoSprint, new MovementStats(3.5, 0.83, 78.4, 0.9604) },

            { Walk | Grounded | Crouch,              new MovementStats(1.5, 0.3, 78.4, 0.9604) },
            { Walk | Airborne | Crouch,              new MovementStats(1, 0.83, 78.4, 0.9604) },

            { Walk | Grounded | Sprint,              new MovementStats(6.5, 0.3, 78.4, 0.9604) },
            { Walk | Airborne | Sprint,              new MovementStats(6.5, 0.83, 78.4, 0.9604) },

            { Fly | NoSprint,                        new MovementStats(11, 0.8, 7.5, 0.3) },
            { Fly | Sprint,                          new MovementStats(22, 0.8, 7.5, 0.3) },

            { Ghost | NoSprint,                      new MovementStats(11, 0.8, 7.5, 0.3) },
            { Ghost | Sprint,                        new MovementStats(22, 0.8, 7.5, 0.3) },
        };
    }

    class Player
    {
        private const double SENSITIVITY = 6.0;

        private const double WIDTH = 0.6;
        private const double HEIGHT = 1.8;
        private const double EYELEVEL = 1.6;
        private const double CROUCHHEIGHT = 1.5;
        private const double CROUCHEYELEVEL = 1.3;

        private const double EYELEVELCHANGE = 0.8;

        private const double JUMP = 9.28;
        private const double JUMPDELAY = 0.05;

        private const double BASEFOV = 70;
        private const double FOVINCREASE = 10;
        private const double FOVCHANGERATE = 100;

        public Vector3d Position { get; private set; }
        public Vector2d Direction { get; private set; }
        private Vector3d velocity;

        public double FOV { get; private set; }

        public AABB hitbox;

        public int Mode { get; set; }
        public int Sprint { get; set; }
        private int grounded;
        private int crouching;

        private Vector3i onBlock;
        private AABB onAABB;

        private double jumpTimer;
        private bool jumped;

        public double eyeLevel;

        public Player(double x, double y, double z)
        {
            Position = new Vector3d(x, y, z);
            Direction = new Vector2d();
            velocity = new Vector3d();

            UpdateHitbox();

            Mode = States.Walk;
            Sprint = States.NoSprint;
            grounded = States.Airborne;
            crouching = States.NoCrouch;

            eyeLevel = EYELEVEL;

            FOV = BASEFOV;
        }

        public void Look(Vector2 delta)
        {
            Direction = new Vector2d(
                Math.Clamp(Direction.X + delta.Y * SENSITIVITY / 10000, -Math.PI / 2, Math.PI / 2),
                (Direction.Y + delta.X * SENSITIVITY / 10000) % (Math.PI * 2));
        }

        public void Update(double deltaTime, World world, int forward, int strafe, bool space, bool shift)
        {
            if (forward != 1)
            {
                Sprint = States.NoSprint;
            }

            HandleCrouch(shift, world, deltaTime);

            MovementStats stats = States.Stats[Mode | Sprint | grounded | crouching];

            Vector3d terminal = new Vector3d(0, stats.TerminalV * (Mode == States.Walk ? -1 : ((space ? 1 : 0) - (shift ? 1 : 0))), 0);

            if (forward != 0 || strafe != 0)
            {
                terminal.Xz = Matrix2d.CreateRotation(-Direction.Y) * new Vector2d(strafe, -forward).Normalized() * stats.TerminalH;
            }

            Vector3d friction = stats.ApplyTime(deltaTime);

            Vector3d move = (velocity - terminal) * (friction - new Vector3d(1, 1, 1)) * stats.Log + terminal * deltaTime;

            velocity = (velocity - terminal) * friction + terminal;

            Move(move, world, shift);

            if (Mode == States.Walk)
            {
                HandleJump(space, deltaTime);
            }

            UpdateFOV(deltaTime);
        }

        private void Move(Vector3d delta, World world, bool shift)
        {
            if (Mode == States.Ghost)
            {
                Position += delta;
            }
            else
            {
                double height = crouching == States.Crouch ? CROUCHHEIGHT : HEIGHT;

                if (grounded == States.Grounded && shift)
                {
                    Vector3d checkPos = Position + new Vector3d(delta.X, -0.625, delta.Z);
                    AABB check = hitbox + new Vector3d(delta.X, -0.625, delta.Z);
                    check.Round(10);

                    bool collision = false;
                    foreach (var AABB in world.SurroundingAABBs(checkPos, checkPos, WIDTH, height))
                    {
                        if (check.Intersects(AABB, true, true, true))
                        {
                            collision = true;
                            break;
                        }
                    }
                    
                    if (!collision)
                    {
                        double padding = WIDTH / 2 - 0.00001;

                        Vector2d clamped = new Vector2d(Math.Clamp(checkPos.X, onAABB.Min.X - padding, onAABB.Max.X + padding),
                                                        Math.Clamp(checkPos.Z, onAABB.Min.Z - padding, onAABB.Max.Z + padding));

                        if (clamped.X != checkPos.X)
                        {
                            velocity.X = 0;
                        }
                        if (clamped.Y != checkPos.Z)
                        {
                            velocity.Z = 0;
                        }

                        delta.Xz = clamped - Position.Xz;
                    }
                }

                List<AABB> AABBs = world.SurroundingAABBs(Position, Position + delta, WIDTH, height);

                grounded = States.Airborne;

                onAABB = new AABB();

                while (delta.LengthSquared > 0)
                {
                    double weight = 1;
                    Vector3i axis = new Vector3i(0, 0, 0);
                    double axisMove = 0;
                    
                    double groundHeight = -1;
                    double groundArea = 0;

                    if (delta.X != 0)
                    {
                        double playerPos = delta.X > 0 ? hitbox.Max.X : hitbox.Min.X;

                        foreach (var candidate in AABBs)
                        {
                            double candidatePos = delta.X > 0 ? candidate.Min.X : candidate.Max.X;
                            double t = (candidatePos - playerPos) / delta.X;
                            if (t >= 0 && t < 1 && t < weight)
                            {
                                AABB moved = hitbox + delta * t;
                                if (moved.Intersects(candidate, false, true, true))
                                {
                                    weight = t;
                                    axis = new Vector3i(1, 0, 0);
                                    axisMove = candidatePos + (delta.X > 0 ? -WIDTH / 2 : WIDTH / 2);
                                }
                            }
                        }
                    }
                    if (delta.Y != 0)
                    {
                        double playerPos = delta.Y > 0 ? hitbox.Max.Y : hitbox.Min.Y;

                        foreach (var candidate in AABBs)
                        {
                            double candidatePos = delta.Y > 0 ? candidate.Min.Y : candidate.Max.Y;
                            double t = (candidatePos - playerPos) / delta.Y;
                            if (t >= 0 && t < 1 && t <= weight)
                            {
                                AABB moved = hitbox + delta * t;
                                if (moved.Intersects(candidate, true, false, true))
                                {
                                    weight = t;
                                    axis = new Vector3i(0, 1, 0);
                                    axisMove = candidatePos + (delta.Y > 0 ? -height : 0);

                                    if (delta.Y < 0)
                                    {
                                        if (candidatePos > groundHeight)
                                        {
                                            groundHeight = candidatePos;
                                            groundArea = moved.AreaXZ(candidate);
                                            onAABB = candidate;
                                        }
                                        else if (candidatePos == groundHeight)
                                        {
                                            double area = moved.AreaXZ(candidate);
                                            if (area > groundArea)
                                            {
                                                groundArea = area;
                                                onAABB = candidate;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (delta.Z != 0)
                    {
                        double playerPos = delta.Z > 0 ? hitbox.Max.Z : hitbox.Min.Z;

                        foreach (var candidate in AABBs)
                        {
                            double candidatePos = delta.Z > 0 ? candidate.Min.Z : candidate.Max.Z;
                            double t = (candidatePos - playerPos) / delta.Z;
                            if (t >= 0 && t < 1 && t < weight)
                            {
                                AABB moved = hitbox + delta * t;
                                if (moved.Intersects(candidate, true, true, false))
                                {
                                    weight = t;
                                    axis = new Vector3i(0, 0, 1);
                                    axisMove = candidatePos + (delta.Z > 0 ? -WIDTH / 2 : WIDTH / 2);
                                }
                            }
                        }
                    }

                    if (delta.Y < 0 && axis.Y == 1)
                    {
                        Mode = States.Walk;
                        grounded = States.Grounded;
                        onBlock = new Vector3i((int)Math.Floor(onAABB.Min.X), (int)Math.Floor(onAABB.Min.Y), (int)Math.Floor(onAABB.Min.Z));
                    }

                    //if (axis.X == 1 || axis.Z == 1) needs better implementation
                    //{
                    //    Sprint = States.NoSprint;
                    //}

                    Vector3d move = delta * weight;
                    Vector3i multiply = new Vector3i(1, 1, 1) - axis;

                    Position = (Vector3d)axis * axisMove + multiply * (Position + move);
                    UpdateHitbox();

                    delta -= move;
                    delta *= multiply;

                    velocity *= multiply;
                }
            }
        }

        private void HandleJump(bool space, double deltaTime)
        {
            if (jumpTimer > 0)
            {
                jumpTimer -= deltaTime;
                if (jumpTimer < 0)
                {
                    jumpTimer = 0;
                }
            }

            if (grounded == States.Grounded)
            {
                if (jumped)
                {
                    jumped = false;
                    jumpTimer = JUMPDELAY;
                }
                else if (space && jumpTimer == 0)
                {
                    jumped = true;
                    velocity.Y = JUMP;
                    grounded = States.Airborne;
                }
            }
        }

        private void HandleCrouch(bool shift, World world, double deltaTime)
        {
            if (Mode == States.Walk && shift)
            {
                crouching = States.Crouch;
            }
            else
            {
                crouching = States.NoCrouch;

                if (Mode != States.Ghost)
                {
                    UpdateHitbox();

                    foreach (var AABB in world.SurroundingAABBs(Position, Position, WIDTH, HEIGHT))
                    {
                        if (hitbox.Intersects(AABB, true, true, true))
                        {
                            crouching = States.Crouch;
                            break;
                        }
                    }
                }
            }

            UpdateHitbox();

            if (Mode == States.Walk && crouching == States.Crouch)
            {
                Sprint = States.NoSprint;
            }

            double weight = Math.Pow(1 - EYELEVELCHANGE, deltaTime * 10);
            eyeLevel = weight * eyeLevel + (1 - weight) * (crouching == States.Crouch ? CROUCHEYELEVEL : EYELEVEL);
        }

        private void UpdateHitbox()
        {
            hitbox = new AABB()
            {
                Min = new Vector3d(-WIDTH / 2, 0, -WIDTH / 2),
                Max = new Vector3d(WIDTH / 2, crouching == States.Crouch ? CROUCHHEIGHT : HEIGHT, WIDTH / 2),
            }
            + Position;
            hitbox.Round(10);
        }

        private void UpdateFOV(double deltaTime)
        {
            double ideal = BASEFOV + ((Sprint == States.Sprint ? 1 : 0) + (Mode == States.Fly ? 1 : 0) + (Mode == States.Ghost ? 1 : 0)) * FOVINCREASE;
            if (FOV < ideal)
            {
                FOV += FOVCHANGERATE * deltaTime;
                if (FOV > ideal)
                {
                    FOV = ideal;
                }
            }
            else if (FOV > ideal)
            {
                FOV -= FOVCHANGERATE * deltaTime;
                if (FOV < ideal)
                {
                    FOV = ideal;
                }
            }
        }

        public bool ValidPlace(IEnumerable<AABB> AABBs)
        {
            foreach (var AABB in AABBs)
            {
                if (hitbox.Intersects(AABB, true, true, true))
                {
                    return false;
                }
            }
            return true;
        }
    }

    struct MovementStats
    {
        public readonly double TerminalH;
        public readonly double TerminalV;
        public readonly double FrictionH;
        public readonly double FrictionV;

        public readonly Vector3d Log;

        public MovementStats(double terminalH, double frictionH, double terminalV, double frictionV)
        {
            TerminalH = terminalH;
            TerminalV = terminalV;
            FrictionH = Math.Pow(frictionH, 10);
            FrictionV = Math.Pow(frictionV, 10);

            Log = new Vector3d(1 / Math.Log(FrictionH), 1 / Math.Log(FrictionV), 1 / Math.Log(FrictionH));
        }

        public Vector3d ApplyTime(double deltaTime)
        {
            double horizontal = Math.Pow(FrictionH, deltaTime);
            double vertical = Math.Pow(FrictionV, deltaTime);
            return new Vector3d(horizontal, vertical, horizontal);
        }
    }
}
