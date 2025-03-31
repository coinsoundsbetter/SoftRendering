using System.Diagnostics;
using System.Numerics;
using Timer = System.Windows.Forms.Timer;

namespace SoftRendering;

public partial class RenderingForm : Form {
    
    public RenderingForm() {
        InitializeComponent();
        InitRendering();
    }

    private int screenWidth = 1920;
    private int screenHeight = 1080;
    private Bitmap frameBuffer;
    private Graphics graphics;
    private Timer _timer; // 定时器控制绘制速度
    private void InitRendering() {
        Width = screenWidth;
        Height = screenHeight;
        graphics = CreateGraphics();
        frameBuffer = new Bitmap(screenWidth, screenHeight);
        Paint += OnPaint;
        _timer = new Timer();
        _timer.Interval = 1000;
        _timer.Tick += (sender, args) => {
            Invalidate();
        };
        _timer.Start();
    }

    private void OnPaint(object? sender, PaintEventArgs e) {
        ExecuteRendering();
    }
    
    private Vector3[] vertices = new[] {
        new Vector3(-1, 0, 0),  // 左下 v0
        new Vector3(0, 1, 0),   // 右下 v1
        new Vector3(1, 0, 0)     // 顶部 v2
    };
    
    private void ExecuteRendering() {
        frameBuffer = new Bitmap(screenWidth, screenHeight);
        graphics.Clear(Color.Black);
        
        // 定义摄像机
        Vector3 cameraPos = new Vector3(0, 0, -10);
        Vector3 cameraTarget = new Vector3(0, 0, 1);
        Vector3 cameraUp = Vector3.UnitY;
        // 创建视图矩阵：将世界空间中的顶点转换为摄像机空间
        Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPos, cameraTarget, cameraUp);
        
        // 计算透视投影矩阵
        float fov = MathF.PI / 3;
        float aspect = (float)screenWidth / screenHeight;
        float near = 0.3f;
        float far = 1000f;
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
        
        // 模型变换
        Matrix4x4 model = Matrix4x4.Identity;
        
        // 总的变换矩阵mvp
        Matrix4x4 transform = model * view * projection;
        
        // 绘制三角形
        Point[] screenPoints = new Point[3];
        for (int i = 0; i < 3; i++) {
            Vector4 vertex = new Vector4(vertices[i], 1);
            Vector4 transformed = Vector4.Transform(vertex, transform);
            Vector3 ndc = Utils.PerspectiveDivide(transformed);
            // 映射到屏幕坐标
            var pointX = (int)((ndc.X + 1) * screenWidth * 0.5f);
            var pointY = (int)((1 - ndc.Y) * screenHeight * 0.5f);
            screenPoints[i] = new Point(pointX, pointY);
        }
        graphics.DrawPolygon(Pens.White, screenPoints);
    }
}

public static class Utils {
    
    // 使用 Bresenham 画线
    public static void DrawLine(Bitmap bitmap, Point p0, Point p1)
    {
        int x0 = p0.X, y0 = p0.Y;
        int x1 = p1.X, y1 = p1.Y;

        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            if (x0 >= 0 && x0 < bitmap.Width && y0 >= 0 && y0 < bitmap.Height)
            {
                bitmap.SetPixel(x0, y0, Color.White);
            }
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
    
    public static Vector3 PerspectiveDivide(Vector4 v)
    {
        if (v.W != 0)
        {
            return new Vector3(v.X / v.W, v.Y / v.W, v.Z / v.W);
        }
        return new Vector3(v.X, v.Y, v.Z);
    }
    
    // 投影矩阵
    public static Vector4 MultiplyProjectionMatrix(Matrix4x4 proj, Vector4 v)
    {
        return new Vector4(
            proj.M11 * v.X + proj.M12 * v.Y + proj.M13 * v.Z + proj.M14 * v.W,
            proj.M21 * v.X + proj.M22 * v.Y + proj.M23 * v.Z + proj.M24 * v.W,
            proj.M31 * v.X + proj.M32 * v.Y + proj.M33 * v.Z + proj.M34 * v.W,
            proj.M41 * v.X + proj.M42 * v.Y + proj.M43 * v.Z + proj.M44 * v.W
        );
    }
    
    // 投影顶点到屏幕坐标
    public static Vector2 ProjectPoint(Vector3 point, Matrix4x4 rotation, Matrix4x4 projection,
        float width, float height)
    {
        // 先旋转
        Vector4 rotated = Vector4.Transform(new Vector4(point, 1.0f), rotation);
        // 再投影
        Vector4 projected = Vector4.Transform(rotated, projection);

        // 透视除法
        if (projected.W != 0)
        {
            projected.X /= projected.W;
            projected.Y /= projected.W;
        }

        // 映射到屏幕坐标
        float screenX = (projected.X * 0.5f + 0.5f) * width;
        float screenY = (1.0f - (projected.Y * 0.5f + 0.5f)) * height;
        
        // 边界检查，确保坐标在屏幕范围内
        screenX = Math.Max(0, Math.Min(width - 1, screenX));
        screenY = Math.Max(0, Math.Min(height - 1, screenY));
        
        return new Vector2(screenX, screenY);
    }
    
    // 透视投影矩阵
    public static Matrix4x4 CreatePerspectiveMatrix(float fov, float aspect, float near, float far)
    {
        float f = 1.0f / (float)Math.Tan(fov / 2);
    
        return new Matrix4x4(
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, far / (near - far), (2 * near * far) / (near - far),
            0, 0, -1, 0
        );
    }
    
    // 正交投影矩阵
    public static Matrix4x4 CreateOrthographicMatrix(float left, float right, float bottom, float top, float near, float far)
    {
        return new Matrix4x4(
            2 / (right - left), 0, 0, -(right + left) / (right - left),
            0, 2 / (top - bottom), 0, -(top + bottom) / (top - bottom),
            0, 0, -2 / (far - near), -(far + near) / (far - near),
            0, 0, 0, 1
        );
    }
    
    // 罗德里格旋转公式:计算三维空间中，一个向量绕旋转轴旋转给定角度以后得到的新向量的计算公式
    public static Matrix4x4 GetRotationMatrix(Vector3 axis, float angle) {
        float radians = angle * (float)Math.PI / 180.0f;
        float cosTheta = (float)Math.Cos(radians);
        float sinTheta = (float)Math.Sin(radians);
        float oneMinusCosTheta = 1 - cosTheta;

        float x = axis.X, y = axis.Y, z = axis.Z;

        return new Matrix4x4(
            cosTheta + x * x * oneMinusCosTheta, x * y * oneMinusCosTheta - z * sinTheta, x * z * oneMinusCosTheta + y * sinTheta, 0,
            y * x * oneMinusCosTheta + z * sinTheta, cosTheta + y * y * oneMinusCosTheta, y * z * oneMinusCosTheta - x * sinTheta, 0,
            z * x * oneMinusCosTheta - y * sinTheta, z * y * oneMinusCosTheta + x * sinTheta, cosTheta + z * z * oneMinusCosTheta, 0,
            0, 0, 0, 1
        );
    }

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
        SetLine(frameBuffer, a, b, color);
        SetLine(frameBuffer, b, c, color);
        SetLine(frameBuffer, c, a, color);
    }
}