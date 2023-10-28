using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ComputerGraphics.Models;
using ComputerGraphics.Services;

namespace ComputerGraphics;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    private readonly ImageInfo _positions = new()
    {
        PositionZ = 0, CameraTarget = new Vector3(0, 0, 0), CameraPosition = new Vector3(12, (float)Math.PI, 0),
        CamUp = new Vector3(0, 1, 0)
    };

    private List<Vector3> _normals;
    private List<Triangle> _triangles;

    private Vector4[] _transformedVertexes;
    private Vector3[] _transformedNormals;

    private bool _isMousePressed;
    private Point _pressPoint;

    private const float RotationSpeed = 0.1f;
    private const float MoveSpeed = 10f;

    private float[,] _zBuffer;

    private string _frameTime;

    public string FrameTime
    {
        get => _frameTime;
        private set
        {
            _frameTime = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        var handler = PropertyChanged;
        if (handler == null)
            return;

        var e = new PropertyChangedEventArgs(propertyName);
        handler(this, e);
    }

    private readonly Stopwatch _stopwatch = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _zBuffer = new float[(int)Grid.ActualWidth, (int)Grid.ActualHeight];
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        (_positions.Vertexes, _normals, _triangles) = ObjFileParser.Parse(File.ReadLines("amogus.obj"));
        _transformedVertexes = new Vector4[_positions.Vertexes.Count];
        _transformedNormals = new Vector3[_normals.Count];
        Draw();
    }

    private void Draw()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
        VertexTransformer.TransformVertexes(_positions, Grid.ActualWidth, Grid.ActualHeight, _transformedVertexes);
        VertexTransformer.TransformNormals(_normals, _positions, _transformedNormals);
        var viewPosition = Vector3.Normalize(_positions.CameraTarget -
                              VertexTransformer.ToOrthogonal(_positions.CameraPosition, _positions.CameraTarget));
        var bitmap = PainterService.DrawModel(_transformedVertexes, _transformedNormals, _triangles,
            (int)Grid.ActualWidth,
            (int)Grid.ActualHeight, _zBuffer,
            VertexTransformer.ToOrthogonal(_positions.CameraPosition, _positions.CameraTarget),
            viewPosition
        );
        PainterService.AddMinimapToBitmap(_positions, bitmap);
        Image.Source = bitmap.Source;
        _stopwatch.Stop();
        FrameTime = $"{_stopwatch.ElapsedMilliseconds}ms";
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
            case Key.W:
                _positions.PositionZ += MoveSpeed;
                _positions.CameraTarget = _positions.CameraTarget with
                {
                    Z = _positions.CameraTarget.Z + MoveSpeed
                };
                break;
            case Key.S:
                _positions.PositionZ -= MoveSpeed;
                _positions.CameraTarget = _positions.CameraTarget with
                {
                    Z = _positions.CameraTarget.Z - MoveSpeed
                };
                break;
            case Key.A:
                _positions.PositionX += MoveSpeed;
                _positions.CameraTarget = _positions.CameraTarget with
                {
                    X = _positions.CameraTarget.X + MoveSpeed
                };
                break;
            case Key.D:
                _positions.PositionX -= MoveSpeed;
                _positions.CameraTarget = _positions.CameraTarget with
                {
                    X = _positions.CameraTarget.X - MoveSpeed
                };
                break;
        }

        Draw();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isMousePressed)
            return;

        var position = e.GetPosition(Image);
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            var yOffset = (float)((position.Y - _pressPoint.Y) * 0.002);
            _positions.CameraTarget = _positions.CameraTarget with
            {
                Y = _positions.CameraTarget.Y + yOffset
            };
        }
        else
        {
            var phiOffset = (position.X - _pressPoint.X) * 0.005;
            var zenithOffset = (position.Y - _pressPoint.Y) * 0.002;

            _positions.CameraPosition = _positions.CameraPosition with
            {
                Y = _positions.CameraPosition.Y + (float)phiOffset,
                Z = (float)Math.Clamp(_positions.CameraPosition.Z + (float)zenithOffset, -Math.PI / 2 + 0.01,
                    Math.PI / 2 - 0.01)
            };
        }

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
        _positions.CameraPosition = _positions.CameraPosition with { X = _positions.CameraPosition.X - 0.005f * e.Delta };
        Draw();
    }
}