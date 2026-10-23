using System.Security.Cryptography;
using SkiaSharp;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;

namespace Zuppeto.Infrastructure.Places;

internal sealed class SkiaSharpPlaceMediaProcessor : IPlaceMediaProcessor
{
    private const long MaximumPixels = 20_000_000;

    public Task<ProcessedPlaceMedia> ProcessAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var data = SKData.CreateCopy(content);
            using var codec = SKCodec.Create(data) ?? throw new InvalidOperationException("El fitxer no és una imatge reconeguda.");
            if ((long)codec.Info.Width * codec.Info.Height > MaximumPixels)
                throw new InvalidOperationException("La fotografia supera el límit de 20 megapíxels.");
            if (codec.FrameCount > 1) throw new InvalidOperationException("No s’admeten imatges animades.");
            using var decoded = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("La imatge no es pot descodificar.");
            using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);
            var optimized = CreateVariant(oriented, 1600, 80, PlaceMediaVariant.Optimized);
            var thumbnail = CreateVariant(oriented, 480, 76, PlaceMediaVariant.Thumbnail);
            var sourceHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            return Task.FromResult(new ProcessedPlaceMedia(sourceHash, [optimized, thumbnail]));
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        { throw new InvalidOperationException("La imatge està malmesa o conté dades no vàlides.", exception); }
    }

    private static ProcessedPlaceMediaVariant CreateVariant(SKBitmap source, int maximumDimension, int quality,
        PlaceMediaVariant variant)
    {
        var scale = Math.Min(1d, maximumDimension / (double)Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var resized = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(resized))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        }
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality)
            ?? throw new InvalidOperationException("No s’ha pogut convertir la fotografia a WebP.");
        var bytes = encoded.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new ProcessedPlaceMediaVariant(variant, "image/webp", bytes, width, height, hash);
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsDimensions = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var target = new SKBitmap(swapsDimensions ? source.Height : source.Width,
            swapsDimensions ? source.Width : source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(target.Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(target.Width, target.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, target.Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.Scale(-1, 1); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(target.Width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(target.Width, target.Height); canvas.Scale(-1, 1); canvas.RotateDegrees(270); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, target.Height); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));
        return target;
    }
}
