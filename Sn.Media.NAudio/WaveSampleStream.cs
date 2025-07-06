using System.Buffers;
using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class WaveSampleStream : ISampleStream
    {
        private readonly int _channels;
        private readonly int _bytesPerSample;
        private readonly WaveStream? _sourceAsWaveStream;
        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Create();

        public IWaveProvider Source { get; }

        public SampleFormat Format { get; }

        public int SampleRate { get; }

        public int Channels { get; }

        public bool HasPosition => _sourceAsWaveStream is { };
        public bool HasLength => _sourceAsWaveStream is { };

        public bool CanSeek => _sourceAsWaveStream is { CanSeek: true };

        public long Position
        {
            get
            {
                if (_sourceAsWaveStream is not null)
                {
                    return _sourceAsWaveStream.Position / _bytesPerSample / _channels;
                }

                throw new InvalidOperationException();
            }
        }

        public long Length
        {
            get
            {
                if (_sourceAsWaveStream is not null)
                {
                    return _sourceAsWaveStream.Length / _bytesPerSample / _channels;
                }

                throw new InvalidOperationException();
            }
        }

        public WaveSampleStream(IWaveProvider source)
        {
            _channels = source.WaveFormat.Channels;
            _bytesPerSample = source.WaveFormat.BitsPerSample / 8;
            _sourceAsWaveStream = source as WaveStream;

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
            if (_sourceAsWaveStream is not null && _sourceAsWaveStream.CanSeek)
            {
                long bytePosition = position * _channels * _bytesPerSample;
                _sourceAsWaveStream.Position = bytePosition;
                return;
            }

            throw new InvalidOperationException();
        }

        public int Read(Span<byte> buffer)
        {
            var array = _bufferPool.Rent(buffer.Length);
            var bytesRead = Source.Read(array, 0, buffer.Length);

            if (bytesRead > 0)
            {
                array.AsSpan().Slice(0, bytesRead).CopyTo(buffer);
            }

            _bufferPool.Return(array, clearArray: false);
            return bytesRead;
        }
    }
}
