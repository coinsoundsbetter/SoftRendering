using System.Numerics;
using Timer = System.Windows.Forms.Timer;

namespace SoftRendering;

public partial class RenderingForm : Form {
    public RenderingForm()
    {
        InitializeComponent();
        DoubleBuffered = true;
        Width = 800;
        Height = 600;
        Init();
    }

    /// <summary>
    /// 系统数据
    /// </summary>
    private Graphics g;
    private Timer renderTimer;
    
    /// <summary>
    /// 渲染数据
    /// </summary>
    private Vector3[] vertices;
    private int[] indices;
    private Color[] faceColors;

    /// <summary>
    /// 相机参数
    /// </summary>
    private Vector3 cameraPos;
    private Vector3 cameraUp;
    private Vector3 cameraForward;
    private Vector3 cameraRight => Vector3.Cross(cameraUp, cameraForward);
    private float cameraSpeed;
    private float fov;
    private float aspectRatio;
    private float zNear;
    private float zFar;

    /// <summary>
    /// MVP变换矩阵
    /// </summary>
    private Matrix4x4 mvpMatrix;

    /// <summary>
    /// NDC
    /// </summary>
    private Vector2[] projected;

    /// <summary>
    /// 设备输入
    /// </summary>
    private int forwardInput;
    private int rightInput;
    private int upInput;
    private HashSet<Keys> pressedKeys = new();

    protected override void OnKeyDown(KeyEventArgs e) {
        pressedKeys.Add(e.KeyCode);
    }

    protected override void OnKeyUp(KeyEventArgs e) {
        pressedKeys.Remove(e.KeyCode);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        g = e.Graphics;
        g.Clear(Color.Black);

        SetInputData();
        
        SetCameraState();

        SetRenderData();

        SetMVPMatrix();

        NDC();

        Draw();
    }

    private void SetInputData() {
        if (pressedKeys.Contains(Keys.W)) {
            forwardInput = 1;
        }else if (pressedKeys.Contains(Keys.S)) {
            forwardInput = -1;
        }else {
            forwardInput = 0;
        }

        if (pressedKeys.Contains(Keys.D)) {
            rightInput = 1;
        }else if (pressedKeys.Contains(Keys.A)) {
            rightInput = -1;
        }else {
            rightInput = 0;
        }

        if (pressedKeys.Contains(Keys.Q)) {
            upInput = 1;
        }else if (pressedKeys.Contains(Keys.E)) {
            upInput = -1;
        }else {
            upInput = 0;
        }
    }
    
    private void SetRenderData() {
        vertices = [
            new Vector3(-1, 0, -1),
            new Vector3(1, 0, -1),
            new Vector3(0, 0, 1),
            new Vector3(0, 1.5f, 0)
        ];

        indices = [
            0, 1, 2,
            0, 1, 3,
            1, 2, 3,
            2, 0, 3
        ];

        faceColors = [
            Color.Gray,
            Color.Red,
            Color.Green,
            Color.Blue
        ];
    }

    private void Init() {
        // 定义摄像机
        cameraPos = new Vector3(0, 0, 0);
        cameraForward = new Vector3(0, 0, 1);
        cameraUp = new Vector3(0, 1, 0);
        
        // 定义刷新频率
        renderTimer = new Timer();
        renderTimer.Interval = 16;
        renderTimer.Tick += (sender, args) => {
            Invalidate();
        };
        renderTimer.Start();
    }

    private void SetCameraState() {
        fov = MathF.PI / 3;
        aspectRatio = (float)this.Width / this.Height;
        zNear = 0.1f;
        zFar = 100f;
        cameraSpeed = 0.02f;
        cameraPos += cameraForward * forwardInput * cameraSpeed;
        cameraPos += cameraUp * upInput * cameraSpeed;
        cameraPos += cameraRight * rightInput * cameraSpeed;
        Console.WriteLine(forwardInput);
    }

    private void SetMVPMatrix() {
        var m = CalculateModelMatrix(Vector3.Zero);
        var v = CalculateViewMatrix();
        var p = CalculateProjection();
        mvpMatrix = m * v * p;
    }

    private void NDC() {
        projected = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) {
            var transformed = Vector4.Transform(new Vector4(vertices[i], 1), mvpMatrix);
            transformed /= transformed.W;

            if (transformed.Z < 0 || transformed.Z > 1) {
                continue;
            }
            
            projected[i] = new Vector2(
                (transformed.X + 1f) * 0.5f * Width,
                (1f - transformed.Y) * 0.5f * Height
            );
        }
    }

    private void Draw() {
        for (int i = 0; i < indices.Length; i+=3) {
            int i0 = indices[i];
            int i1 = indices[i + 1];
            int i2 = indices[i + 2];
            
            Vector2 a = projected[i0];
            Vector2 b = projected[i1];
            Vector2 c = projected[i2];

            PointF[] tri = [
                new PointF(a.X, a.Y),
                new PointF(b.X, b.Y),
                new PointF(c.X, c.Y)
            ];
            
            using SolidBrush brush = new SolidBrush(faceColors[i / 3]);
            g.FillPolygon(brush, tri);
        }
    }

    private Matrix4x4 CalculateModelMatrix(Vector3 translation) {
        return Matrix4x4.CreateTranslation(translation);
    }
    
    private Matrix4x4 CalculateViewMatrix() {
        Vector3 zAxis = Vector3.Normalize(cameraForward);                    // Forward（Z+ 方向）
        Vector3 xAxis = Vector3.Normalize(Vector3.Cross(cameraUp, zAxis));  // Right  = Up × Forward
        Vector3 yAxis = Vector3.Normalize(Vector3.Cross(zAxis, xAxis));     // Up     = Forward × Right
        Matrix4x4 rotation = new Matrix4x4(
            xAxis.X, yAxis.X, zAxis.X, 0,
            xAxis.Y, yAxis.Y, zAxis.Y, 0,
            xAxis.Z, yAxis.Z, zAxis.Z, 0,
            0,       0,       0,       1
        );

        Matrix4x4 translation = Matrix4x4.CreateTranslation(-cameraPos);

        return rotation * translation;
    }
    
    private Matrix4x4 CalculateProjection() {
        float yScale = 1f / MathF.Tan(fov / 2f);
        float xScale = yScale / aspectRatio;
        float zRange = zFar - zNear;

        return new Matrix4x4(
            xScale, 0, 0, 0,
            0, yScale, 0, 0,
            0, 0, zFar / zRange, 1,
            0, 0, -zNear * zFar / zRange, 0
        );
    }
}