namespace Sn.Media
{
    public interface IFramePlayer
    {
        public IFrameStream? Source { get; set; }

        public TimeSpan Position { get; set; }
        public TimeSpan Length { get; }

        public bool IsPlaying { get; set; }
    }
}
