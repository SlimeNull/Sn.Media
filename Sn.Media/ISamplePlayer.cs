namespace Sn.Media
{
    public interface ISamplePlayer
    {
        public ISampleStream? Source { get; set; }

        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; }

        public bool IsPlaying { get; set; }
    }
}
