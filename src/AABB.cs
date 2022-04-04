using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace Minecraft_Clone
{
    struct AABB
    {
        public Vector3d Min;
        public Vector3d Max;

        public static AABB operator +(AABB left, Vector3d right)
        {
            return new AABB() { Min = left.Min + right, Max = left.Max + right };
        }
        public static AABB operator -(AABB left, Vector3d right)
        {
            return new AABB() { Min = left.Min - right, Max = left.Max - right };
        }

        public void Round(int dp)
        {
            Min = new Vector3d(
                Math.Round(Min.X, dp),
                Math.Round(Min.Y, dp),
                Math.Round(Min.Z, dp));
            Max = new Vector3d(
                Math.Round(Max.X, dp),
                Math.Round(Max.Y, dp),
                Math.Round(Max.Z, dp));
        }

        public bool Intersects(AABB other, bool x, bool y, bool z)
        {
            return (!x || (Max.X > other.Min.X && Min.X < other.Max.X))
                && (!y || (Max.Y > other.Min.Y && Min.Y < other.Max.Y))
                && (!z || (Max.Z > other.Min.Z && Min.Z < other.Max.Z));
        }

        public double AreaXZ(AABB other)
        {
            return Math.Round((Math.Min(Max.X, other.Max.X) - Math.Max(Min.X, other.Min.X))
                * (Math.Min(Max.Z, other.Max.Z) - Math.Max(Min.Z, other.Min.Z)), 10);
        }
    }
}
