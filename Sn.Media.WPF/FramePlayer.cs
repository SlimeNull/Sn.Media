
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

        private unsafe void ReadAndFillFrameImage(DrawingContext drawingContext, IFrameStream frameStream)
        {
            EnsureFrameBuffer(frameStream);

            frameStream.ReadFrame(_frameDataBuffer, 0, _frameDataBuffer.Length);

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

        protected override void OnRender(DrawingContext drawingContext)
        {
            var source = Source;
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

            // TODO: 对帧绘制进行布局
            drawingContext.DrawImage(_frameImageBuffer, new Rect(default, RenderSize));
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(FramePlayer), new FrameworkPropertyMetadata(default(TimeSpan), FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnPositionChanged));


        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(IFrameStream), typeof(FramePlayer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender), ValidateSource);

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(FramePlayer), new FrameworkPropertyMetadata(false, propertyChangedCallback: OnIsPlayingChanged));


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
