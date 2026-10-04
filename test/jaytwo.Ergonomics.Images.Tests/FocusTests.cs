using Xunit;

namespace jaytwo.Ergonomics.Images.Tests;

public class FocusTests
{
    [Fact]
    public void Zoom_focus_keeps_the_left_of_a_horizontal_image()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 200,
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 0.25, 1),
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 220, 20, 20);
        image.Near(90, 100, 220, 20, 20);
    }

    [Fact]
    public void Zoom_point_keeps_the_right_edge()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 200,
            Fit = ImageFit.Zoom,
            Focus = ImageFocus.Point(1, 0.5),
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 20, 20, 220);
        image.Near(90, 100, 20, 20, 220);
    }

    [Fact]
    public void Zoom_focus_keeps_the_top_of_a_vertical_image()
    {
        var output = Transform.Png(SampleImages.VerticalHalves(200, 400), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 200,
            Height = 100,
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 1, 0.25),
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(100, image.Height);
        image.Near(100, 10, 220, 20, 20);
        image.Near(100, 90, 220, 20, 20);
    }

    [Fact]
    public void Zoom_focus_upscales_the_anchored_window()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(40, 20), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 80,
            Height = 80,
            Fit = ImageFit.Zoom,
            Focus = ImageFocus.Point(0, 0.5),
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(80, image.Width);
        Assert.Equal(80, image.Height);
        image.Near(16, 40, 220, 20, 20);
        image.Near(48, 40, 220, 20, 20);
    }

    [Fact]
    public void Zoom_to_square_moves_with_the_focus_box()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Aspect = ImageAspectRatio.Square,
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 0.25, 1),
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(200, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(10, 100, 220, 20, 20);
        image.Near(190, 100, 220, 20, 20);
    }

    [Fact]
    public void Zoom_stops_at_a_focus_box_and_paints_the_gap()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 100,
            Height = 200,
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 1, 1),
            Canvas = ImageCanvas.SolidBlack,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(100, image.Width);
        Assert.Equal(200, image.Height);
        image.Near(50, 10, 0, 0, 0);
        image.Near(10, 100, 220, 20, 20);
        image.Near(90, 100, 20, 20, 220);
        image.Near(50, 190, 0, 0, 0);
    }

    [Fact]
    public void Zoom_focus_gap_is_transparent_when_the_canvas_is_omitted()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(80, 40), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Width = 40,
            Height = 80,
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 1, 1),
            Enlarge = false,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(40, image.Width);
        Assert.Equal(80, image.Height);
        image.AlphaNear(20, 4, 0);
        image.Near(4, 40, 220, 20, 20);
        image.Near(36, 40, 20, 20, 220);
        image.AlphaNear(20, 40, 255);
    }

    [Fact]
    public void Zoom_to_square_pads_when_the_focus_box_is_the_whole_image()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png)
        {
            Aspect = ImageAspectRatio.Square,
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 1, 1),
            Canvas = ImageCanvas.SolidWhite,
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(400, image.Width);
        Assert.Equal(400, image.Height);
        image.Near(200, 2, 255, 255, 255);
        image.Near(20, 200, 220, 20, 20);
        image.Near(380, 200, 20, 20, 220);
    }

    [Fact]
    public void Box_zoom_uses_the_focus_box()
    {
        var output = Transform.Png(SampleImages.HorizontalHalves(400, 200), (source, destination) => ImageTransforms.Resize(source, destination, new ImageResize(ImageOutputFormat.Png, ImageSize.S)
        {
            Fit = ImageFit.Zoom,
            Focus = new ImageFocus(0, 0, 0.25, 1),
        }));

        using var image = DecodedImage.Open(output);
        Assert.Equal(360, image.Width);
        Assert.Equal(360, image.Height);
        image.Near(20, 180, 220, 20, 20);
        image.Near(340, 180, 220, 20, 20);
    }
}
