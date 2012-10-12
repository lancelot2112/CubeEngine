using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    /// <summary>
    /// Handles coordinate wrapping through use of a wrap distance value and a max height value.  
    /// This allows for automatic coordinate transformations and figuring out other neighboring chunks.
    /// </summary>
    public struct ChunkCoordinate
    {
        public int X;
        public int Z;
        public int WrapDistance;

        public ChunkCoordinate(int x, int z, int wrapDistance)
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

        public void GetShiftedX(int x, out ChunkCoordinate coords)
        {
            int temp = X + x;
            if (Math.Abs(temp) > WrapDistance) temp = -temp;
            coords.X = temp;
            coords.Z = Z;
            coords.WrapDistance = WrapDistance;
        }

        public void GetShiftedZ(int z, out ChunkCoordinate coords)
        {
            int temp = Z + z;
            if (Math.Abs(temp) > WrapDistance) temp = -temp;
            coords.X = X;
            coords.Z = temp;
            coords.WrapDistance = WrapDistance;
        }

        public void Add(int x, int z, out ChunkCoordinate coords)
        {
            x = X + x;
            z = Z + z;
            if (Math.Abs(x) > WrapDistance) x = -x;
            if (Math.Abs(z) > WrapDistance) z = -z;
            coords.X = x;
            coords.Z = z;
            coords.WrapDistance = WrapDistance;
        }

        public byte Neighbors(ref ChunkCoordinate coord2)
        {
            int diffX = coord2.X - X;
            int diffZ = coord2.Z - Z;

            if (diffZ == 0 && diffX == 1) return 1;
            else if (diffZ == 0 && diffX == -1) return 2;
            else if (diffX == 0 && diffZ == 1) return 4;
            else if (diffX == 0 && diffZ == -1) return 8;
            else if (diffX == 1 && diffZ == 1) return 16;
            else if (diffX == -1 && diffZ == 1) return 32;
            else if (diffX == 1 && diffZ == -1) return 64;
            else if (diffX == -1 && diffZ == -1) return 128;
            else return 0;
        }

        public int CompareDistance(ref ChunkCoordinate other, ref ChunkCoordinate origin)
        {
            int dist = DistanceSquared(ref origin);
            int otherdist = other.DistanceSquared(ref origin);

            return dist.CompareTo(otherdist);

        }

        public int Distance(ref ChunkCoordinate other)
        {
            int x = other.X - X;
            int z = other.Z - Z;
            return (int)Math.Sqrt(x * x + z * z);
        }

        public int DistanceSquared(ref ChunkCoordinate other)
        {
            int x = other.X - X;
            int z = other.Z - Z;
            return x * x + z * z;
        }

        public override string ToString()
        {
            return "{" + X.ToString() + "," + Z.ToString() + "}";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(ref ChunkCoordinate coords)
        {
            return (coords.X == X) && (coords.Z == Z) && (coords.WrapDistance == WrapDistance);
        }
    }
}
