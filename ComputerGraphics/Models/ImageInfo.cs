using System.Collections.Generic;
using System.Numerics;

namespace ComputerGraphics.Models
{
    public class ImageInfo
    {
        public ImageInfo(float positionX, float positionY, float positionZ, float rotationX, float rotationY, List<Vector3> vertexes)
        {
            PositionX = positionX;
            PositionY = positionY;
            PositionZ = positionZ;
            RotationX = rotationX;
            RotationY = rotationY;
            Vertexes = vertexes;
        }

        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public List<Vector3> Vertexes { get; set; }
    }
}