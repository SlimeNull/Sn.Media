using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using PropertyChanging;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Utils;

namespace Sn.Media.SdcbFFmpeg
{
    [ImplementPropertyChanging]
    [AddINotifyPropertyChangedInterface]
    public class MediaFileSampleStream : ISampleStream, IDisposable
    {
        private readonly bool _leaveOpen;
        private readonly Stream _mediaStream;
        private readonly IOContext _inputContext;
        private readonly FormatContext _inputFormatContext;
        private readonly MediaStream _inputAudioStream;
        private readonly CodecContext _inputAudioDecoder;
        private readonly int _bytesPerSample;
        private readonly int _bytesPerSampleGroup;
        private readonly bool _samplePlanar;
        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Create();
        private byte[]? _samplesRemain;
        private int _samplesRemainDataSize;
        private long _samplesRemainPosition;
        private bool _disposedValue;

        public SampleFormat Format { get; }

        public int SampleRate { get; }

        public int Channels { get; }

        public bool HasPosition => true;

        public bool HasLength => true;

        public bool CanSeek => true;

        public long Position { get; private set; }

        public long Length { get; }


        public MediaFileSampleStream(Stream mediaStream, bool leaveOpen)
        {
            _mediaStream = mediaStream;
            _inputContext = IOContext.ReadStream(mediaStream);
            _inputFormatContext = FormatContext.OpenInputIO(_inputContext);

            // initialize
            _inputFormatContext.LoadStreamInfo();
            _inputAudioStream = _inputFormatContext.FindBestStreamOrNull(AVMediaType.Audio)
                ?? throw new ArgumentException("No audio stream found");

            // decoder
            _inputAudioDecoder = new CodecContext(Codec.FindDecoderById(_inputAudioStream.Codecpar!.CodecId));
            _inputAudioDecoder.FillParameters(_inputAudioStream.Codecpar);
            _inputAudioDecoder.Open();

            _inputAudioDecoder.ChLayout = _inputAudioDecoder.ChLayout;
            _inputAudioDecoder.PktTimebase = _inputAudioStream.TimeBase;

            // stream info
            var sampleFormat = (AVSampleFormat)_inputAudioStream.Codecpar.Format;
            var sampleRate = _inputAudioStream.Codecpar.SampleRate;
            var channelCount = _inputAudioStream.Codecpar.ChLayout.nb_channels;
            var duration = _inputAudioStream.Duration;
            var timeBase = _inputAudioStream.TimeBase;

            Format = sampleFormat switch
            {
                AVSampleFormat.U8 or AVSampleFormat.U8p => SampleFormat.UInt8,
                AVSampleFormat.S16 or AVSampleFormat.S16p => SampleFormat.Int16,
                AVSampleFormat.S32 or AVSampleFormat.S32p => SampleFormat.Int32,
                AVSampleFormat.Flt or AVSampleFormat.Fltp => SampleFormat.Float32,
                _ => throw new NotSupportedException()
            };

            _bytesPerSample = Format.GetByteSize();
            _bytesPerSampleGroup = _bytesPerSample * channelCount;
            _samplePlanar = sampleFormat is
                AVSampleFormat.U8p or AVSampleFormat.S16p or AVSampleFormat.S32p or AVSampleFormat.Fltp;

            SampleRate = sampleRate;
            Channels = channelCount;
            Length = TimeStampToPosition(duration);
        }

        private void EnsureNotDisposed()
        {
            if (_disposedValue)
            {
                throw new InvalidOperationException("Object disposed");
            }
        }

        public MediaFileSampleStream(Stream mediaStream)
            : this(mediaStream, false) { }

        public MediaFileSampleStream(string filePath)
            : this(File.OpenRead(filePath), false) { }

        private long TimeStampToPosition(long timeStamp)
        {
            var timeBase = _inputAudioStream.TimeBase;

            return timeStamp
                * timeBase.Num
                * SampleRate
                / timeBase.Den;
        }

        private long PositionToTimeStamp(long position)
        {
            var timeBase = _inputAudioStream.TimeBase;

            return position
                * timeBase.Den
                / SampleRate
                / timeBase.Num;
        }

        public void Seek(long position)
        {
            EnsureNotDisposed();

            var timeStamp = PositionToTimeStamp(position);
            _inputFormatContext.SeekFrame(timeStamp, _inputAudioStream.Index);
            Position = position;
        }

