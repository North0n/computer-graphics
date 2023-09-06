using System.Collections.Generic;
using System.Numerics;

namespace ComputerGraphics.Models
{
    public class ImageInfo
    {
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public List<Vector3> Vertexes { get; set; }
    }
}