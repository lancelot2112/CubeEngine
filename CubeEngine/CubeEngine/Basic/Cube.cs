using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace CubeEngine.Basic
{

    public struct CubeLight
    {
        /// <summary>
        /// Contains the light levels of the surrounding faces.
        /// Bits 1-4 Sunlight [0,15]
        ///     to accesss use val & 15
        /// Bits 5-8 LocalLight [0,15]
        ///     to access use (val & 240) >> 4
        /// </summary>
        public byte LightLevels;
        /// <summary>
        /// Stores the individual color channel values so that 
        /// colored lights can be used.
        /// </summary>
        public byte Red, Green, Blue;
        /// <summary>
        /// Vertical offset in the 'voxel' column, since continuous 
        /// data is not kept and position can't be inferred by the 
        /// position in the array.
        /// </summary>
        public byte Offset;
        /// <summary>
        /// Cube faces 'registered' to this light value.
        /// Bit 1 +x (+x cube but -x face of that cube) val & 1
        /// Bit 2 -x (-x cube but +x face of that cube) val & 2
        /// Bit 3 +y (+y cube but -y face of that cube) val & 4
        /// Bit 4 -y (-y cube but +y face of that cube) val & 8
        /// Bit 5 +z (+z cube but -z face of that cube) val & 16
        /// Bit 6 -z (-z cube but +z face of that cube) val & 32
        /// </summary>
        public byte SidesFlag;
    }

    public enum CubeMaterial : byte
    {
        None = 0,
        Dirt,
        Grass,
        Stone
    }
    /// <summary>
    /// Contains information for a Run-Length encoding scheme
    /// </summary>
    public struct Cube
    {
        /// <summary>
        /// Material of the cube will be used to access cube metadata 
        /// and determine how to render.
        /// </summary>
        public CubeMaterial Material;
        /// <summary>
        /// Number of consecutive cubes of this Material.  Used in the 
        /// Run-Length Encoding scheme for each cube column.
        /// </summary>
        public Byte Run;

        public Cube(CubeMaterial material, byte run)
        {
            this.Material = material;
            this.Run = run;
        }

        public bool IsTransparent { get { return Material == 0; } }

        public override string ToString()
        {
            return "m: " + Material.ToString() + " | run: " + Run.ToString();
        }

        /// <summary>
        /// NULL cube containing 0 Material
        /// </summary>
        public static Cube NULL = new Cube();
    }
}
