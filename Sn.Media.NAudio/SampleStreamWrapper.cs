using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class SampleStreamWrapper : ISampleStream
    {
        public IWaveProvider Source { get; }

        public SampleFormat Format { get; }

        public int SampleRate { get; }

        public int Channels { get; }

        public bool HasPosition => false;

        public bool CanSeek => false;

        public long Position => throw new InvalidOperationException();

        public SampleStreamWrapper(IWaveProvider source)
        {
            Source = source;

            if (source.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                Format = source.WaveFormat.BitsPerSample switch
                {
                    8 => SampleFormat.UInt8,
                    16 => SampleFormat.Int16,
                    32 => SampleFormat.Int32,
                    _ => throw new NotSupportedException($"Unsupported bits per sample for pcm encoding: {source.WaveFormat.BitsPerSample}"),
                };
            }
            else if (source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                Format = source.WaveFormat.BitsPerSample switch
                {
                    32 => SampleFormat.Float32,
                    _ => throw new NotSupportedException($"Unsupported bits per sample for float encoding: {source.WaveFormat.BitsPerSample}"),
                };
            }
            else
            {
                throw new NotSupportedException();
            }

            SampleRate = source.WaveFormat.SampleRate;
            Channels = source.WaveFormat.Channels;
        }

        public void Seek(long position)
        {
            throw new InvalidOperationException();
        }

        public int ReadSamples(byte[] buffer, int offset, int count)
        {
            return Source.Read(buffer, offset, count);
        }
    }
}
