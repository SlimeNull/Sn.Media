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
using Sn.Media.OpenCvSharp4;
using Sn.Media.Tests;

namespace MediaPlayerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SamplePlayer _samplePlayer = new SamplePlayer();

        public MainWindow()
        {
            InitializeComponent();
        }
        public void PlaySampleStream(ISampleStream sampleStream)
        {

        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            framePlayer.Source = new Sn.Media.SdcbFFmpeg.MediaFileFrameStream(@"C:\Users\Xavier\Videos\2025-02-07 12-58-36.mkv").AsNonSeekable();
            framePlayer.IsPlaying = true;

            _samplePlayer.Source = new MediaFoundationSampleStream(@"C:\Users\Xavier\Videos\2025-02-07 12-58-36.mkv");
            _samplePlayer.IsPlaying = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            framePlayer.IsPlaying = false;
            _samplePlayer.IsPlaying = false;
        }
    }
}