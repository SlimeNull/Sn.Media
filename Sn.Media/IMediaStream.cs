using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public interface IMediaStream
    {
        public SampleFormat SampleFormat { get; }
        public FrameFormat FrameFormat { get; }

        public int SampleRate { get; }
        public int Channels { get; }

        public Fraction FrameRate { get; }
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public int FrameStride { get; }
        public int FrameDataSize { get; }

        public bool CanSeek { get; }
        public bool HasDuration { get; }

        public long SamplesPosition { get; }
        public TimeSpan Duration { get; }


        public void Seek(TimeSpan time);

        public MediaStreamReadResult Read(
            Span<byte> bufferForSamples,
            Span<byte> bufferForFrame,
            out TimeSpan frameTime);
    }
}
