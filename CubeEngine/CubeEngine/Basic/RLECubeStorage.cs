using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubeEngine.Basic
{
    public class RLECubeStorage : ICubeStorage
    {
        /// <summary>
        /// Contains a column of run-length encoded cubes and also lighting 
        /// values for any transparent cubes that touch any opaque cubes.
        /// </summary>
        private struct RLEColumn
        {
            public Cube[] Cubes;
            public CubeLight[] Lighting;
        }

        /// <summary>
        /// A 2D array containing any RLE columns that have been placed into
        /// memory through disk lookup or generation via noise.
        /// </summary>
        private RLEColumn[,] _columns;

        /// <summary>
        /// Instead of modulus to find the remainder of an index it's possible
        /// to use the binary & operator if and only if the divisor is a power 
        /// of 2.  ie. i%2^n == i&(2^n-1) for positives and for negatives if
        /// i%2^n + 2^n the negative remainder gets shifted by 2^n.  These store
        /// the value 2^n-1.
        /// </summary>
        private int _maskX, _maskZ;

        public RLECubeStorage(int xLen, int zLen)
        {
            if (!Utilities.MathI.IsPowerOf2(xLen)) throw new NotSupportedException("X dimension is not a power of 2.");
            if (!Utilities.MathI.IsPowerOf2(zLen)) throw new NotSupportedException("Z dimension is not a power of 2.");

            _maskX = xLen - 1;
            _maskZ = zLen - 1;
        }

        public void  GetMaterialAt(int x, int y, int z, out CubeMaterial material)
        {
            x = x & _maskX;
            z = z & _maskZ;

            RLEColumn column = _columns[x, z];

            int stride = 256;
            for (int i = column.Cubes.Length-1; i >= 0; i--)
            {
                stride -= column.Cubes[i].Run;
                if (stride <= y)
                {
                    material = column.Cubes[i].Material;
                    break;
                }
            }

            material = CubeMaterial.None;
        }

        public void SetMaterialAt(int x, int y, int z, CubeMaterial material)
        {
            x = x & _maskX;
            z = z & _maskZ;

            RLEColumn column = _columns[x, z];

            int stride = 256;
            for (int i = column.Cubes.Length - 1; i >= 0; i--)
            {
                stride -= column.Cubes[i].Run;
                //TODO: Make an algo to set a cube in the RLE scheme
            }


        }
    }
}
