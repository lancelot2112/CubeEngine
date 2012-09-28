using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    /// <summary>
    /// Handles coordinate wrapping through use of a wrap distance value and a max height value.  This allows for automatic coordinate transformations and figuring out other neighboring chunks.
    /// </summary>
    public struct ChunkCoords
    {
        public int X;
        public int Z;
        public int WrapDistance;

        public ChunkCoords(int x, int z, int wrapDistance)
        {
            this.X = x;
            this.Z = z;
            this.WrapDistance = wrapDistance;
        }

        public void AddX(int x)
        {
            int temp = X + x;
            if (Math.Abs(temp) > WrapDistance) X = -temp;
            else X = temp;
        }

        public void AddZ(int z)
        {
            int temp = Z + z;
            if (Math.Abs(temp) > WrapDistance) Z = -temp;
            else Z = temp;
        }

        public ChunkCoords GetShiftedX(int x)
        {
            int temp = X + x;
            if (Math.Abs(temp) > WrapDistance) temp = -temp;
            return new ChunkCoords(temp, Z, WrapDistance);
        }

        public ChunkCoords GetShiftedZ(int z)
        {
            int temp = Z + z;
            if (Math.Abs(temp) > WrapDistance) temp = -temp;
            return new ChunkCoords(X, temp, WrapDistance);
        }

        public byte Neighbors(ChunkCoords coord2)
        {
            int diffX = coord2.X - X;
            int diffZ = coord2.Z - Z;

            if (diffZ == 0 && diffX == 1) return 1;
            else if ( diffZ == 0 && diffX == -1) return 2;
            else if ( diffX == 0 && diffZ == 1) return 4;
            else if ( diffX == 0 && diffZ == -1) return 8;            
            else return 0;
        }

        public int CompareDistance(ChunkCoords other, ChunkCoords origin)
        {
            int dist = Distance(ref origin);
            int otherdist = other.Distance(ref origin);

            return dist.CompareTo(otherdist);

        }

        public int Distance(ref ChunkCoords other)
        {
            int x = other.X - X;
            int z = other.Z - Z;
            return (int)Math.Sqrt(x * x + z * z);
        }

        public static bool operator == (ChunkCoords coords1, ChunkCoords coords2)
        {
            return (coords1.X == coords2.X) && (coords1.Z == coords2.Z);
        }

        public static bool operator != (ChunkCoords coords1, ChunkCoords coords2)
        {
            return !((coords1.X == coords2.X) && (coords1.Z == coords2.Z));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
