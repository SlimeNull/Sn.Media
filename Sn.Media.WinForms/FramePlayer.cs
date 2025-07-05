using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media.WinForms
{
    public class FramePlayer : Control
    {
        int _bufferWidth;
        int _bufferHeight;
        BufferedGraphics? _buffer;
        private bool _isPlaying;

        public IFrameStream? Source { get; set; }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
            }
        }

        public TimeSpan Position { get; set; }

        public FramePlayerStretch Stretch { get; set; }




    }
}
