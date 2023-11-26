using System.Numerics;

namespace ComputerGraphics.Models;

public struct LightSource
{
    public LightSource(Vector3 position, Vector3 lightColor, float intensity)
    {
        Position = position;
        Color = lightColor;
        Intensity = intensity;
    }

    public Vector3 Position { get; set; }
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }
}