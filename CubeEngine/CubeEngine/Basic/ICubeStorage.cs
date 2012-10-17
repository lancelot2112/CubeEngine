using System;

namespace CubeEngine.Basic
{
    interface ICubeStorage
    {
        void GetMaterialAt(int x, int y, int z, out CubeMaterial material);
        void SetMaterialAt(int x, int y, int z, CubeMaterial material);
    }
}
