namespace App.Tests;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VerifyTests;

public static class PngComparer
{
    private const double MaxDifferentPixelsFraction = 0.03; // 3.0%
    private const int ChannelTolerance = 10; // per R/G/B channel, out of 255

    public static void Register()
    {
        VerifierSettings.RegisterStreamComparer("png", Compare);
    }

    private static async Task<CompareResult> Compare(
        Stream received,
        Stream verified,
        IReadOnlyDictionary<string, object> context)
    {
        using var receivedImage = await Image.LoadAsync<Rgb24>(received);
        using var verifiedImage = await Image.LoadAsync<Rgb24>(verified);

        if (receivedImage.Width != verifiedImage.Width || receivedImage.Height != verifiedImage.Height)
        {
            var message = $"Size mismatch: received {receivedImage.Width}x{receivedImage.Height}, verified {verifiedImage.Width}x{verifiedImage.Height}";
            return CompareResult.NotEqual(message);
        }

        long differentPixels = 0;
        long totalPixels = receivedImage.Width * receivedImage.Height;

        for (var y = 0; y < receivedImage.Height; y++)
        {
            for (var x = 0; x < receivedImage.Width; x++)
            {
                var r = receivedImage[x, y];
                var v = verifiedImage[x, y];

                if (Math.Abs(r.R - v.R) > ChannelTolerance 
                    || Math.Abs(r.G - v.G) > ChannelTolerance 
                    || Math.Abs(r.B - v.B) > ChannelTolerance)
                {
                    differentPixels++;
                }
            }
        }

        var fraction = (double)differentPixels / totalPixels;
        if (fraction > MaxDifferentPixelsFraction)
        {
            var percent = fraction * 100;
            var message = $"{percent:F2}% of pixels differ beyond tolerance (threshold: {MaxDifferentPixelsFraction * 100:F1}%)";
            return CompareResult.NotEqual(message);
        }

        return CompareResult.Equal;
    }
}
