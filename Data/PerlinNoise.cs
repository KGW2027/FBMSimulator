using System.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FBM_Simulate.Data;

public class PerlinNoise
{
    private readonly Random? m_Random;
    private float[,] m_Noise;
    private readonly float[,][]? m_Grid;
    private readonly int m_GridSize;
    private readonly bool m_IsReadOnly;

    public PerlinNoise()
    {
        m_Noise = new float[MainWindow.Resolution, MainWindow.Resolution];
        m_IsReadOnly = true;
    }
    
    private PerlinNoise(PerlinNoise copy)
    {
        m_Noise = new float[copy.m_Noise.GetLength(0), copy.m_Noise.GetLength(1)];
        m_IsReadOnly = true;
    }

    public PerlinNoise(int gridSize, int seed)
    {
        m_Random = new Random(seed);
        m_Noise = new float[MainWindow.Resolution, MainWindow.Resolution];
        m_Grid = new float[MainWindow.Resolution / gridSize + 1, MainWindow.Resolution / gridSize + 1][];
        m_GridSize = gridSize;
        m_IsReadOnly = false;
        
        DefineGrid();
        DrawNoise();
    }
    
    public static PerlinNoise operator+(PerlinNoise lhs, PerlinNoise rhs)
    {
        if (lhs.m_Noise.GetLength(0) != rhs.m_Noise.GetLength(1) ||
            lhs.m_Noise.GetLength(1) != rhs.m_Noise.GetLength(1))
            throw new Exception("PerlinNoise의 + 연산자는 같은 크기의 노이즈 끼리만 연산 가능합니다.");

        PerlinNoise newNoise = new PerlinNoise(lhs);
        
        for (int y = 0; y < lhs.m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < lhs.m_Noise.GetLength(0); x++)
            {
                newNoise.m_Noise[x, y] = lhs.m_Noise[x, y] + rhs.m_Noise[x, y];
            }
        }

        return newNoise;
    }

    public void Clean()
    {
        for (int y = 0; y < m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < m_Noise.GetLength(0); x++)
            {
                m_Noise[x, y] = 0;
            }
        }
    }

    private void DefineGrid()
    {
        if (m_IsReadOnly) throw new ReadOnlyException("읽기 전용 PerlinNoise 객체에서 DefineGrid를 호출하였습니다.");
        for (int y = 0; y < m_Grid!.GetLength(1); y++)
        {
            for (int x = 0; x < m_Grid.GetLength(0); x++)
            {            
                float angle = (float)(m_Random!.NextDouble() * 2.0 * Math.PI);
                m_Grid[x, y] = new[] {(float)Math.Cos(angle), (float)Math.Sin(angle)};
            }
        }
    }

    private void DrawNoise()
    {
        if (m_IsReadOnly) throw new ReadOnlyException("읽기 전용 PerlinNoise 객체에서 DrawNoise를 호출하였습니다.");
        for (int y = 0; y < m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < m_Noise.GetLength(0); x++)
            {
                int gridX = x / m_GridSize;
                int gridY = y / m_GridSize;
            
                float lt = DotGradient(m_Grid, gridX    , gridY    , x, y);
                float rt = DotGradient(m_Grid, gridX + 1, gridY    , x, y);
                float lb = DotGradient(m_Grid, gridX    , gridY + 1, x, y);
                float rb = DotGradient(m_Grid, gridX + 1, gridY + 1, x, y);

                float lxInterp = Fade((float) (x % m_GridSize) / m_GridSize);
                float tyInterp = Fade((float) (y % m_GridSize) / m_GridSize);

                float txInterp = Lerp(lt, rt, lxInterp);
                float bxInterp = Lerp(lb, rb, lxInterp);
                float yInterp = Lerp(txInterp, bxInterp, tyInterp);

                m_Noise[x, y] = yInterp;
            }
        }
    }
    
    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }
    
    private float DotGradient(float[,][]? grid, int gx, int gy, int x, int y)
    {
        float[] gradient = grid![gx, gy];
        float dx = x - gx * m_GridSize;
        float dy = y - gy * m_GridSize;
        return dx * gradient[0] + dy * gradient[1];
    }

    public void ApplyMeta(bool abs = false, bool invert = false, int pow = 1)
    {
        float maxValue = float.MinValue, minValue = float.MaxValue;
        
        for (int y = 0; y < m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < m_Noise.GetLength(0); x++)
            {
                if (abs) m_Noise[x, y] = Math.Abs(m_Noise[x, y]);
                if (pow > 1) m_Noise[x, y] = (float) Math.Pow(m_Noise[x, y], pow);
                if (invert)
                {
                    minValue = minValue > m_Noise[x, y] ? m_Noise[x, y] : minValue;
                    maxValue = maxValue < m_Noise[x, y] ? m_Noise[x, y] : maxValue;
                }
            }
        }

        if (!invert) return;
        for (int y = 0; y < m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < m_Noise.GetLength(0); x++)
            {
                m_Noise[x, y] = maxValue + minValue - m_Noise[x, y];
            }
        }
    }

    private int Wrap(int value)
    {
        int result = value % MainWindow.Resolution;
        if (result < 0) result += MainWindow.Resolution;
        return result;
    }
    
    /// <summary>
    /// <para> Scale > Rotate > Translate 순서로 적용 </para>
    /// <para> NewPosition = Translate * Rotate * Scale * CurrentPosition </para>
    /// </summary>
    public void ApplyAffine(float translateX = 0, float translateY = 0, float rotateDeg = 0, float scaleX = 1, float scaleY = 1)
    {
        float[,] output = new float[m_Noise.GetLength(0), m_Noise.GetLength(1)];
        float rad = (float) (rotateDeg * Math.PI / 180.0f);
        
        for (int y = 0; y < output.GetLength(1); y++)
        {
            for (int x = 0; x < output.GetLength(0); x++)
            {
                int newX = Wrap((int) Math.Round(Math.Cos(rad) * scaleX * x - Math.Sin(rad) * scaleY * y + translateX));
                int newY = Wrap((int) Math.Round(Math.Sin(rad) * scaleX * x + Math.Cos(rad) * scaleY * y + translateY));
                output[x, y] = m_Noise[newX, newY];
            }
        }

        m_Noise = output;
    }

    public WriteableBitmap LoadBitmap()
    {
        // Normalize Data 
        float minValue = float.MaxValue, maxValue = float.MinValue;
        for (int y = 0; y < m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < m_Noise.GetLength(0); x++)
            {
                minValue = minValue > m_Noise[x, y] ? m_Noise[x, y] : minValue;
                maxValue = maxValue < m_Noise[x, y] ? m_Noise[x, y] : maxValue;
            }
        }
        
        WriteableBitmap bitmap = new WriteableBitmap(MainWindow.Resolution, MainWindow.Resolution, 96, 96, PixelFormats.Gray8, null);
        byte[] pixels = new byte[MainWindow.Resolution * MainWindow.Resolution];
        for (int y = 0; y < m_Noise.GetLength(1); y++)
        {
            for (int x = 0; x < m_Noise.GetLength(0); x++)
            {
                // Normalize
                m_Noise[x, y] = (m_Noise[x, y] - minValue) / (maxValue - minValue) * 2.0f - 1;
                // Draw Image
                byte gray = (byte) ((m_Noise[x, y] + 1) / 2.0f * 255);
                pixels[y * MainWindow.Resolution + x] = gray;
            }
        }

        Int32Rect rect = new Int32Rect(0, 0, MainWindow.Resolution, MainWindow.Resolution);
        bitmap.WritePixels(rect, pixels, MainWindow.Resolution, 0);
        return bitmap;
    }
}