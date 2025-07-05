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
            framePlayer.Source = new BufferedFrameStream(new VideoFileFrameStream(@"D:\CloudMusic\MV\李宗盛 - 山丘.mp4"));
            framePlayer.IsPlaying = true;

            _samplePlayer.Source = new AudioFileSampleStream(@"D:\CloudMusic\MV\李宗盛 - 山丘.mp4");
            _samplePlayer.IsPlaying = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            framePlayer.IsPlaying = false;
            _samplePlayer.IsPlaying = false;
        }
    }
}