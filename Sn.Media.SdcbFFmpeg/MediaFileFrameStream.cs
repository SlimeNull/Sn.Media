using System.Buffers;
using System.Threading.Channels;
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
    public class MediaFileFrameStream : IFrameStream, IDisposable
    {
        private readonly bool _leaveOpen;
        private readonly Stream _mediaStream;
        private readonly IOContext _inputContext;
        private readonly FormatContext _inputFormatContext;
        private readonly MediaStream _inputVideoStream;
        private readonly CodecContext _inputVideoDecoder;
        private readonly VideoFrameConverter? _videoFrameConverter;
        private readonly Frame? _convertedVideoFrame;
        private bool _disposedValue;

        public FrameFormat Format => FrameFormat.Bgr888;

        public Fraction FrameRate { get; }

        public int FrameWidth { get; }

        public int FrameHeight { get; }

        public int FrameStride { get; }

        public int FrameDataSize { get; }

        public bool HasPosition => true;

        public bool HasLength => true;

        public bool CanSeek => true;

        public long Position { get; private set; }

        public long Length { get; }

        public MediaFileFrameStream(Stream mediaStream, bool leaveOpen)
        {
            _mediaStream = mediaStream;
            _inputContext = IOContext.ReadStream(mediaStream);
            _inputFormatContext = FormatContext.OpenInputIO(_inputContext);

            // initialize
            _inputFormatContext.LoadStreamInfo();
            _inputVideoStream = _inputFormatContext.FindBestStreamOrNull(AVMediaType.Video)
                ?? throw new ArgumentException("No audio stream found");

            // decoder
            _inputVideoDecoder = new CodecContext(Codec.FindDecoderById(_inputVideoStream.Codecpar!.CodecId));
            _inputVideoDecoder.FillParameters(_inputVideoStream.Codecpar);
            _inputVideoDecoder.Open();

            _inputVideoDecoder.ChLayout = _inputVideoDecoder.ChLayout;
            _inputVideoDecoder.PktTimebase = _inputVideoStream.TimeBase;

            // stream info
            var frameFormat = (AVPixelFormat)_inputVideoStream.Codecpar.Format;
            var frameRate = _inputVideoStream.Codecpar.Framerate;
            var frameWidth = _inputVideoStream.Codecpar.Width;
            var frameHeight = _inputVideoStream.Codecpar.Height;
            var duration = _inputVideoStream.Duration;
            var timeBase = _inputVideoStream.TimeBase;

            if (frameFormat != AVPixelFormat.Bgr24)
            {
                _videoFrameConverter = new VideoFrameConverter();
                _convertedVideoFrame = new Frame()
                {
                    Width = frameWidth,
                    Height = frameHeight,
                    Format = (int)AVPixelFormat.Bgr24,
                };

                _convertedVideoFrame.EnsureBuffer();
                _convertedVideoFrame.MakeWritable();
            }

            FrameRate = new Fraction(frameRate.Num, frameRate.Den);
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameStride = frameWidth * 3;  // bgr
            FrameDataSize = FrameStride * FrameHeight;
            Length = TimeStampToPosition(duration);
        }

        private void EnsureNotDisposed()
        {
            if (_disposedValue)
            {
                throw new InvalidOperationException("Object disposed");
            }
        }

        public MediaFileFrameStream(Stream mediaStream)
            : this(mediaStream, false) { }

        public MediaFileFrameStream(string filePath)
            : this(File.OpenRead(filePath), false) { }

        private long TimeStampToPosition(long timeStamp)
        {
            var timeBase = _inputVideoStream.TimeBase;

            return timeStamp
                * timeBase.Num
                * FrameRate.Numerator
                / FrameRate.Denominator
                / timeBase.Den;
        }

        private long PositionToTimeStamp(long position)
        {
            var timeBase = _inputVideoStream.TimeBase;

            return position
                * timeBase.Den
                * FrameRate.Denominator
                / FrameRate.Numerator
                / timeBase.Num;
        }

        public void Seek(long position)
        {
            EnsureNotDisposed();

            var timeStamp = PositionToTimeStamp(position);
            _inputFormatContext.SeekFrame(timeStamp, _inputVideoStream.Index);
            Position = position;
        }

        public unsafe bool Read(Span<byte> buffer)
        {
            EnsureNotDisposed();

            using var packet = new Packet();
            using var frame = new Frame();

            while (true)
            {
                var result = _inputVideoDecoder.ReceiveFrame(frame);

                if (result == CodecResult.Success)
                {
                    break;
                }
                else if (result == CodecResult.EOF)
                {
                    return false;
                }

                do
                {
                    _inputFormatContext.ReadFrame(packet);
                }
                while (packet.StreamIndex != _inputVideoStream.Index);

                _inputVideoDecoder.SendPacket(packet);
            }

            var frameToRead = frame;

            if (_videoFrameConverter is not null)
            {
                _videoFrameConverter.ConvertFrame(frame, _convertedVideoFrame!);

                frameToRead = _convertedVideoFrame!;
            }

            for (int y = 0; y < FrameHeight; y++)
            {
                new Span<byte>((void*)(frameToRead.Data[0] + frameToRead.Linesize[0] * y), FrameStride)
                    .CopyTo(buffer.Slice(FrameStride * y));
            }

            Position = TimeStampToPosition(frame.Pts) + 1;
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _convertedVideoFrame?.Dispose();
                _videoFrameConverter?.Dispose();
                _inputVideoDecoder.Dispose();
                _inputFormatContext.Dispose();
                _inputContext.Dispose();

                if (!_leaveOpen)
                {
                    _mediaStream.Dispose();
                }

                _disposedValue = true;
            }
        }

        ~MediaFileFrameStream()
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
