using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Receiver.Modules.Visual;

public partial class GlitchOverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly DispatcherTimer _glitchTimer;
    private readonly Random _random = new();
    private volatile int _intensity = 5;
    private volatile int _speed = 5;

    private readonly Brush[] _glitchBrushes = new Brush[]
    {
        new SolidColorBrush(Color.FromArgb(80, 0, 255, 255)),   // Cyan
        new SolidColorBrush(Color.FromArgb(80, 255, 0, 128)),   // Magenta
        new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), // White
        new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),       // Dark scan slice
        new SolidColorBrush(Color.FromArgb(70, 0, 255, 60))     // Terminal Green
    };

    public GlitchOverlayWindow(int intensity = 5, int speed = 5)
    {
        InitializeComponent();

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        _intensity = Math.Clamp(intensity, 1, 10);
        _speed = Math.Clamp(speed, 1, 10);

        _glitchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(CalculateIntervalMs(_speed))
        };
        _glitchTimer.Tick += OnGlitchTick;
    }

    private static int CalculateIntervalMs(int speed) => Math.Max(15, 105 - (speed * 9));

    public void UpdateSettings(int intensity, int speed)
    {
        _intensity = Math.Clamp(intensity, 1, 10);
        _speed = Math.Clamp(speed, 1, 10);
        _glitchTimer.Interval = TimeSpan.FromMilliseconds(CalculateIntervalMs(_speed));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
        SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        _glitchTimer.Start();
    }

    private void OnGlitchTick(object? sender, EventArgs e)
    {
        GlitchCanvas.Children.Clear();

        double screenW = Width;
        double screenH = Height;

        int minBars = Math.Max(1, _intensity / 2);
        int maxBars = Math.Max(minBars + 1, _intensity * 2);
        int barCount = _random.Next(minBars, maxBars);

        for (int i = 0; i < barCount; i++)
        {
            double barHeight = _random.Next(4, 15 + (_intensity * 4));
            double barTop = _random.NextDouble() * (screenH - barHeight);
            double barLeft = _random.NextDouble() * (screenW * 0.3);
            double barWidth = _random.Next((int)(screenW * 0.2), (int)screenW);

            var rect = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = _glitchBrushes[_random.Next(_glitchBrushes.Length)],
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rect, barLeft);
            Canvas.SetTop(rect, barTop);
            GlitchCanvas.Children.Add(rect);
        }

        // Random scanline shift scaled by intensity
        ScanlineRect.Opacity = 0.03 + ((_intensity / 10.0) * 0.08) + (_random.NextDouble() * 0.08);
    }

    public void StopAndClose()
    {
        _glitchTimer.Stop();
        Close();
    }
}