        public unsafe int Read(Span<byte> buffer)
        {
            EnsureNotDisposed();

            using var packet = new Packet();
            using var frame = new Frame();

            long lastFramePosition = 0;
            int totalRead = 0;

            if (_samplesRemain is not null)
            {
                var toWrite = Math.Min(_samplesRemainDataSize, buffer.Length);

                if (_samplesRemainDataSize < buffer.Length)
                {
                    _samplesRemain
                        .AsSpan(0, _samplesRemainDataSize)
                        .CopyTo(buffer);

                    _bufferPool.Return(_samplesRemain);
                    _samplesRemain = null;

                    lastFramePosition = _samplesRemainPosition;
                    totalRead += _samplesRemainDataSize;
                }
                else
                {
                    _samplesRemain
                        .AsSpan(0, buffer.Length)
                        .CopyTo(buffer);

                    Array.Copy(_samplesRemain, buffer.Length, _samplesRemain, 0, _samplesRemainDataSize - buffer.Length);
                    _samplesRemainDataSize -= buffer.Length;
                    _samplesRemainPosition += buffer.Length / _bytesPerSampleGroup;

                    lastFramePosition = _samplesRemainPosition;
                    totalRead += buffer.Length;
                }
            }

            while (totalRead < buffer.Length)
            {
                while (true)
                {
                    var result = _inputAudioDecoder.ReceiveFrame(frame);

                    if (result == CodecResult.Success)
                    {
                        break;
                    }
                    else if (result == CodecResult.EOF)
                    {
                        return totalRead;
                    }

                    do
                    {
                        _inputFormatContext.ReadFrame(packet);
                    }
                    while (packet.StreamIndex != _inputAudioStream.Index);

                    _inputAudioDecoder.SendPacket(packet);
                }

                var frameDataSize = frame.NbSamples * _bytesPerSampleGroup;
                var frameDataSpan = new Span<byte>((void*)frame.Data[0], frameDataSize);
                var bufferForPlanarConv = default(byte[]);

                if (_samplePlanar)
                {
                    bufferForPlanarConv = _bufferPool.Rent(frameDataSize);

                    fixed (byte* bufferForPlanarConvPtr = bufferForPlanarConv)
                    {
                        if (_bytesPerSample == 1)
                        {
                            for (int i = 0; i < frame.NbSamples; i++)
                            {
                                for (int c = 0; c < Channels; c++)
                                {
                                    bufferForPlanarConv[i * Channels + c] = ((byte*)frame.Data[c])[i];
                                }
                            }
                        }
                        else if (_bytesPerSample == 2)
                        {
                            var destSpan = new Span<short>(bufferForPlanarConvPtr, frameDataSize);

                            for (int i = 0; i < frame.NbSamples; i++)
                            {
                                for (int c = 0; c < Channels; c++)
                                {
                                    destSpan[i * Channels + c] = ((short*)frame.Data[c])[i];
                                }
                            }
                        }
                        else if (_bytesPerSample == 4)
                        {
                            var destSpan = new Span<int>(bufferForPlanarConvPtr, frameDataSize);

                            for (int i = 0; i < frame.NbSamples; i++)
                            {
                                for (int c = 0; c < Channels; c++)
                                {
                                    destSpan[i * Channels + c] = ((int*)frame.Data[c])[i];
                                }
                            }
                        }
                    }

                    frameDataSpan = new Span<byte>(bufferForPlanarConv, 0, frameDataSize);
                }

                lastFramePosition = TimeStampToPosition(frame.Pts);
                if (totalRead + frameDataSize <= buffer.Length)
                {
                    frameDataSpan.CopyTo(buffer.Slice(totalRead));

                    totalRead += frameDataSize;
                }
                else
                {
                    var toWrite = buffer.Length - totalRead;
                    var samplesRemainDataSize = frameDataSize - toWrite;

                    frameDataSpan.Slice(0, toWrite).CopyTo(buffer.Slice(totalRead));

                    _samplesRemain = _bufferPool.Rent(samplesRemainDataSize);
                    _samplesRemainDataSize = samplesRemainDataSize;
                    _samplesRemainPosition = Position + totalRead;
                    frameDataSpan.Slice(toWrite).CopyTo(_samplesRemain);

                    totalRead += toWrite;
                }

                if (bufferForPlanarConv is not null)
                {
                    _bufferPool.Return(bufferForPlanarConv);
                }
            }

            packet.Free();
            frame.Free();

            Position = lastFramePosition + totalRead / _bytesPerSampleGroup;
            return totalRead;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_samplesRemain is not null)
                    {
                        _bufferPool.Return(_samplesRemain);
                        _samplesRemain = null;
                    }
                }

                _inputAudioDecoder.Dispose();
                _inputFormatContext.Dispose();
                _inputContext.Dispose();

                if (!_leaveOpen)
                {
                    _mediaStream.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~MediaFileSampleStream()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
