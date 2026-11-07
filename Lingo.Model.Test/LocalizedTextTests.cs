using System.Text.Json;

namespace Lingo.Model.Test;

public class LocalizedTextTests
{
    private record WelcomeSignature(string Name) : TranslationSignature("msg.welcome");

    [Test]
    public void DefaultConstructor_IsEmpty()
    {
        var text = new LocalizedText();
        text.Count.Should().Be(0);
        text.DefaultText.Should().BeNull();
    }

    [Test]
    public void SingleTextConstructor_SetsDefaultCulture()
    {
        var text = new LocalizedText("Hello World");
        text.Count.Should().Be(1);
        text.DefaultText.Should().Be("Hello World");
        text[""].Should().Be("Hello World");
        text.GetText().Should().Be("Hello World");
    }

    [Test]
    public void DictionaryInterface_ImplementsCoreOperations()
    {
        var text = new LocalizedText
        {
            [""] = "Save",
            ["de-DE"] = "Speichern",
            ["fr-FR"] = "Enregistrer"
        };

        text.Count.Should().Be(3);
        text.ContainsKey("de-DE").Should().BeTrue();
        text.ContainsKey("DE-de").Should().BeTrue(); // Case-insensitive
        text["de-DE"].Should().Be("Speichern");

        text.TryGetValue("fr-FR", out var fr).Should().BeTrue();
        fr.Should().Be("Enregistrer");

        text.Remove("fr-FR").Should().BeTrue();
        text.Count.Should().Be(2);

        text.Keys.Should().Contain("");
        text.Keys.Should().Contain("de-DE");
        text.Values.Should().Contain("Save");
        text.Values.Should().Contain("Speichern");
    }

    [Test]
    public void GetText_FallbackHierarchy_WorksCorrectly()
    {
        var text = new LocalizedText
        {
            [""] = "Default text",
            ["de"] = "German text",
            ["de-DE"] = "German (Germany) text"
        };

        // Exact match
        text.GetText("de-DE").Should().Be("German (Germany) text");

        // Parent culture match for "de-CH" (falls back to "de")
        text.GetText("de-CH").Should().Be("German text");

        // Fallback to default culture "" for unknown language
        text.GetText("it-IT").Should().Be("Default text");

        // Direct request for default culture
        text.GetText("").Should().Be("Default text");
        text.GetText((string?)null).Should().Be("Default text");
    }

    [Test]
    public void GetText_WithCultureInfo_ResolvesHierarchy()
    {
        var text = new LocalizedText
        {
            [""] = "Fallback text",
            ["en"] = "English text",
            ["en-GB"] = "British text"
        };

        text.GetText(new CultureInfo("en-GB")).Should().Be("British text");
        text.GetText(new CultureInfo("en-US")).Should().Be("English text");
        text.GetText(new CultureInfo("fr-FR")).Should().Be("Fallback text");
        text.GetText(CultureInfo.InvariantCulture).Should().Be("Fallback text");
    }

    [Test]
    public void GetTextOrDefault_ReturnsDefaultWhenMissing()
    {
        var text = new LocalizedText();
        text.GetTextOrDefault("ja-JP", "Not Found").Should().Be("Not Found");
    }

    [Test]
    public void JsonSerialization_SerializesToDictionaryFormat()
    {
        var text = new LocalizedText
        {
            [""] = "Hello",
            ["de-DE"] = "Hallo",
            ["fr-FR"] = "Bonjour"
        };

        var json = text.ToJson();

        json.Should().Contain("\"\":");
        json.Should().Contain("\"de-DE\":");
        json.Should().Contain("\"fr-FR\":");

        // Deserialize back
        var deserialized = LocalizedText.FromJson(json);
        deserialized.Should().NotBeNull();
        deserialized[""].Should().Be("Hello");
        deserialized["de-DE"].Should().Be("Hallo");
        deserialized["fr-FR"].Should().Be("Bonjour");
    }

    [Test]
    public void JsonSerializer_StandardSerialization_ProducesJsonDictionary()
    {
        var text = new LocalizedText
        {
            ["en-US"] = "Hello {name}",
            ["de-DE"] = "Hallo {name}",
            [""] = "Hi {name}"
        };

        var json = JsonSerializer.Serialize(text);
        var restored = JsonSerializer.Deserialize<LocalizedText>(json);

        restored.Should().NotBeNull();
        restored.Equals(text).Should().BeTrue();
        restored!["en-US"].Should().Be("Hello {name}");
        restored["de-DE"].Should().Be("Hallo {name}");
        restored[""].Should().Be("Hi {name}");
    }

    [Test]
    public void Format_WithTranslationSignature_FormatsText()
    {
        var text = new LocalizedText
        {
            [""] = "Welcome, {name}!",
            ["de-DE"] = "Willkommen, {name}!"
        };

        var sig = new WelcomeSignature("Alice");

        text.Format(sig, new CultureInfo("de-DE")).Should().Be("Willkommen, Alice!");
        text.Format(sig, new CultureInfo("en-US")).Should().Be("Welcome, Alice!");
    }
}
