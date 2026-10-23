using System.Text;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Places.Wikidata;
using Zuppeto.Infrastructure.Persistence.Repositories;
using Zuppeto.Infrastructure.Places;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class PlaceMediaGovernanceTests
{
    [Fact]
    public void Primary_policy_prefers_explicit_selection_then_trusted_source_order()
    {
        var policy = new PlaceMediaSelectionPolicy();
        var commons = Asset(PlaceMediaSource.WikimediaCommons);
        var owner = Asset(PlaceMediaSource.Owner);
        Assert.Equal(owner.Id, policy.SelectPrimary([commons, owner])!.Id);

        var selectedCommons = commons with { IsPrimary = true };
        Assert.Equal(selectedCommons.Id, policy.SelectPrimary([selectedCommons, owner])!.Id);
        Assert.Null(policy.SelectPrimary([owner with { VerificationStatus = PlaceMediaVerificationStatus.Rejected }]));
        Assert.Null(policy.SelectPrimary([owner with { VerificationStatus = PlaceMediaVerificationStatus.Withdrawn }]));
    }

    [Theory]
    [InlineData("CC0-1.0", true)]
    [InlineData("CC-BY-4.0", true)]
    [InlineData("CC-BY-SA-3.0", true)]
    [InlineData("CC-BY-SA", true)]
    [InlineData("CC-BY-NC-4.0", false)]
    [InlineData("CC-BY-ND-4.0", false)]
    [InlineData("unknown", false)]
    public void License_policy_only_accepts_commercial_transformable_licenses(string identifier, bool allowed)
    {
        var decision = new PlaceMediaLicensePolicy().Evaluate(identifier, null);
        Assert.Equal(allowed, decision.Allowed);
        Assert.Equal(allowed, decision.PermanentStorageAllowed);
        Assert.Equal(allowed, decision.TransformationAllowed);
    }

    [Fact]
    public async Task Wikimedia_parser_uses_only_non_deprecated_P18_and_preserves_file_metadata()
    {
        await using var entity = Json("""
            {"entities":{"Q123":{"claims":{"P18":[
              {"rank":"normal","mainsnak":{"datavalue":{"value":"Local.jpg"}}},
              {"rank":"deprecated","mainsnak":{"datavalue":{"value":"Old.jpg"}}}
            ]}}}}
            """);
        Assert.Equal(["Local.jpg"], await WikimediaCommonsMediaSource.ReadP18FileNamesAsync(entity, "Q123"));

        await using var commons = Json("""
            {"query":{"pages":[{"title":"File:Local.jpg","imageinfo":[{
              "url":"https://upload.wikimedia.org/local.jpg","thumburl":"https://upload.wikimedia.org/thumb/local.jpg","sha1":"abc123",
              "extmetadata":{"Artist":{"value":"<b>Autora</b>"},"Credit":{"value":"Obra pròpia"},
              "LicenseShortName":{"value":"CC BY-SA 4.0"},"LicenseUrl":{"value":"https://creativecommons.org/licenses/by-sa/4.0/"}}
            }]}]}}
            """);
        var candidate = await WikimediaCommonsMediaSource.ReadCommonsCandidateAsync(commons, "Q123", "Local.jpg");
        Assert.NotNull(candidate);
        Assert.True(candidate!.IdentityVerified);
        Assert.Equal("Autora", candidate.Author);
        Assert.Equal("CC BY-SA 4.0", candidate.LicenseIdentifier);
        Assert.Contains("Obra pròpia", candidate.Attribution);
        Assert.Equal("abc123", candidate.ContentHash);
    }

    [Fact]
    public void Commons_identity_accepts_only_a_verified_exact_place_qid()
    {
        Assert.Equal("Q100", PlaceMediaRepository.ExtractVerifiedExactPlaceQid(
            """{"place.wikidata.id":"q100","place.wikidata.identity_status":"VerifiedExact"}"""));
        Assert.Null(PlaceMediaRepository.ExtractVerifiedExactPlaceQid(
            """{"place.wikidata.id":"Q100","place.wikidata.identity_status":"PendingReview"}"""));
        Assert.Null(PlaceMediaRepository.ExtractVerifiedExactPlaceQid(
            """{"brand.wikidata.id":"Q200","place.wikidata.identity_status":"VerifiedExact"}"""));
        Assert.Null(PlaceMediaRepository.ExtractVerifiedExactPlaceQid(
            """{"operator.wikidata.id":"Q300","place.wikidata.identity_status":"VerifiedExact"}"""));
        Assert.Null(PlaceMediaRepository.ExtractVerifiedExactPlaceQid(
            """{"wikidata.id":"Q400"}"""));
    }

    [Fact]
    public async Task Uploaded_photo_is_reencoded_without_original_and_with_bounded_webp_variants()
    {
        var result = await new SkiaSharpPlaceMediaProcessor().ProcessAsync(CreateBmp(2400, 1200));

        Assert.Equal(64, result.SourceSha256.Length);
        var optimized = Assert.Single(result.Variants, item => item.Variant == PlaceMediaVariant.Optimized);
        var thumbnail = Assert.Single(result.Variants, item => item.Variant == PlaceMediaVariant.Thumbnail);
        Assert.Equal((1600, 800, "image/webp"), (optimized.Width, optimized.Height, optimized.MimeType));
        Assert.Equal((480, 240, "image/webp"), (thumbnail.Width, thumbnail.Height, thumbnail.MimeType));
        Assert.Equal("RIFF", Encoding.ASCII.GetString(optimized.Content, 0, 4));
        Assert.Equal("WEBP", Encoding.ASCII.GetString(optimized.Content, 8, 4));
    }

    [Fact]
    public async Task Uploaded_payload_must_be_a_real_decodable_image()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SkiaSharpPlaceMediaProcessor().ProcessAsync("not-an-image"u8.ToArray()));
        Assert.Contains("imatge", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static PlaceMediaAsset Asset(PlaceMediaSource source) => new(
        Guid.NewGuid(), Guid.NewGuid(), source, "Organització", "external-1", new Uri("https://example.test/original.jpg"),
        new Uri("https://example.test/served.jpg"), "Autoria", "CC-BY-4.0", "4.0",
        new Uri("https://creativecommons.org/licenses/by/4.0/"), "Autoria · CC BY 4.0",
        DateTimeOffset.UtcNow, true, true, Guid.NewGuid().ToString("N"),
        PlaceMediaVerificationStatus.Verified, true, false, true, "Atribució obligatòria", null, null, null, null);

    private static MemoryStream Json(string value) => new(Encoding.UTF8.GetBytes(value));

    private static byte[] CreateBmp(int width, int height)
    {
        var stride = (width * 3 + 3) & ~3;
        var pixels = stride * height;
        var bytes = new byte[54 + pixels];
        bytes[0] = (byte)'B'; bytes[1] = (byte)'M';
        BitConverter.GetBytes(bytes.Length).CopyTo(bytes, 2);
        BitConverter.GetBytes(54).CopyTo(bytes, 10);
        BitConverter.GetBytes(40).CopyTo(bytes, 14);
        BitConverter.GetBytes(width).CopyTo(bytes, 18);
        BitConverter.GetBytes(height).CopyTo(bytes, 22);
        BitConverter.GetBytes((short)1).CopyTo(bytes, 26);
        BitConverter.GetBytes((short)24).CopyTo(bytes, 28);
        BitConverter.GetBytes(pixels).CopyTo(bytes, 34);
        for (var offset = 54; offset < bytes.Length; offset += 3)
        {
            bytes[offset] = 112; bytes[offset + 1] = 96; bytes[offset + 2] = 12;
        }
        return bytes;
    }
}
