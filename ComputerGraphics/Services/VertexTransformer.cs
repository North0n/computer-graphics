using ComputerGraphics.Models;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace ComputerGraphics.Services
{
    public static class VertexTransformer
    {
        private const float FieldOfView = MathF.PI / 2;
        private const float NearPlaneDistance = 0.1f;
        private const float FarPlaneDistance = 100;

        public static IEnumerable<Vector4> TransformVertexes(ImageInfo info, double gridWidth, double gridHeight)
        {
            const float xMin = 0;
            const float yMin = 0;

            var result = new Vector4[info.Vertexes.Count];

            var translationMatrix = Matrix4x4.CreateTranslation(info.PositionX, info.PositionY, info.PositionZ);
            var rotationMatrix = Matrix4x4.CreateRotationX(info.RotationX) * Matrix4x4.CreateRotationY(info.RotationY);

            var viewMatrix = Matrix4x4.CreateLookAt(info.CameraPosition, info.CameraTarget, info.CamUp);
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, (float)(gridWidth / gridHeight),
                NearPlaneDistance, FarPlaneDistance);


            var viewPortMatrix = new Matrix4x4((float)gridWidth / 2, 0, 0, 0,
                0, -((float)gridHeight / 2), 0, 0,
                0, 0, 1024, 0,
                xMin + (float)gridWidth / 2, yMin + (float)gridHeight / 2, 0, 1);

            var matrix = rotationMatrix * translationMatrix * viewMatrix * projectionMatrix * viewPortMatrix;
            Parallel.ForEach(Partitioner.Create(0, info.Vertexes.Count), range =>
            {
                for (var i = range.Item1; i < range.Item2; ++i)
                {
                    result[i] = Vector4.Transform(info.Vertexes[i], matrix);
                    result[i] /= result[i].W;
                }
            });
            return result;
        }
    }
}