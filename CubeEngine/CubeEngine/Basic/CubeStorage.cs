using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    public class CubeStorage
    {
        public Cube[, ,] Cubes;

        private int _maxX;
        private int _maxY;
        private int _maxZ;

        public CubeStorage(int x, int y, int z)
        {
            Cubes = new Cube[x, y, z];
        }
    }
}
