using Xunit;
using Zuppeto.Infrastructure.Places;

namespace Backend.Activation.Tests;

public sealed class HtmlVenuePageParserTests
{
    [Fact]
    public void ExtractJsonLdFacts_PrioritizesFactualStructuredFields()
    {
        const string html = """
            <script type="application/ld+json">
            {"@type":"Hotel","name":"Sa Voga","telephone":"937000000","petsAllowed":true,"openingHours":["Mo-Fr 09:00-18:00"]}
            </script>
            """;

        var facts = HtmlVenuePageParser.ExtractJsonLdFacts(html);

        Assert.Contains("Hotel", facts);
        Assert.Contains("Sa Voga", facts);
        Assert.Contains("petsAllowed: True", facts);
        Assert.Contains("Mo-Fr 09:00-18:00", facts);
    }

    [Theory]
    [InlineData("User-agent: *\nDisallow: /private", "/private/place", false)]
    [InlineData("User-agent: *\nDisallow: /private", "/public/place", true)]
    [InlineData("User-agent: Other\nDisallow: /", "/place", true)]
    public void IsAllowedByRobotsTxt_AppliesWildcardRules(string robots, string path, bool expected) =>
        Assert.Equal(expected, HtmlVenuePageParser.IsAllowedByRobotsTxt(robots, path));
}
