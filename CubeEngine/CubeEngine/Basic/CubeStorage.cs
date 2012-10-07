using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    public class CubeStorage
    {
        public Cube[, ,] Cubes;

        private int _maskX;
        private int _lenY;
        private int _maskZ;

        public CubeStorage(int x, int y, int z)
        {
            bool xp2 = (x != 0) && ((x & (x - 1)) == 0);
            bool zp2 = (z != 0) && ((z & (z - 1)) == 0);

            if (!xp2) throw new NotSupportedException("X dimension is not a power of 2.");
            if (!zp2) throw new NotSupportedException("Z dimension is not a power of 2.");

            Cubes = new Cube[x, y, z];
            _maskX = x - 1;
            _lenY = y;
            _maskZ = z - 1;
        }

        public void GetCube(int x, int y, int z, out Cube cube)
        {
            //Wrap coords so that the array is a 3d circular queue
            x = x & _maskX;
            z = z & _maskZ;

            cube = Cubes[x, y, z];
        }

        public void SafeGetCube(int x, int y, int z, out Cube cube)
        {
            if (y < 0 || y >= _lenY)
            {
                cube = Cube.NULL;
                return;
            }

            //Wrap coords so that the array is a 3d circular queue
            x = x & _maskX;
            z = z & _maskZ;

            cube = Cubes[x, y, z];
        }
        public void SetCube(int x, int y, int z, ref Cube cube)
        {
            //Wrap coords so that the array is a 3d circular queue
            x = x & _maskX;
            z = z & _maskZ;

            Cubes[x, y, z] = cube;
        }

        public void SetSunlight(int x, int y, int z, int sun)
        {
            //Wrap coords so that the array is a 3d circular queue
            x = x & _maskX;
            z = z & _maskZ;
            
            sun = sun - Cubes[x,y,z].Opacity;
            Cubes[x, y, z].SunLight = (sun < 0) ? 0 : sun;
        }

        public int GetSunlight(int x, int y, int z)
        {
            x = x & _maskX;
            z = z & _maskZ;

            return Cubes[x, y, z].SunLight;
        }

        public void GetCubeNeighbors(int x, int y, int z, out Cube curr, out Cube posX, out Cube negX, out Cube posY, out Cube negY, out Cube posZ, out Cube negZ)
        {
            x = x & _maskX;
            z = z & _maskZ;

            curr = Cubes[x, y, z];

            int reg = (x + 1) & _maskX;
            posX = Cubes[reg, y, z];
            reg = (x - 1) & _maskX;
            negX = Cubes[reg, y, z];
            reg = y + 1;
            posY = (reg < _lenY) ? Cubes[x, reg, z] : Cube.NULL;
            reg = y - 1;
            negY = (reg >= 0) ? Cubes[x, reg, z] : Cube.NULL;
            reg = (z + 1) & _maskZ;
            posZ = Cubes[x, y, reg];
            reg = (z - 1) & _maskZ;
            negZ = Cubes[x, y, reg];
        }

        public void WrapCoords(ref int x, ref int z)
        {
            //x = x % _maskX;
            //if (x < 0) x += _maskX;

            //z = z % _maskZ;
            //if (z < 0) z += _maskZ;

            x = x & _maskX;
            z = z & _maskZ;
        }

    }
}
