using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FBM_Simulate.Data;

namespace FBM_Simulate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int Resolution = 1024;

        private static MainWindow? SelfReference;

        private readonly ObservableCollection<NoiseInfo> m_NoiseInfos;
        private NoiseInfo? m_SelectedNoiseInfo;
        private PerlinNoise m_DisplayNoise;

        public delegate void InfoSelected(NoiseInfo noiseInfo);

        public static InfoSelected OnInfoSelected = delegate { };

        public static int Seed { 
            get => SelfReference == null ? 0 : int.Parse(SelfReference.SeedInput.Text);
            private set
            {
                if (SelfReference == null) return;
                SelfReference.SeedInput.Text = value.ToString();
                foreach (NoiseInfo noiseInfo in SelfReference.m_NoiseInfos)
                    noiseInfo.MarkDirty();
            }
        }

    public MainWindow()
        {
            SelfReference = this;
            
            InitializeComponent();

            m_NoiseInfos = new ObservableCollection<NoiseInfo>();
            m_SelectedNoiseInfo = null;
            m_DisplayNoise = new PerlinNoise();
            OnInfoSelected += SelectInfo;
            
            m_NoiseInfos.CollectionChanged += (_, args) =>
            {
                if (args.NewItems is {Count: > 0})
                {
                    foreach (object newElement in args.NewItems)
                    {
                        NoiseInfoPanel.Children.Add((NoiseInfo) newElement);
                    }
                }

                if (args.OldItems is {Count: > 0})
                {
                    List<UIElement> removes = NoiseInfoPanel.Children.Cast<UIElement>().Where(element => args.OldItems.Contains(element)).ToList();
                    removes.ForEach(removeTarget => NoiseInfoPanel.Children.Remove(removeTarget));
                }
            };
        }

        private void SelectInfo(NoiseInfo noiseInfo)
        {
            m_SelectedNoiseInfo?.Deselect();
            m_SelectedNoiseInfo = noiseInfo;
            m_SelectedNoiseInfo.Select();
        }

        private void CreateNewNoiseInfo(object sender, RoutedEventArgs e)
        {
            m_NoiseInfos.Add(new NoiseInfo());
        }

        private void RemoveSelectedNoiseInfo(object sender, RoutedEventArgs e)
        {
            if (m_SelectedNoiseInfo == null) return;
            m_NoiseInfos.Remove(m_SelectedNoiseInfo);
            m_SelectedNoiseInfo = null;
        }

        private void EnableWindowMove(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        
        private void ExitProgram(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void RenderNoise(object sender, RoutedEventArgs e)
        {
            m_DisplayNoise.Clean();
            foreach (NoiseInfo noiseInfo in m_NoiseInfos)
            {
                if (!noiseInfo.Valid) continue;
                
                m_DisplayNoise += noiseInfo.Noise;
            }
            
            m_DisplayNoise.ApplyMeta(ApplyAbs.IsChecked!.Value, ApplyIvt.IsChecked!.Value, int.Parse(PowInput.Text));
            
            NoiseDisplay.Source = m_DisplayNoise.LoadBitmap();
        }

        private void RandomSeed(object sender, RoutedEventArgs e)
        {
            Seed = new Random().Next();
        }

        private void ValidateIntegerInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }
}