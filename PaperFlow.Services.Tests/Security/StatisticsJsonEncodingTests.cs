namespace PaperFlow.Web.Tests.Security
{
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using NUnit.Framework;


[TestFixture]
public sealed class StatisticsJsonEncodingTests
{
    [Test]
    public void DefaultEncoder_EscapesHtmlSensitiveInputInInlineScriptData()
    {
        const string maliciousLabel = "</script><script>alert(1)</script>";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Default
        };

        string json = JsonSerializer.Serialize(new[] { new { Label = maliciousLabel, Value = 1 } }, options);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Not.Contain("</script>"));
            Assert.That(json, Does.Not.Contain("<script>"));
            Assert.That(json, Does.Contain("\\u003C"));
        });
    }
}
}

