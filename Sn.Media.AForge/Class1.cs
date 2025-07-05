namespace Sn.Media.AForge
{
    public class VideoFileFrameStream : IFrameStream
    {
        public FrameFormat Format => throw new NotImplementedException();

        public Fraction FrameRate => throw new NotImplementedException();

        public int FrameWidth => throw new NotImplementedException();

        public int FrameHeight => throw new NotImplementedException();

        public int FrameStride => throw new NotImplementedException();

        public int FrameDataSize => throw new NotImplementedException();

        public bool HasPosition => throw new NotImplementedException();

        public bool CanSeek => throw new NotImplementedException();

        public long Position => throw new NotImplementedException();

        public VideoFileFrameStream()
        {
            global::AForge.Video.AsyncVideoSource
        }

        public int ReadFrame(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Seek(long position)
        {
            throw new NotImplementedException();
        }
    }
}
