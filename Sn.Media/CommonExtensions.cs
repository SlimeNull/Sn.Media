using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public static class CommonExtensions
    {
        public static int GetByteSize(this SampleFormat sampleFormat)
        {
            return sampleFormat switch
            {
                SampleFormat.UInt8 => 1,
                SampleFormat.Int16 => 2,
                SampleFormat.Int32 => 4,
                SampleFormat.Float32 => 4,
                _ => throw new NotSupportedException($"Unsupported sample format: {sampleFormat}"),
            };
        }

        public static ISampleStream AsNonSeekable(this ISampleStream sampleStream)
        {
            ArgumentNullException.ThrowIfNull(sampleStream);

            if (!sampleStream.CanSeek)
            {
                return sampleStream;
            }

            return new NonSeekableSampleStream(sampleStream);
        }

        public static ISampleStream AsFormat(this ISampleStream sampleStream, SampleFormat format)
        {
            ArgumentNullException.ThrowIfNull(sampleStream);

            if (sampleStream.Format == format)
            {
                return sampleStream;
            }

            return new FormatConvertSampleStream(sampleStream, format);
        }

        private class NonSeekableSampleStream : ISampleStream
        {
            private readonly ISampleStream _source;

            public NonSeekableSampleStream(ISampleStream source)
            {
                _source = source;
            }

            public SampleFormat Format => _source.Format;

            public int SampleRate => _source.SampleRate;

            public int Channels => _source.Channels;

            public bool HasPosition => _source.HasPosition;

            public bool HasLength => _source.HasLength;

            public bool CanSeek => false;

            public long Position => _source.Position;

            public long Length => _source.Length;

            public void Seek(long position)
            {
                throw new InvalidOperationException();
            }

            public int Read(Span<byte> buffer)
            {
                return _source.Read(buffer);
            }
        }

        private class FormatConvertSampleStream : ISampleStream
        {
            private readonly ISampleStream _source;
            private readonly SampleFormat _targetFormat;
            private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Create();

            public FormatConvertSampleStream(ISampleStream source, SampleFormat targetFormat)
            {
                _source = source;
                _targetFormat = targetFormat;
            }

            public SampleFormat Format => _targetFormat;

            public int SampleRate => _source.SampleRate;

            public int Channels => _source.Channels;

            public bool HasPosition => _source.HasPosition;

            public bool HasLength => _source.HasLength;

            public bool CanSeek => _source.CanSeek;

            public long Position => _source.Position;

            public long Length => _source.Length;

            private static float ConvertToFloat(byte value)
            {
                return (float)value / byte.MaxValue * 2 - 1;
            }

            private static float ConvertToFloat(short value)
            {
                return (float)value / (short.MaxValue + 1);
            }

            private static double ConvertToFloat(int value)
            {
                return (double)value / (int.MaxValue + 1L);
            }

            private static byte ConvertToByte(short value) => (byte)((ConvertToFloat(value) + 1) / 2 * byte.MaxValue);
            private static byte ConvertToByte(int value) => (byte)((ConvertToFloat(value) + 1) / 2 * byte.MaxValue);
            private static byte ConvertToByte(float value) => (byte)((value + 1) / 2 * byte.MaxValue);

            private static short ConvertToShort(byte value) => (short)(ConvertToFloat(value) * short.MaxValue);
            private static short ConvertToShort(int value) => (short)(ConvertToFloat(value) * short.MaxValue);
            private static short ConvertToShort(float value) => (short)(value * short.MaxValue);

            private static int ConvertToInt(byte value) => (short)(ConvertToFloat(value) * short.MaxValue);
            private static int ConvertToInt(short value) => (short)(ConvertToFloat(value) * short.MaxValue);
            private static int ConvertToInt(float value) => (short)(value * short.MaxValue);

            private void ConvertSamples(Span<byte> source, Span<byte> destination, int sampleCount)
            {
                switch ((_source.Format, _targetFormat))
                {
                    case (SampleFormat.UInt8, SampleFormat.UInt8):
                    case (SampleFormat.Int16, SampleFormat.Int16):
                    case (SampleFormat.Int32, SampleFormat.Int32):
                    case (SampleFormat.Float32, SampleFormat.Float32):
                        source.CopyTo(destination);
                        break;

                    case (SampleFormat.UInt8, var targetFormat):
                        if (targetFormat == SampleFormat.Int16)
                        {
                            var int16Span = MemoryMarshal.Cast<byte, short>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                int16Span[i] = ConvertToShort(source[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Int32)
                        {
                            var int32Span = MemoryMarshal.Cast<byte, int>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                int32Span[i] = ConvertToInt(source[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Float32)
                        {
                            var float32Span = MemoryMarshal.Cast<byte, float>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                float32Span[i] = ConvertToFloat(source[i]);
                            }
                        }
                        break;

                    case (SampleFormat.Int16, var targetFormat):
                        var sourceInt16Span = MemoryMarshal.Cast<byte, short>(source);
                        if (targetFormat == SampleFormat.UInt8)
                        {
                            var destInt8Span = MemoryMarshal.Cast<byte, byte>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt8Span[i] = ConvertToByte(sourceInt16Span[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Int32)
                        {
                            var destInt32Span = MemoryMarshal.Cast<byte, int>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt32Span[i] = ConvertToInt(sourceInt16Span[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Float32)
                        {
                            var destFloat32Span = MemoryMarshal.Cast<byte, float>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destFloat32Span[i] = ConvertToFloat(sourceInt16Span[i]);
                            }
                        }
                        break;

                    case (SampleFormat.Int32, var targetFormat):
                        var sourceInt32Span = MemoryMarshal.Cast<byte, int>(source);
                        if (targetFormat == SampleFormat.UInt8)
                        {
                            var destInt8Span = MemoryMarshal.Cast<byte, byte>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt8Span[i] = ConvertToByte(sourceInt32Span[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Int16)
                        {
                            var destInt16Span = MemoryMarshal.Cast<byte, short>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt16Span[i] = ConvertToShort(sourceInt32Span[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Float32)
                        {
                            var destFloat32Span = MemoryMarshal.Cast<byte, float>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destFloat32Span[i] = (float)ConvertToFloat(sourceInt32Span[i]);
                            }
                        }
                        break;

                    case (SampleFormat.Float32, var targetFormat):
                        var sourceFloat32Span = MemoryMarshal.Cast<byte, float>(source);
                        if (targetFormat == SampleFormat.UInt8)
                        {
                            var destInt8Span = MemoryMarshal.Cast<byte, byte>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt8Span[i] = ConvertToByte(sourceFloat32Span[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Int16)
                        {
                            var destInt16Span = MemoryMarshal.Cast<byte, short>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt16Span[i] = ConvertToShort(sourceFloat32Span[i]);
                            }
                        }
                        else if (targetFormat == SampleFormat.Int32)
                        {
                            var destInt32Span = MemoryMarshal.Cast<byte, int>(destination);
                            for (int i = 0; i < sampleCount; i++)
                            {
                                destInt32Span[i] = ConvertToInt(sourceFloat32Span[i]);
                            }
                        }
                        break;

                }
            }

            public void Seek(long position)
            {
                _source.Seek(position);
            }

            public int Read(Span<byte> buffer)
            {
                var sourceBytesPerSample = _source.Format.GetByteSize();
                var targetBytesPerSample = _targetFormat.GetByteSize();
                var requiredBufferSize = buffer.Length * sourceBytesPerSample / targetBytesPerSample;
                var bufferForSource = _bufferPool.Rent(requiredBufferSize);
                var bytesRead = _source.Read(new Span<byte>(bufferForSource, 0, requiredBufferSize));
                var samplesRead = bytesRead / sourceBytesPerSample;

                ConvertSamples(bufferForSource, buffer, samplesRead);

                _bufferPool.Return(bufferForSource);
                return samplesRead * targetBytesPerSample;
            }
        }
    }
}
