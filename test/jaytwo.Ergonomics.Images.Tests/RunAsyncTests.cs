using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class RunAsyncTests
{
    [Fact]
    public async Task RunAsync_works_with_async_only_streams()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(40, 40)
            .Fit(ImageFit.Fit)
            .EncodingPng();

        var sourceBytes = SampleImages.SolidJpeg(80, 40, 30, 140, 60);
        await using var source = new AsyncOnlyStream(new MemoryStream(sourceBytes));
        await using var backing = new MemoryStream();
        await using var destination = new AsyncOnlyStream(backing);

        var size = await pipeline.RunAsync(source, destination);

        Assert.Equal(new ImageSize(40, 20), size);
        Assert.True(backing.Length > 0);
        using var image = DecodedImage.Open(backing.ToArray());
        Assert.Equal(40, image.Width);
        Assert.Equal(20, image.Height);
    }

    [Fact]
    public async Task RunAsync_cancel_throws_operation_canceled()
    {
        var pipeline = ImagePipeline.Create()
            .Resize(800, 800)
            .Fit(ImageFit.Fit)
            .EncodingPng();

        var sourceBytes = SampleImages.SolidJpeg(1600, 1200, 30, 140, 60);
        await using var source = new AsyncOnlyStream(new MemoryStream(sourceBytes));
        await using var backing = new MemoryStream();
        await using var destination = new SlowAsyncOnlyStream(backing, delayMs: 25);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(source, destination, cts.Token));
    }

    [Fact]
    public async Task RunAsync_requires_encoding()
    {
        var pipeline = ImagePipeline.Create().Resize(10, 10);
        await using var source = new MemoryStream(SampleImages.SolidJpeg(8, 8, 10, 10, 10));
        await using var destination = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.RunAsync(source, destination));
    }
}
