using System.Diagnostics;
using System.Numerics;
using Timer = System.Windows.Forms.Timer;

namespace SoftRendering;

public partial class RenderingForm : Form {
    
    Vector3[] vertices;
    int[] indices;
    Color[] faceColors;
    Vector2[] projected;

    float yaw = 0f, pitch = 0f;
    Vector3 cameraPosition = new Vector3(0, 0, -10);
    float moveSpeed = 0.1f;

    Point lastMousePos;
    bool isDragging = false;

    public RenderingForm()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.Width = 800;
        this.Height = 600;

        SetupCube();

        this.MouseDown += (s, e) => { isDragging = true; lastMousePos = e.Location; };
        this.MouseUp += (s, e) => { isDragging = false; };
        this.MouseMove += (s, e) =>
        {
            if (isDragging)
            {
                float dx = e.X - lastMousePos.X;
                float dy = e.Y - lastMousePos.Y;
                yaw += dx * 0.01f;
                pitch -= dy * 0.01f;
                pitch = Math.Clamp(pitch, -1.5f, 1.5f);
                lastMousePos = e.Location;
                Invalidate();
            }
        };

        this.KeyDown += Form1_KeyDown;
    }

    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        Vector3 forward = Vector3.Normalize(new Vector3(
            (float)(Math.Cos(yaw) * Math.Cos(pitch)),
            (float)(Math.Sin(pitch)),
            (float)(Math.Sin(yaw) * Math.Cos(pitch))
        ));

        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

        if (e.KeyCode == Keys.W)
            cameraPosition += forward * moveSpeed;
        if (e.KeyCode == Keys.S)
            cameraPosition -= forward * moveSpeed;
        if (e.KeyCode == Keys.A)
            cameraPosition -= right * moveSpeed;
        if (e.KeyCode == Keys.D)
            cameraPosition += right * moveSpeed;
        if (e.KeyCode == Keys.Space)
            cameraPosition += up * moveSpeed;
        if (e.KeyCode == Keys.ShiftKey)
            cameraPosition -= up * moveSpeed;

        Invalidate();
    }

    private void SetupCube()
    {
        vertices = new[]
        {
            new Vector3(-1, -1, -1),
            new Vector3( 1, -1, -1),
            new Vector3( 1,  1, -1),
            new Vector3(-1,  1, -1),
            new Vector3(-1, -1,  1),
            new Vector3( 1, -1,  1),
            new Vector3( 1,  1,  1),
            new Vector3(-1,  1,  1),
        };

        indices = new int[]
        {
            0,1,2, 0,2,3, // front
            5,4,7, 5,7,6, // back
            4,0,3, 4,3,7, // left
            1,5,6, 1,6,2, // right
            3,2,6, 3,6,7, // top
            4,5,1, 4,1,0  // bottom
        };

        faceColors = new[]
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Orange,
            Color.Purple,
            Color.Yellow
        };

        projected = new Vector2[vertices.Length];
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Color.Black);

        float aspect = (float)Width / Height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 3f, aspect, 0.1f, 100f);

        Matrix4x4 model = Matrix4x4.Identity;

        Vector3 forward = new Vector3(
            (float)(Math.Cos(yaw) * Math.Cos(pitch)),
            (float)(Math.Sin(pitch)),
            (float)(Math.Sin(yaw) * Math.Cos(pitch))
        );
        Vector3 target = cameraPosition + forward;

        Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, target, Vector3.UnitY);
        Matrix4x4 vp = model * view * projection;

        // Project all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector4 transformed = Vector4.Transform(new Vector4(vertices[i], 1), vp);

            // 透视除法
            transformed /= transformed.W;

            projected[i] = new Vector2(
                (transformed.X + 1f) * 0.5f * Width,
                (1f - transformed.Y) * 0.5f * Height
            );
        }

        // Draw cube faces with backface culling
        for (int i = 0; i < indices.Length; i += 6)
        {
            Color faceColor = faceColors[i / 6];
            for (int j = 0; j < 6; j += 3)
            {
                int i0 = indices[i + j];
                int i1 = indices[i + j + 1];
                int i2 = indices[i + j + 2];

                // Transform to world space
                Vector3 v0 = Vector3.Transform(vertices[i0], model);
                Vector3 v1 = Vector3.Transform(vertices[i1], model);
                Vector3 v2 = Vector3.Transform(vertices[i2], model);

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                Vector3 viewDir = Vector3.Normalize(v0 - cameraPosition);

                if (Vector3.Dot(normal, viewDir) >= 0)
                    continue;

                Vector2 a = projected[i0];
                Vector2 b = projected[i1];
                Vector2 c = projected[i2];

                PointF[] tri = new PointF[]
                {
                    new PointF(a.X, a.Y),
                    new PointF(b.X, b.Y),
                    new PointF(c.X, c.Y)
                };

                using SolidBrush brush = new SolidBrush(faceColor);
                g.FillPolygon(brush, tri);
                g.DrawPolygon(Pens.Black, tri);
            }
        }

        // Draw world axis
        DrawLine(g, Vector3.Zero, new Vector3(2, 0, 0), Color.Red, vp);   // X
        DrawLine(g, Vector3.Zero, new Vector3(0, 2, 0), Color.Green, vp); // Y
        DrawLine(g, Vector3.Zero, new Vector3(0, 0, 2), Color.Blue, vp);  // Z
    }

    void DrawLine(Graphics g, Vector3 p1, Vector3 p2, Color color, Matrix4x4 vp)
    {
        Vector2? Project(Vector3 v)
        {
            Vector4 t = Vector4.Transform(new Vector4(v, 1), vp);
            if (t.W <= 0) return null;
            t /= t.W;
            return new Vector2(
                (t.X + 1f) * 0.5f * Width,
                (1f - t.Y) * 0.5f * Height
            );
        }

        var s = Project(p1);
        var e = Project(p2);

        if (s.HasValue && e.HasValue)
        {
            using Pen pen = new Pen(color, 2);
            g.DrawLine(pen, s.Value.X, s.Value.Y, e.Value.X, e.Value.Y);
        }
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