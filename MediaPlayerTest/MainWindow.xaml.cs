using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using Sn.Media;
using Sn.Media.NAudio;
using Sn.Media.Tests;

namespace MediaPlayerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveOutEvent? _currentPlayingWave;

        public MainWindow()
        {
            InitializeComponent();
        }
        public void PlaySampleStream(ISampleStream sampleStream)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPlayingWave?.Stop();
            framePlayer.Source = new BlinkFrameStream(200, 200, new ColorBgra(0, 0, 255, 255), new ColorBgra(0, 255, 0, 255), 2);
            framePlayer.IsPlaying = true;

            Task.Run(() =>
            {
                WaveOutEvent waveOutEvent = new WaveOutEvent();
                waveOutEvent.Init(new WaveProviderWrapper(new SineWaveSampleStream(SampleFormat.Float32, 44100, 1, 1000, 1, 10)));
                waveOutEvent.Play();

                _currentPlayingWave = waveOutEvent;
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            framePlayer.IsPlaying = false;
            _currentPlayingWave?.Stop();
        }
    }
}