using System;
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
    {
        PositionZ = 0, CameraTarget = new Vector3(0, 0, 0), CameraPosition = new Vector3(900, 0, 0),
        CamUp = new Vector3(0, 1, 0)
    };

    private List<List<int>> _faces;
    private List<Vector3> _normals;
    private List<List<int>> _normalIndexes;

    private bool _isMousePressed = false;
    private Point _pressPoint;

    private const float RotationSpeed = 0.1f;
    private const float MoveSpeed = 10f;
    private const float MoveSpeedZ = 10f;

    private float[,] _zBuffer;

    public MainWindow()
    {
        InitializeComponent();
        _zBuffer = new float[(int)Grid.ActualWidth, (int)Grid.ActualHeight];
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        (_positions.Vertexes, _faces, _normals, _normalIndexes) = ObjFileParser.Parse(File.ReadLines("amogus.obj"));
        Draw();
    }

    private void Draw()
    {
        var vertexes = VertexTransformer.TransformVertexes(_positions, Grid.ActualWidth, Grid.ActualHeight).ToArray();
        var normals = VertexTransformer.TransformNormals(_normals, _positions);
        var bitmap = PainterService.DrawModel(vertexes, _faces, (int)Grid.ActualWidth, (int)Grid.ActualHeight, _zBuffer,
            normals.ToArray(), _normalIndexes, Vector3.Normalize(_positions.CameraPosition - _positions.CameraTarget));
        PainterService.AddMinimapToBitmap(_positions, bitmap);
        Image.Source = bitmap.Source;
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _zBuffer = new float[(int)e.NewSize.Width, (int)e.NewSize.Height];
    }

    private void OnWindowKeydown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                _positions.RotationY -= RotationSpeed;
                break;
            case Key.Right:
                _positions.RotationY += RotationSpeed;
                break;
            case Key.Up:
                _positions.RotationX -= RotationSpeed;
                break;
            case Key.Down:
                _positions.RotationX += RotationSpeed;
                break;
            case Key.Z:
                _positions.PositionX += MoveSpeed;
                _positions.CameraTarget = new Vector3(_positions.PositionX, _positions.PositionY, _positions.PositionZ);
                break;
            case Key.C:
                _positions.PositionX -= MoveSpeed;
                _positions.CameraTarget = new Vector3(_positions.PositionX, _positions.PositionY, _positions.PositionZ);
                break;
        }

        Draw();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isMousePressed)
            return;

        var position = e.GetPosition(Image);
        var phiOffset = -(position.X - _pressPoint.X) * 0.005;
        var zenithOffset = (position.Y - _pressPoint.Y) * 0.002;

        _positions.CameraPosition =
            _positions.CameraPosition with { Y = _positions.CameraPosition.Y + (float)phiOffset };

        _positions.CameraPosition =
            _positions.CameraPosition with
            {
                Z = (float)Math.Clamp(_positions.CameraPosition.Z + (float)zenithOffset, -Math.PI / 2, Math.PI / 2)
            };

        _pressPoint = position;
        Draw();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isMousePressed = true;
        _pressPoint = e.GetPosition(Image);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isMousePressed = false;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _positions.CameraPosition = _positions.CameraPosition with { X = _positions.CameraPosition.X - 0.1f * e.Delta };
        Draw();
    }
}