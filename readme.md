# Sn.Media

Media abstraction and playback library

![Demo](/assets/demo.webp)


## Structure

- Sn.Media: Basic abstraction
- Sn.Media.NAudio: Sample stream and player based on NAudio
- Sn.Media.OpenCvSharp4: Frame stream based on OpenCvSharp4 VideoCapture
- Sn.Media.WPF: Frame player implementation on WPF



## Usage

### Play video on WPF

1. Add xmlns:

    ```xml
    xmlns:mwpf="clr-namespace:Sn.Media.WPF;assembly=Sn.Media.WPF"
    ```

2. Add `SamplePlayer` control:

    ```xml
    <mwpf:FramePlayer x:Name="framePlayer" />
    ```

3. Set source, and play:
    ```csharp
    framePlayer.Source = new VideoFileFrameStream(@"D:\CloudMusic\MV\shanqiu.mp4");
    framePlayer.IsPlaying = true;

    ```

    > Tips: Type `VideoFileFrameStream` is from `Sn.Media.OpenCvSharp4`


### Play audio

1. Create player, set source, play

    ```csharp
    var samplePlayer = new SamplePlayer();
    samplePlayer.Source = new AudioFileSampleStream(@"D:\CloudMusic\MV\shanqiu.mp4");
    samplePlayer.IsPlaying = true;
    ```

### Use buffered stream

If you want the reading of frame data or sample data to happen in the background, you can use buffered frame or sample streams

```c#
framePlayer.Source = new BufferedFrameStream(new VideoFileFrameStream(@"D:\CloudMusic\MV\shanqiu.mp4"));
```