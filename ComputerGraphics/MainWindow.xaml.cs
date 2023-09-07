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
public partial class MainWindow : Window
{
    private float PositionX { get; set; }
    private float PositionY { get; set; }
    private float PositionZ { get; set; } = -70;
    private float RotationX { get; set; }
    private float RotationY { get; set; }

    private List<Vector3> _vertexes;
    private List<List<int>> _faces;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        (_vertexes, _faces) = ObjFileParser.Parse(File.ReadLines("amogus.obj"));
        Draw();
    }

    private void Draw()
    {
        var bitmap = PainterService.DrawModel(
            VertexTransformer.TransformVertexes(
                new ImageInfo(PositionX, PositionY, PositionZ, RotationX, RotationY, _vertexes),
                Grid.ActualWidth, Grid.ActualHeight).ToArray(), _faces,
            (int)Grid.ActualWidth, (int)Grid.ActualHeight);
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
                RotationY -= rotationSpeed;
                break;
            case Key.Right:
                RotationY += rotationSpeed;
                break;
            case Key.Up:
                RotationX -= rotationSpeed;
                break;
            case Key.Down:
                RotationX += rotationSpeed;
                break;
            case Key.W:
                PositionY += moveSpeed;
                break;
            case Key.S:
                PositionY -= moveSpeed;
                break;
            case Key.A:
                PositionX -= moveSpeed;
                break;
            case Key.D:
                PositionX += moveSpeed;
                break;
            case Key.Q:
                PositionZ -= moveSpeedZ;
                break;
            case Key.E:
                PositionZ += moveSpeedZ;
                break;
            case Key.Z:
                RotationY -= rotationSpeed;
                PositionX -= moveSpeed;
                RotationX -= rotationSpeed;
                break;
            case Key.C:
                RotationY += rotationSpeed;
                PositionX += moveSpeed;
                RotationX += rotationSpeed;
                break;
        }

        Draw();
    }
}