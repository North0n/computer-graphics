using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ComputerGraphics.Models;
using ComputerGraphics.Services;
using Microsoft.Win32;

namespace ComputerGraphics;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    private readonly ImageInfo _positions = new()
    {
        PositionZ = 0, CameraTarget = new Vector3(0, 0, 0), CameraPosition = new Vector3(4, (float)Math.PI, 0),
        CamUp = new Vector3(0, 1, 0)
    };

    private List<Vector3> _normals;
    private List<Vector3> _textures;
    private List<Triangle> _triangles;

    private Vector4[] _transformedVertexes;
    private Vector4[] _worldVertexes;
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
        LoadFile("Shovel Knight/shovel_low.obj");
        Draw();
    }

    private void LoadFile(string fileName)
    {
        (_positions.Vertexes, _textures, _normals, _triangles) = ObjFileParser.Parse(fileName);
        _transformedVertexes = new Vector4[_positions.Vertexes.Count];
        _worldVertexes = new Vector4[_positions.Vertexes.Count];
        _transformedNormals = new Vector3[_normals.Count];
    }

    private const float CoeffY = -4;
    private const float CoeffD = 2;

    private static readonly LightSource[] LightSources = {
        new(Vector3.Zero, new(1f, 1f, 1f), 20f),
        // new(new(CoeffD, -CoeffY, CoeffD), new(1, 0, 0), 80f),
        // new(new(-CoeffD, -CoeffY, CoeffD), new(0, 1, 0), 80f),
        // new(new(CoeffD, -CoeffY, -CoeffD), new(0, 0, 1), 80f),
        // new(new(-CoeffD, -CoeffY, -CoeffD), new(1, 1, 1), 80f),
    };

    private void Draw()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
        LightSources[0].Position = VertexTransformer.ToOrthogonal(_positions.CameraPosition, _positions.CameraTarget);

        VertexTransformer.TransformVertexes(_positions, Grid.ActualWidth, Grid.ActualHeight, _transformedVertexes, _worldVertexes);
        VertexTransformer.TransformNormals(_normals, _positions, _transformedNormals);
        var viewDirection = Vector3.Normalize(_positions.CameraTarget -
                              VertexTransformer.ToOrthogonal(_positions.CameraPosition, _positions.CameraTarget));
        var bitmap = PainterService.DrawModel(_transformedVertexes, _worldVertexes, _transformedNormals, _textures, _triangles,
            (int)Grid.ActualWidth, (int)Grid.ActualHeight, _zBuffer, LightSources, viewDirection);
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
            case Key.O:
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "Object Files|*.obj"
                    };
                    var opened = openFileDialog.ShowDialog();
                    if (opened != null && opened.Value)
                    {
                        LoadFile(openFileDialog.FileName);
                        Draw();
                    }
                }
                break;
            case Key.Q:
            case Key.E:
                LightSources[0].Intensity += (e.Key == Key.E ? 1 : -1) * 1;
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
            var yOffset = (float)((position.Y - _pressPoint.Y) * 0.001);
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
        _positions.CameraPosition = _positions.CameraPosition with { X = _positions.CameraPosition.X - 0.001f * e.Delta };
        Draw();
    }
}