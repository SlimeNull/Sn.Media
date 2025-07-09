
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sn.Media;

namespace Sn.Media.WPF
{
    public class FramePlayer : FrameworkElement, IFramePlayer
    {
        private bool _isPlaying;
        private bool _isChangingTimeByPlayLogic;
        private byte[]? _frameDataBuffer;
        private bool _hasBufferedFrame;
        private TimeSpan _bufferedFrameTime;
        private WriteableBitmap? _frameImageBuffer;

        private TimeSpan _startPlayTime;
        private Stopwatch? _stopwatchFromPlayStarted;


        public IFrameStream? Source
        {
            get { return (IFrameStream?)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }
        public TimeSpan Duration
        {
            get
            {
                if (Source is null)
                {
                    throw new InvalidOperationException("No source specified");
                }
                if (!Source.HasDuration)
                {
                    throw new InvalidOperationException("Source no length");
                }

                return Source.Duration;
            }
        }

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }



        [MemberNotNull(nameof(_frameDataBuffer))]
        [MemberNotNull(nameof(_frameImageBuffer))]
        private void EnsureFrameBuffer(IFrameStream frameStream)
        {
            var pixelFormat = frameStream.Format.ToPixelFormat();

            if (_frameDataBuffer is null ||
                _frameDataBuffer.Length != frameStream.FrameDataSize)
            {
                _frameDataBuffer = new byte[frameStream.FrameDataSize];
            }

            if (_frameImageBuffer is null ||
            _frameImageBuffer.PixelWidth != frameStream.FrameWidth ||
            _frameImageBuffer.PixelHeight != frameStream.FrameHeight ||
            _frameImageBuffer.Format != pixelFormat)
            {
                _frameImageBuffer = new WriteableBitmap(
                    frameStream.FrameWidth,
                    frameStream.FrameHeight,
                    96, // DPI X
                    96, // DPI Y
                    pixelFormat,
                    null);
            }
        }

        private unsafe void FillFrameImage(IFrameStream frameStream, WriteableBitmap writeableBitmap, byte[] frameBuffer)
        {
            writeableBitmap.Lock();
            var buffer = (byte*)writeableBitmap.BackBuffer;
            var stride = Math.Min(frameStream.FrameStride, writeableBitmap.BackBufferStride);
            var height = writeableBitmap.PixelHeight;

            fixed (byte* bufferPtr = frameBuffer)
            {
                for (int y = 0; y < writeableBitmap.Height; y++)
                {
                    NativeMemory.Copy(bufferPtr + y * frameStream.FrameStride,
                                      buffer + y * stride,
                                      (nuint)stride);
                }
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();
        }

        private unsafe void ClearFrameImage(WriteableBitmap writeableBitmap)
        {
            writeableBitmap.Lock();
            NativeMemory.Clear((void*)writeableBitmap.BackBuffer, (nuint)(writeableBitmap.BackBufferStride * writeableBitmap.PixelHeight));

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var requiredSize = default(Size);
            if (Source is { } source)
            {
                requiredSize = new Size(
                    Math.Min(availableSize.Width, source.FrameWidth),
                    Math.Min(availableSize.Height, source.FrameHeight));
            }

            return requiredSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var source = Source;
            var stretch = Stretch;
            var position = Position;
            var isPlaying = IsPlaying;

            if (!isPlaying ||
                source is null)
            {
                return;
            }

            EnsureFrameBuffer(source);

            bool filled = false;
            while (true)
            {
                if (!_hasBufferedFrame)
                {
                    _hasBufferedFrame = source.Read(_frameDataBuffer, out _bufferedFrameTime);
                }

                // 有帧
                if (_hasBufferedFrame)
                {
                    if (_bufferedFrameTime <= position)
                    {
#if DEBUG
                        if (filled)
                        {
                            Debug.WriteLine("Skip Frame");
                        }
#endif

                        FillFrameImage(source, _frameImageBuffer, _frameDataBuffer);
                        _hasBufferedFrame = false;
                        filled = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (!filled)
                    {
                        ClearFrameImage(_frameImageBuffer);
                    }

                    break;
                }
            }


            if (_frameImageBuffer is not null)
            {
                if (stretch == Stretch.None)
                {
                    drawingContext.DrawImage(_frameImageBuffer, new Rect(0, 0, _frameImageBuffer.Width, _frameImageBuffer.Height));
                }
                else if (stretch == Stretch.Fill)
                {
                    drawingContext.DrawImage(_frameImageBuffer, new Rect(default, RenderSize));
                }
                else if (stretch == Stretch.Uniform)
                {
                    var aspectRatio = (double)_frameImageBuffer.PixelWidth / _frameImageBuffer.PixelHeight;
                    var renderAspectRatio = RenderSize.Width / RenderSize.Height;
                    double width, height;
                    if (aspectRatio > renderAspectRatio)
                    {
                        width = RenderSize.Width;
                        height = RenderSize.Width / aspectRatio;
                    }
                    else
                    {
                        width = RenderSize.Height * aspectRatio;
                        height = RenderSize.Height;
                    }

                    drawingContext.DrawImage(_frameImageBuffer, new Rect((RenderSize.Width - width) / 2, (RenderSize.Height - height) / 2, width, height));
                }
                else if (stretch == Stretch.UniformToFill)
                {
                    var aspectRatio = (double)_frameImageBuffer.PixelWidth / _frameImageBuffer.PixelHeight;
                    var renderAspectRatio = RenderSize.Width / RenderSize.Height;
                    double width, height;
                    if (aspectRatio > renderAspectRatio)
                    {
                        width = RenderSize.Height * aspectRatio;
                        height = RenderSize.Height;
                    }
                    else
                    {
                        width = RenderSize.Width;
                        height = RenderSize.Width / aspectRatio;
                    }

                    drawingContext.DrawImage(_frameImageBuffer, new Rect((RenderSize.Width - width) / 2, (RenderSize.Height - height) / 2, width, height));
                }
            }
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(FramePlayer),
                new FrameworkPropertyMetadata(default(TimeSpan), FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnPositionChanged));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(IFrameStream), typeof(FramePlayer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender), ValidateSource);

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(FramePlayer),
                new FrameworkPropertyMetadata(false, propertyChangedCallback: OnIsPlayingChanged));

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(FramePlayer),
                new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsRender));

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            var length = Duration;
            var position = _startPlayTime + _stopwatchFromPlayStarted!.Elapsed;
            if (position > length)
            {
                position = length;
            }

            _isChangingTimeByPlayLogic = true;
            Position = position;
            _isChangingTimeByPlayLogic = false;
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FramePlayer player ||
                e.NewValue is not TimeSpan newTime)
            {
                return;
            }

            if (!player._isChangingTimeByPlayLogic)
            {
                player._startPlayTime = newTime;
                player._stopwatchFromPlayStarted?.Restart();
                player._hasBufferedFrame = false;

                if (player.Source is { CanSeek: true } seekableSource)
                {
                    seekableSource.SeekByTime(newTime);
                }
            }
        }

        private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FramePlayer player)
            {
                return;
            }

            CompositionTarget.Rendering -= player.CompositionTarget_Rendering;

            if (e.NewValue is true)
            {
                player._startPlayTime = player.Position;
                player._stopwatchFromPlayStarted = Stopwatch.StartNew();
                CompositionTarget.Rendering += player.CompositionTarget_Rendering;
            }
        }

        private static bool ValidateSource(object value)
        {
            return
                value is null ||
                (value is IFrameStream frameStream && frameStream.Format.IsSupportedPixelFormat());
        }
    }

}
