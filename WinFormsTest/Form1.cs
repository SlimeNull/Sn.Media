using Sn.Media;
using Sn.Media.NAudio;
using Sn.Media.WinForms;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        FramePlayer _framePlayer = new FramePlayer();
        SamplePlayer _samplePlayer = new SamplePlayer();

        public Form1()
        {
            InitializeComponent();
            _framePlayer.Dock = DockStyle.Fill;

            Controls.Add(_framePlayer);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _framePlayer.Source = new Sn.Media.SdcbFFmpeg.MediaFileFrameStream(@"D:\CloudMusic\MV\bbbb.mp4");
            _framePlayer.Position = default;
            _framePlayer.IsPlaying = true;

            _samplePlayer.Source = new BufferedSampleStream(new MediaFoundationSampleStream(@"D:\CloudMusic\MV\bbbb.mp4"));
            _samplePlayer.IsPlaying = true;
        }
    }
}
