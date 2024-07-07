using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FBM_Simulate.Data;

namespace FBM_Simulate
{
    /// <summary>
    /// NoiseInfo.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NoiseInfo : UserControl
    {
        private PerlinNoise? m_CachedNoise;
        private bool m_Dirty;

        public bool Valid
        {
            get
            {
                // Scale은 0 초과
                if (!float.TryParse(ScaleX.Text.Trim(), out float scaleX) || scaleX <= 0) return false;
                if (!float.TryParse(ScaleY.Text.Trim(), out float scaleY) || scaleY <= 0) return false;
                if (!float.TryParse(Degree.Text.Trim(), out _)) return false;
                if (!float.TryParse(TranslateX.Text.Trim(), out _)) return false;
                if (!float.TryParse(TranslateY.Text.Trim(), out _)) return false;
                
                if (!int.TryParse(GridSize.Text.Trim(), out int gridSize)) return false;
                double log2Grid = Math.Log2(gridSize) - (int) Math.Log2(gridSize);
                if (log2Grid > 0.00001) return false; // gridSize가 2의 지수승이 아닌 경우
                if (gridSize is <= 0 or >= MainWindow.Resolution) return false; // gridSize가 정상 범위(1~resolution-1)를 벗어난 경우

                return true;
            }
        }
        
        public PerlinNoise Noise
        {
            get
            {
                if (m_CachedNoise == null || m_Dirty)
                {
                    m_CachedNoise = new PerlinNoise(int.Parse(GridSize.Text.Trim()), MainWindow.Seed);
                    m_CachedNoise.ApplyMeta(ApplyAbs.IsChecked!.Value, ApplyIvt.IsChecked!.Value);
                    m_CachedNoise.ApplyAffine(
                        float.Parse(TranslateX.Text.Trim()),
                        float.Parse(TranslateY.Text.Trim()),
                        float.Parse(Degree.Text.Trim()),
                        float.Parse(ScaleX.Text.Trim()),
                        float.Parse(ScaleY.Text.Trim())
                        );
                    m_Dirty = false;
                }

                return m_CachedNoise;
            }
        }
        
        public NoiseInfo()
        {
            InitializeComponent();

            m_Dirty = true;
            m_CachedNoise = null;
        }

        public void Select()
        {
            Window.Background = Brushes.LightGray;
        }

        public void Deselect()
        {
            Window.Background = Brushes.DarkGray;
        }

        public void MarkDirty()
        {
            m_Dirty = true;
        }

        private void OnInfoSelected(object sender, MouseButtonEventArgs e)
        {
            MainWindow.OnInfoSelected.Invoke(this);
        }

        private void ValidateNumberInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length <= 0) return;
            e.Handled = !(e.Text[0] == '.' || (e.Text[0] >= '0' && e.Text[0] <= '9'));
        }

        private void OnChangedInput(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) return;
            m_Dirty = true;
            ValidDisplay.Visibility = Valid ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnChangedCheckbox(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            m_Dirty = true;
            ValidDisplay.Visibility = Valid ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
