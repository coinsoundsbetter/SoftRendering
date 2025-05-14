using System.Numerics;

namespace SoftRendering;

public partial class RenderingForm : Form {
    public RenderingForm()
    {
        InitializeComponent();
        DoubleBuffered = true;
        Width = 800;
        Height = 600;
    }

    /// <summary>
    /// 系统数据
    /// </summary>
    private Graphics g;

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
    
    protected override void OnPaint(PaintEventArgs e)
    {
        g = e.Graphics;
        g.Clear(Color.Black);

        SetRenderData();

        SetCamera();

        SetMVPMatrix();

        NDC();

        Draw();
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

    private void SetCamera() {
        cameraPos = new Vector3(0, 0, 5);
        fov = MathF.PI / 3;
        aspectRatio = (float)this.Width / this.Height;
        zNear = 0.1f;
        zFar = 100f;
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
        Vector3 cameraWorldPos = cameraPos;
        Vector3 cameraYAxis = new Vector3(0, 1, 0);
        Vector3 cameraZAxis = new Vector3(0, 0, 1);
        Vector3 cameraXAxis = Vector3.Cross(cameraYAxis, cameraZAxis);
        Matrix4x4 translation = Matrix4x4.CreateTranslation(-cameraWorldPos);
        Matrix4x4 rotation = new Matrix4x4 (
            cameraXAxis.X, cameraYAxis.X, cameraZAxis.X, 0,
            cameraXAxis.Y, cameraYAxis.Y, cameraZAxis.Y, 0,
            cameraXAxis.Z, cameraYAxis.Z, cameraZAxis.Z, 0,
            0, 0, 0, 1
        );
        
        return translation * rotation;
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