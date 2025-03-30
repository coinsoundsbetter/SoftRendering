using System.Diagnostics;
using System.Numerics;
using Timer = System.Windows.Forms.Timer;

namespace SoftRendering;

public partial class RenderingForm : Form {
    
    public RenderingForm() {
        InitializeComponent();
        InitRendering();
    }

    private int screenWidth = 700;
    private int screenHeight = 700;
    private Bitmap frameBuffer;
    private Graphics graphics;
    private Timer _timer; // 定时器控制绘制速度
    private void InitRendering() {
        Width = screenWidth;
        Height = screenHeight;
        graphics = CreateGraphics();
        graphics.TranslateTransform(0, this.ClientSize.Height);
        graphics.ScaleTransform(1, -1);
        frameBuffer = new Bitmap(screenWidth, screenHeight);
        Paint += OnPaint;
        _timer = new Timer();
        _timer.Interval = 1000; // 30 FPS，大约每 33 毫秒更新一次
        _timer.Tick += (sender, args) => {
            //Invalidate();
        };
        _timer.Start();
    }

    private void OnPaint(object? sender, PaintEventArgs e) {
        ExecuteRendering();
    }
    
    private Vector3 v0 = new Vector3(0, 0, 0);
    private Vector3 v1 = new Vector3(10, 0, 0);
    private Vector3 v2 = new Vector3(10, 5, 0);
    private void ExecuteRendering() {
        frameBuffer = new Bitmap(screenWidth, screenHeight);
        graphics.Clear(Color.Black);
        
        // 在这里，我们尝试旋转一个三角形
        // 1.旋转三角形的各个顶点
        // 2.进行透视投影，将3D坐标转换为2D屏幕坐标
        // 3.绘制

        // 顶点旋转矩阵
        var rotationMatrix = Utils.GetRotationMatrix(new Vector3(0, 1, 0), 360);
        // 更新顶点坐标
        v0 = Vector3.Transform(v0, rotationMatrix);
        v1 = Vector3.Transform(v1, rotationMatrix);
        v2 = Vector3.Transform(v2, rotationMatrix);
        // 透视投影矩阵
        float fov = (float)(Math.PI / 4);
        float aspect = (float)screenWidth / (float)screenHeight;
        float near = 1.0f;
        float far = 10.0f;
        Matrix4x4 projectionMatrix = Utils.CreatePerspectiveMatrix(fov, aspect, near, far);
        // 将顶点转换到屏幕坐标
        var p0 = Utils.ProjectPoint(v0, rotationMatrix, 
            projectionMatrix, screenWidth, screenHeight);
        var p1 = Utils.ProjectPoint(v1, rotationMatrix,
            projectionMatrix, screenWidth, screenHeight);
        var p2 = Utils.ProjectPoint(v2, rotationMatrix,
            projectionMatrix, screenWidth, screenHeight);
        var triangle = Utils.GetTriangle(p0, p1, p2);
        Utils.SetTriangle(frameBuffer, triangle.one, triangle.two, triangle.three, Color.White);
        graphics.DrawImage(frameBuffer, 0, 0);
    }
}

public struct TriangleVector2
{
    public Vector2 one;
    public Vector2 two;
    public Vector2 three;

    public TriangleVector2(Vector2 one, Vector2 two, Vector2 three)
    {
        this.one = one;
        this.two = two;
        this.three = three;
    }
}

public struct TriangleVector3 {
    public Vector3 one;
    public Vector3 two;
    public Vector3 three;
    
    public TriangleVector3(Vector3 one, Vector3 two, Vector3 three)
    {
        this.one = one;
        this.two = two;
        this.three = three;
    }
}

public static class Utils {

    public static TriangleVector3 GetTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        // 计算叉积
        float crossProduct = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        // 如果叉积小于零，说明是逆时针排列，需要交换顺序
        if (crossProduct < 0)
        {
            return new TriangleVector3(a, c, b); // 交换 b 和 c
        }

        // 否则，已经是顺时针排列
        return new TriangleVector3(a, b, c);
    }
    
    public static TriangleVector2 GetTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        // 计算 2D 叉积
        float crossProduct = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        // 如果叉积小于零，说明是逆时针排列，需要交换顺序
        if (crossProduct < 0)
        {
            return new TriangleVector2(a, c, b); // 交换 b 和 c
        }

        // 否则，已经是顺时针排列
        return new TriangleVector2(a, b, c);
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
    
    // 透视投影矩阵（将 3D 坐标转换到 2D 屏幕）
    public static Matrix4x4 CreatePerspectiveMatrix(float fov, float aspect, float near, float far)
    {
        float f = 1.0f / (float)Math.Tan(fov / 2);
        return new Matrix4x4(
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, far / (near - far), -1,
            0, 0, (near * far) / (near - far), 0);
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