using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;

namespace Sn.Media.Bass
{
    public static class CommonExtensions
    {
        public static unsafe FileProcedures CreateFileProcedures(this ISampleStream source)
        {
            if (!source.HasLength)
            {
                throw new ArgumentException("Sample stream must have a length to create file procedures.", nameof(source));
            }

            var bytesPerSample = source.Format.GetByteSize();
            var bytesPerSampleGroup = bytesPerSample * source.Channels;

            var waveFileHeader = WaveFileHeader.Create(source, null);
            var waveFileHeaderBytes = new ReadOnlySpan<byte>(&waveFileHeader, sizeof(WaveFileHeader)).ToArray();

            var position = 0L;

            return new FileProcedures
            {
                Close = (user) =>
                {
                    if (source is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                },

                Length = (user) => sizeof(WaveFileHeader) + source.Length * bytesPerSampleGroup,

                Read = (buffer, length, user) =>
                {
                    var waveFileHeaderByteCount = waveFileHeaderBytes.Length;
                    var headerRequiredSize = (int)(waveFileHeaderByteCount - position);

                    var totalRead = 0;

                    if (headerRequiredSize > 0)
                    {
                        waveFileHeaderBytes
                            .AsSpan()
                            .Slice((int)position, headerRequiredSize)
                            .CopyTo(new Span<byte>((void*)buffer, headerRequiredSize));

                        totalRead += headerRequiredSize;
                    }
                    else if (headerRequiredSize < 0)
                    {
                        headerRequiredSize = 0;
                    }

                    var sampleRequiredSize = length - headerRequiredSize;
                    if (sampleRequiredSize > 0)
                    {
                        totalRead += source.Read(new Span<byte>((void*)(buffer + headerRequiredSize), sampleRequiredSize));
                    }

                    position += totalRead;

                    if (totalRead <= 0)
                    {
                        return -1;
                    }

                    return totalRead;
                },

                Seek = (offset, user) =>
                {
                    if (!source.CanSeek)
                    {
                        return false;
                    }

                    var waveFileHeaderByteCount = waveFileHeaderBytes.Length;
                    var offsetRelatedToSampleStream = offset - waveFileHeaderByteCount;
                    if (offsetRelatedToSampleStream < 0)
                    {
                        offsetRelatedToSampleStream = 0;
                    }

                    source.Seek(offsetRelatedToSampleStream / bytesPerSampleGroup);
                    position = offset;
                    return true;
                },
            };
        }

        public static unsafe StreamProcedure CreateStreamProcedure(this ISampleStream source)
        {
            var header = WaveFileHeader.Create(source, uint.MaxValue);

            var bytesPerSample = source.Format.GetByteSize();
            var bytesPerSampleGroup = bytesPerSample * source.Channels;

            var waveFileHeader = WaveFileHeader.Create(source, null);
            var waveFileHeaderBytes = new ReadOnlySpan<byte>(&waveFileHeader, sizeof(WaveFileHeader)).ToArray();

            var position = 0L;

            return (stream, buffer, length, user) =>
            {
                var waveFileHeaderByteCount = waveFileHeaderBytes.Length;
                var headerRequiredSize = (int)(waveFileHeaderByteCount - position);

                var totalRead = 0;
                headerRequiredSize = 0;

                if (headerRequiredSize > 0)
                {
                    waveFileHeaderBytes
                        .AsSpan()
                        .Slice((int)position, headerRequiredSize)
                        .CopyTo(new Span<byte>((void*)buffer, headerRequiredSize));

                    totalRead += headerRequiredSize;
                }
                else if (headerRequiredSize < 0)
                {
                    headerRequiredSize = 0;
                }

                var sampleRequiredSize = length - headerRequiredSize;
                if (sampleRequiredSize > 0)
                {
                    totalRead += source.Read(new Span<byte>((void*)(buffer + headerRequiredSize), sampleRequiredSize));
                }

                position += totalRead;

                if (totalRead <= 0)
                {
                    return -1;
                }

                return totalRead;
            };
        }
    }
}
