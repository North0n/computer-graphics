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
        public static IEnumerable<Vector4> TransformVertexes(ImageInfo info, double gridWidth, double gridHeight)
        {
            var result = new Vector4[info.Vertexes.Count];

            var translationMatrix = Matrix4x4.CreateTranslation(info.PositionX, info.PositionY, info.PositionZ);
            var rotationMatrix = Matrix4x4.CreateRotationX(info.RotationX) * Matrix4x4.CreateRotationY(info.RotationY);

            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)(gridWidth / gridHeight), 0.1f, 100);

            var matrix = rotationMatrix * translationMatrix * projectionMatrix;
            Parallel.ForEach(Partitioner.Create(0, info.Vertexes.Count), range =>
            {
                for (int i = range.Item1; i < range.Item2; ++i)
                {

                    result[i] = Vector4.Transform(info.Vertexes[i], matrix);
                    result[i] /= result[i].W;
                }
            });
            return result;
        }
    }
}