using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using ComputerGraphics.Models;
using ComputerGraphics.Services;

namespace ComputerGraphics;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly ImageInfo _positions = new()
        { PositionZ = 0, CameraTarget = new Vector3(0, 0, 0), CameraPosition = new Vector3(0, 0, 900), CamUp = new Vector3(0, 1, 0) };

    private List<List<int>> _faces;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        (_positions.Vertexes, _faces) = ObjFileParser.Parse(File.ReadLines("amogus.obj"));
        Draw();
    }

    private void Draw()
    {
        var vertexes = VertexTransformer.TransformVertexes(_positions, Grid.ActualWidth, Grid.ActualHeight).ToArray();
        var bitmap = PainterService.DrawModel(vertexes, _faces, (int)Grid.ActualWidth, (int)Grid.ActualHeight);
        PainterService.AddMinimapToBitmap(_positions, bitmap);
        Image.Source = bitmap.Source;
    }

    private void OnWindowKeydown(object sender, KeyEventArgs e)
    {
        const float rotationSpeed = 0.1f;
        const float moveSpeed = 10f;
        const float moveSpeedZ = 10f;
        switch (e.Key)
        {
            case Key.Left:
                _positions.RotationY -= rotationSpeed;
                break;
            case Key.Right:
                _positions.RotationY += rotationSpeed;
                break;
            case Key.Up:
                _positions.RotationX -= rotationSpeed;
                break;
            case Key.Down:
                _positions.RotationX += rotationSpeed;
                break;
            case Key.W:
                _positions.CameraPosition =
                    _positions.CameraPosition with { Y = _positions.CameraPosition.Y + moveSpeed };
                break;
            case Key.S:
                _positions.CameraPosition =
                    _positions.CameraPosition with { Y = _positions.CameraPosition.Y - moveSpeed };
                break;
            case Key.A:
                _positions.CameraPosition =
                    _positions.CameraPosition with { X = _positions.CameraPosition.X - moveSpeed };
                break;
            case Key.D:
                _positions.CameraPosition =
                    _positions.CameraPosition with { X = _positions.CameraPosition.X + moveSpeed };
                break;
            case Key.Q:
                _positions.CameraPosition =
                    _positions.CameraPosition with { Z = _positions.CameraPosition.Z - moveSpeedZ };
                break;
            case Key.E:
                _positions.CameraPosition =
                    _positions.CameraPosition with { Z = _positions.CameraPosition.Z + moveSpeedZ };
                break;
            case Key.Z:
                _positions.PositionX += moveSpeed;
                _positions.CameraTarget = new Vector3(_positions.PositionX, _positions.PositionY, _positions.PositionZ);
                break;
            case Key.C:
                _positions.PositionX -= moveSpeed;
                _positions.CameraTarget = new Vector3(_positions.PositionX, _positions.PositionY, _positions.PositionZ);
                break;
        }

        Draw();
    }
}