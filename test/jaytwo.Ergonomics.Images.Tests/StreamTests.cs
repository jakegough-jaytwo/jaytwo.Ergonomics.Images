using System;
using System.IO;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class StreamTests
{
    [Fact]
    public void Resize_accepts_non_seekable_streams()
    {
        var sourceBytes = SampleImages.HorizontalHalves(400, 200);
        using var source = new NonSeekableStream(new MemoryStream(sourceBytes));
        using var backing = new MemoryStream();
        using var destination = new NonSeekableStream(backing);
        ImageTransforms.Resize(source, destination, ImageOutputFormat.Png, 100, 100, enlarge: false);
        var output = backing.ToArray();

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(50, image.Height);
        image.Near(10, 25, 220, 20, 20);
    }

    [Fact]
    public void Crop_matches_on_a_non_seekable_stream()
    {
        var bytes = SampleImages.HorizontalHalves(400, 200);
        Action<Stream, Stream> render = (source, destination) =>
            ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
            {
                Width = 100,
                Height = 200,
                Fit = ImageFit.Zoom,
            });
        var seekable = Transform.Png(bytes, render);

        using var input = new NonSeekableStream(new MemoryStream(bytes));
        using var backing = new MemoryStream();
        using var output = new NonSeekableStream(backing);
        render(input, output);

        using var seekableImage = DecodedImage.Open(seekable);
        using var streamed = DecodedImage.Open(backing.ToArray());
        Assert.Equal(seekableImage.Width, streamed.Width);
        Assert.Equal(seekableImage.Height, streamed.Height);
        streamed.Near(10, 100, 220, 20, 20);
        streamed.Near(90, 100, 20, 20, 220);
    }
}
