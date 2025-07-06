namespace Sn.Media
{
    public enum WaveAudioFormat : ushort
    {
        None = 0,
        PCM = 1,
        IeeeFloat = 3,
        ALaw = 6,
        MuLaw = 7,
        Extensible = 0xFFFE, // Used for WAVEFORMATEXTENSIBLE
    }
}
