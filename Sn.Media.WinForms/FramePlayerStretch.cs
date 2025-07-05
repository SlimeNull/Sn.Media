namespace Sn.Media.WinForms
{
    public enum FramePlayerStretch
    {
        //
        // 摘要:
        //     The content preserves its original size.
        None = 0,
        //
        // 摘要:
        //     The content is resized to fill the destination dimensions. The aspect ratio is
        //     not preserved.
        Fill = 1,
        //
        // 摘要:
        //     The content is resized to fit in the destination dimensions while it preserves
        //     its native aspect ratio.
        Uniform = 2,
        //
        // 摘要:
        //     The content is resized to fill the destination dimensions while it preserves
        //     its native aspect ratio. If the aspect ratio of the destination rectangle differs
        //     from the source, the source content is clipped to fit in the destination dimensions.
        UniformToFill = 3
    }
}
