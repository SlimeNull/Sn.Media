
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
    public class FramePlayer : FrameworkElement
    {
        private bool _isPlaying;
        private bool _isChangingTimeByPlayLogic;
        private TimeSpan _currentTime;
        private byte[]? _frameDataBuffer;
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

        [MemberNotNull(nameof(_frameImageBuffer))]
        private unsafe void ReadAndFillFrameImage(DrawingContext drawingContext, IFrameStream frameStream)
        {
            EnsureFrameBuffer(frameStream);

            frameStream.Read(_frameDataBuffer, 0, _frameDataBuffer.Length);

            _frameImageBuffer.Lock();
            var buffer = (byte*)_frameImageBuffer.BackBuffer;
            var stride = Math.Min(frameStream.FrameStride, _frameImageBuffer.BackBufferStride);
            var height = _frameImageBuffer.PixelHeight;

            fixed (byte* bufferPtr = _frameDataBuffer)
            {
                for (int y = 0; y < _frameImageBuffer.Height; y++)
                {
                    NativeMemory.Copy(bufferPtr + y * frameStream.FrameStride,
                                      buffer + y * stride,
                                      (nuint)stride);
                }
            }

            _frameImageBuffer.AddDirtyRect(new Int32Rect(0, 0, _frameImageBuffer.PixelWidth, _frameImageBuffer.PixelHeight));
            _frameImageBuffer.Unlock();
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

            var requiredFrameIndex = (long)(position.TotalSeconds * source.FrameRate.Numerator / source.FrameRate.Denominator);
            if (Math.Abs(requiredFrameIndex - source.Position) > 1 &&
                source.CanSeek)
            {
                source.Seek(requiredFrameIndex);
            }

            while (source.Position <= requiredFrameIndex)
            {
                ReadAndFillFrameImage(drawingContext, source);
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
            _isChangingTimeByPlayLogic = true;
            Position = _startPlayTime + _stopwatchFromPlayStarted!.Elapsed;
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
                (value is IFrameStream frameStream && frameStream.Format.IsSupportedPixelFormat() && frameStream.HasPosition);
        }
    }

}
