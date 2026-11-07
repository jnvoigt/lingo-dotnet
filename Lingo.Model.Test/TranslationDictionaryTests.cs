using System.Text.Json;

namespace Lingo.Model.Test;

public class TranslationDictionaryTests
{
    private record GreetingSignature(string Name) : TranslationSignature("app.greeting");

    [Test]
    public void AddTranslation_CreatesAndUpdatesLocalizedText()
    {
        var dict = new TranslationDictionary();

        dict.AddTranslation("btn.save", "", "Save");
        dict.AddTranslation("btn.save", "de-DE", "Speichern");

        dict.Count.Should().Be(1);
        dict.ContainsKey("btn.save").Should().BeTrue();

        dict.GetText("btn.save", "").Should().Be("Save");
        dict.GetText("btn.save", "de-DE").Should().Be("Speichern");
        dict.GetText("btn.save", new CultureInfo("de-DE")).Should().Be("Speichern");
        dict.GetText("btn.save", new CultureInfo("fr-FR")).Should().Be("Save"); // Fallback to default
    }

    [Test]
    public void Get_ResolvesTextBasedOnThreadCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var dict = new TranslationDictionary();
            dict.AddTranslation("btn.cancel", "en-US", "Cancel");
            dict.AddTranslation("btn.cancel", "de-DE", "Abbrechen");

            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            dict.Get("btn.cancel").Should().Be("Abbrechen");

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            dict.Get("btn.cancel").Should().Be("Cancel");

            dict.Get("btn.cancel", new CultureInfo("de-DE")).Should().Be("Abbrechen");
            dict.Get("btn.cancel", "de-DE").Should().Be("Abbrechen");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Test]
    public void Get_WithoutDefaultCulture_PicksAnyTranslation()
    {
        var dict = new TranslationDictionary();
        dict.AddTranslation("btn.ok", "de-DE", "OK (DE)");

        dict.Get("btn.ok", new CultureInfo("ja-JP")).Should().Be("OK (DE)");
        dict.GetText("btn.ok", "ja-JP").Should().Be("OK (DE)");
    }

    [Test]
    public void Format_WithSignature_FormatsCorrectly()
    {
        var dict = new TranslationDictionary();
        dict.AddTranslation("app.greeting", "", "Hello, {name}!");
        dict.AddTranslation("app.greeting", "de-DE", "Hallo, {name}!");

        var sig = new GreetingSignature("Max");

        dict.Format(sig, new CultureInfo("de-DE")).Should().Be("Hallo, Max!");
        dict.Format(sig, new CultureInfo("en-US")).Should().Be("Hello, Max!");
    }

    [Test]
    public void Format_WithKeyAndObject_FormatsCorrectly()
    {
        var dict = new TranslationDictionary();
        dict.AddTranslation("items.count", "", "{count, plural, =0 {no items} =1 {one item} other {# items}}");

        dict.Format("items.count", new { count = 0 }).Should().Be("no items");
        dict.Format("items.count", new { count = 1 }).Should().Be("one item");
        dict.Format("items.count", new { count = 7 }).Should().Be("7 items");
    }

    [Test]
    public void JsonSerialization_SerializesAndDeserializesEntireDictionary()
    {
        var dict = new TranslationDictionary();
        dict.AddTranslation("key1", "", "Text 1");
        dict.AddTranslation("key1", "de-DE", "Text 1 DE");
        dict.AddTranslation("key2", "", "Text 2");

        var json = dict.ToJson();
        var restored = TranslationDictionary.FromJson(json);

        restored.Should().NotBeNull();
        restored.Count.Should().Be(2);
        restored.GetText("key1", "de-DE").Should().Be("Text 1 DE");
        restored.GetText("key2", "").Should().Be("Text 2");
    }
}
