using System.Diagnostics;
using System.Numerics;
using Timer = System.Windows.Forms.Timer;

namespace SoftRendering;

public partial class RenderingForm : Form {
    
    /// <summary>
    /// Y往下越大, X往右越大
    /// </summary>
    public RenderingForm() {
        InitializeComponent();
        InitRendering();
    }

    private int screenWidth;
    private int screenHeight;
    private Bitmap screenBuffer;
    private Graphics screenGraphics;
    private Timer refreshTimer;
    private void InitRendering() {
        Width = 1980;
        Height = 1080;
        screenWidth = Width;
        screenHeight = Height;
        screenGraphics = CreateGraphics();
        screenBuffer = new Bitmap(screenWidth, screenHeight);
        refreshTimer = new Timer();
        refreshTimer.Interval = 1000;
        refreshTimer.Tick += Refresh;
        refreshTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        refreshTimer.Stop();
        refreshTimer.Dispose();
        base.OnFormClosed(e);
    }
    
    private void Refresh(object? sender, EventArgs e) {
        var triangle = new Vector2[] {
            new Vector2(10, 70),
            
            new Vector2(70, 80),
            new Vector2(50, 160),
        };
        Rendering.SetTriangle(screenBuffer, 
            triangle[0], triangle[1], triangle[2], 
            Color.Blue);
        screenGraphics.DrawImage(screenBuffer, 0, 0);
    }
}

public static class Rendering {

    /// <summary>
    /// 绘制线段
    /// </summary>
    public static void SetLine(Bitmap frameBuffer, Vector2 a, Vector2 b, Color c) {
        for (float t = 0f; t < 1.0f; t += 0.01f) {
            int x = (int)(a.X + (b.X - a.X) * t);
            int y = (int)(a.Y + (b.Y - a.Y) * t);
            frameBuffer.SetPixel(x, y, c);
        }
    }

    /// <summary>
    /// 绘制三角形
    /// </summary>
    public static void SetTriangle(Bitmap frameBuffer, Vector2 a, Vector2 b, Vector2 c, Color color) {
        // 升序排序，最后让三个点从上到下排序
        if (a.Y > b.Y) {
            (a.Y, b.Y) = (b.Y, a.Y);
        }
        if (a.Y > c.Y) {
            (a.Y, c.Y) = (c.Y, a.Y);
        }
        if (b.Y > c.Y) {
            (b.Y, c.Y) = (c.Y, b.Y);
        }
        Debug.WriteLine($"{a}, {b}, {c}");
        
        SetLine(frameBuffer, a, b, color);
        SetLine(frameBuffer, b, c, color);
        SetLine(frameBuffer, c, a, color);
    }
}