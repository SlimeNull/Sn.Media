using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct WaveFileHeader
    {
        private fixed byte _chunkId[4];
        private uint _chunkSize;
        private fixed byte _format[4];

        private fixed byte _subChunk1Id[4];
        private uint _subChunk1Size;
        private WaveAudioFormat _audioFormat;
        private ushort _numChannels;
        private uint _sampleRate;
        private uint _byteRate;
        private ushort _blockAlign;
        private ushort _bitsPerSample;

        private fixed byte _subChunk2Id[4];
        private uint _subChunk2Size;

        public unsafe string ChunkId
        {
            get
            {
                fixed (byte* ptr = _chunkId)
                {
                    return CreateString(ptr, 4);
                }
            }
            set
            {
                fixed (byte* ptr = _chunkId)
                {
                    FillString(ptr, value, 4);
                }
            }
        }

        public uint ChunkSize
        {
            get => _chunkSize;
            set => _chunkSize = value;
        }

        public string Format
        {
            get
            {
                fixed (byte* ptr = _format)
                {
                    return CreateString(ptr, 4);
                }
            }
            set
            {
                fixed (byte* ptr = _format)
                {
                    FillString(ptr, value, 4);
                }
            }
        }

        public string FmtChunkId
        {
            get
            {
                fixed (byte* ptr = _subChunk1Id)
                {
                    return CreateString(ptr, 4);
                }
            }
            set
            {
                fixed (byte* ptr = _subChunk1Id)
                {
                    FillString(ptr, value, 4);
                }
            }
        }

        public uint FmtChunkSize
        {
            get => _subChunk1Size;
            set => _subChunk1Size = value;
        }

        public WaveAudioFormat AudioFormat
        {
            get => _audioFormat;
            set => _audioFormat = value;
        }

        public ushort ChannelCount
        {
            get => _numChannels;
            set => _numChannels = value;
        }

        public uint SampleRate
        {
            get => _sampleRate;
            set => _sampleRate = value;
        }

        public uint ByteRate
        {
            get => _byteRate;
            set => _byteRate = value;
        }

        public ushort BlockAlign
        {
            get => _blockAlign;
            set => _blockAlign = value;
        }

        public ushort BitsPerSample
        {
            get => _bitsPerSample;
            set => _bitsPerSample = value;
        }

        public string DataChunkId
        {
            get
            {
                fixed (byte* ptr = _subChunk2Id)
                {
                    return CreateString(ptr, 4);
                }
            }
            set
            {
                fixed (byte* ptr = _subChunk2Id)
                {
                    FillString(ptr, value, 4);
                }
            }
        }

        public uint DataChunkSize
        {
            get => _subChunk2Size;
            set => _subChunk2Size = value;
        }

        private static int ReadComplete(Stream stream, Span<byte> buffer)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead = stream.Read(buffer.Slice(totalBytesRead));
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException(); // End of stream
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public static WaveFileHeader Read(Stream stream)
        {
            var header = default(WaveFileHeader);
            var totalBytesRead = 0;

            totalBytesRead = ReadComplete(stream, new Span<byte>(header._chunkId, 4));

            if (header.ChunkId != "RIFF")
            {
                throw new InvalidDataException("Invalid WAV file: Missing RIFF header.");
            }

            totalBytesRead = ReadComplete(stream, new Span<byte>(&header._chunkSize, sizeof(uint)));
            totalBytesRead = ReadComplete(stream, new Span<byte>(header._format, 4));

            if (header.Format != "WAVE")
            {
                throw new InvalidDataException("Invalid WAV file: Missing WAVE format.");
            }

            var tempBuffer = default(byte[]);
            while (
                !stream.CanSeek ||
                totalBytesRead < stream.Length)
            {
                uint subChunkId;
                totalBytesRead += ReadComplete(stream, new Span<byte>(&subChunkId, 4));

                if (subChunkId == 0x20746D66) // "fmt "
                {
                    header.FmtChunkId = "fmt ";
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._subChunk1Size, sizeof(uint)));
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._audioFormat, sizeof(WaveAudioFormat)));
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._numChannels, sizeof(ushort)));
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._sampleRate, sizeof(uint)));
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._byteRate, sizeof(uint)));
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._blockAlign, sizeof(ushort)));
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._bitsPerSample, sizeof(ushort)));
                }
                else if (subChunkId == 0x61746164) // "data"
                {
                    header.DataChunkId = "data";
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&header._subChunk2Size, sizeof(uint)));
                    break; // Data chunk found
                }
                else
                {
                    tempBuffer ??= new byte[1024];

                    uint chunkSize;
                    totalBytesRead += ReadComplete(stream, new Span<byte>(&chunkSize, sizeof(uint)));

                    int remain = (int)chunkSize;

                    while (remain > 0)
                    {
                        remain -= stream.Read(tempBuffer, 0, Math.Min(remain, tempBuffer.Length));
                    }
                }
            }

            return header;
        }

        public static WaveFileHeader Create(WaveAudioFormat audioFormat, ushort channelCount, uint sampleRate, ushort bitsPerSample, uint pcmDataSize)
        {
            var header = new WaveFileHeader()
            {
                ChunkId = "RIFF",
                ChunkSize = (uint)(pcmDataSize + (sizeof(WaveFileHeader) - 8)),
                Format = "WAVE",

                FmtChunkId = "fmt ",
                FmtChunkSize = 16,
                AudioFormat = audioFormat,
                ChannelCount = channelCount,
                SampleRate = sampleRate,
                ByteRate = sampleRate * channelCount * bitsPerSample / 8,
                BlockAlign = (ushort)(channelCount * bitsPerSample / 8),
                BitsPerSample = bitsPerSample,

                DataChunkId = "data",
                DataChunkSize = pcmDataSize
            };

            return header;
        }

        public static WaveFileHeader Create(ISampleStream sampleStream, uint? dataSize)
        {
            var bytesPerSample = sampleStream.Format.GetByteSize();
            var bytesPerSampleGroup = bytesPerSample * sampleStream.Channels;

            var actualDataSize = 0u;
            if (sampleStream.HasLength)
            {
                actualDataSize = (uint)(bytesPerSampleGroup * sampleStream.Length);
            }
            else if (dataSize.HasValue)
            {
                actualDataSize = (uint)dataSize.Value;
            }
            else
            {
                throw new ArgumentException("Sample stream must have length or data size specified.");
            }

            var waveAudioFormat = sampleStream.Format switch
            {
                SampleFormat.UInt8 => WaveAudioFormat.PCM,
                SampleFormat.Int16 => WaveAudioFormat.PCM,
                SampleFormat.Int32 => WaveAudioFormat.PCM,
                SampleFormat.Float32 => WaveAudioFormat.IeeeFloat,
                _ => throw new NotSupportedException("Sample format not supported for WAV files.")
            };

            var header = Create(waveAudioFormat, (ushort)sampleStream.Channels, (uint)sampleStream.SampleRate, (ushort)(bytesPerSample * 8), actualDataSize);

            return header;
        }

        private static void FillString(byte* ptr, string value, int maxLength)
        {
            if (value.Length > maxLength)
            {
                throw new ArgumentException(nameof(value));
            }

            fixed (char* textPtr = value)
            {
                for (int i = 0; i < value.Length && i < maxLength; i++)
                {
                    ptr[i] = (byte)textPtr[i];
                }
            }
        }

        private static string CreateString(byte* ptr, int maxLength)
        {
            StringBuilder sb = new StringBuilder(maxLength);
            for (int i = 0; i < maxLength; i++)
            {
                if (ptr[i] == 0)
                {
                    break;
                }

                sb.Append((char)ptr[i]);
            }

            return sb.ToString();
        }
    }
}
