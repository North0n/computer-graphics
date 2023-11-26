using ComputerGraphics.Models;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ComputerGraphics.extensions;

namespace ComputerGraphics.Services
{
    public static class VertexTransformer
    {
        private const float FieldOfView = MathF.PI / 8;
        private const float NearPlaneDistance = 0.1f;
        private const float FarPlaneDistance = 100;

        public static Vector3 ToOrthogonal(Vector3 v, Vector3 rotationPoint)
        {
            var r = v.X;
            var phi = v.Y;
            var zenith = v.Z;

            return new Vector3(r * Cos(zenith) * Sin(phi), r * Sin(zenith), -r * Cos(zenith) * Cos(phi)) + rotationPoint;

            float Sin(float a)
            {
                return (float)Math.Sin(a);
            }

            float Cos(float a)
            {
                return (float)Math.Cos(a);
            }
        }

        public static void TransformVertexes(ImageInfo info, double gridWidth, double gridHeight,
            Vector4[] resultVertexes, Vector4[] worldVertexes)
        {
            const float xMin = 0;
            const float yMin = 0;
            const float minDepth = 0;
            const float maxDepth = 1;

            var translationMatrix = Matrix4x4.CreateTranslation(info.PositionX, info.PositionY, info.PositionZ);
            var rotationMatrix = Matrix4x4.CreateRotationX(info.RotationX) * Matrix4x4.CreateRotationY(info.RotationY);
            var scaleMatrix = Matrix4x4.CreateScale(0.1f, 0.1f, 0.1f);

            // TODO If cam is right above or below target, than it doesn't see target, because Vector3.Normalize(info.CameraTarget - info.CameraPosition) == -info.CamUp;
            // How to fix that?
            var viewMatrix = Matrix4x4.CreateLookAt(ToOrthogonal(info.CameraPosition, info.CameraTarget),
                info.CameraTarget, info.CamUp);
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, (float)(gridWidth / gridHeight),
                NearPlaneDistance, FarPlaneDistance);
            var viewPortMatrix = Matrix4x4Extension.CreateViewportLeftHanded(xMin, yMin, (float)gridWidth,
                (float)gridHeight, minDepth, maxDepth);

            var toWorld = scaleMatrix * rotationMatrix * translationMatrix;
            Parallel.ForEach(Partitioner.Create(0, info.Vertexes.Count), range =>
            {
                for (var i = range.Item1; i < range.Item2; ++i)
                {
                    worldVertexes[i] = Vector4.Transform(info.Vertexes[i], toWorld);
                    resultVertexes[i] = Vector4.Transform(worldVertexes[i], viewMatrix * projectionMatrix);
                    resultVertexes[i] /= resultVertexes[i].W;
                    resultVertexes[i] = Vector4.Transform(resultVertexes[i], viewPortMatrix);
                }
            });
        }

        public static void TransformNormals(List<Vector3> normals, ImageInfo info, Vector3[] result)
        {
            if (normals.Count == 0)
                return;

            var rotationMatrix = Matrix4x4.CreateRotationX(info.RotationX) * Matrix4x4.CreateRotationY(info.RotationY);
            Parallel.ForEach(Partitioner.Create(0, normals.Count), range =>
            {
                for (var i = range.Item1; i < range.Item2; ++i)
                {
                    var vec = Vector4.Transform(normals[i], rotationMatrix);
                    result[i] = new Vector3(vec.X, vec.Y, vec.Z);
                }
            });
        }
    }
}