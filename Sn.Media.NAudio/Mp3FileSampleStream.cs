using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class Mp3FileSampleStream : ISampleStream, IDisposable
    {
        private readonly Mp3FileReader _rawReader;
        private readonly WaveSampleStream _wrapper;

        public Mp3FileSampleStream(string filePath)
        {
            _rawReader = new Mp3FileReader(filePath);
            _wrapper = new WaveSampleStream(_rawReader);
        }

        public Mp3FileSampleStream(Stream mp3Stream)
        {
            _rawReader = new Mp3FileReader(mp3Stream);
            _wrapper = new WaveSampleStream(_rawReader);
        }

        public SampleFormat Format => ((ISampleStream)_wrapper).Format;

        public int SampleRate => ((ISampleStream)_wrapper).SampleRate;

        public int Channels => ((ISampleStream)_wrapper).Channels;

        public bool HasLength => ((ISampleStream)_wrapper).HasLength;

        public bool CanSeek => ((ISampleStream)_wrapper).CanSeek;

        public long Position => ((ISampleStream)_wrapper).Position;

        public long Length => ((ISampleStream)_wrapper).Length;

        public void Seek(long position)
        {
            ((ISampleStream)_wrapper).Seek(position);
        }

        public int Read(Span<byte> buffer)
        {
            return ((ISampleStream)_wrapper).Read(buffer);
        }

        public void Dispose()
        {
            ((IDisposable)_rawReader).Dispose();
        }
    }
}
