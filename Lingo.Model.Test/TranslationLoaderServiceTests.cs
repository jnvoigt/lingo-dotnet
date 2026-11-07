namespace Lingo.Model.Test;

public class TranslationLoaderServiceTests
{
    private static readonly string TestDataDir = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "../../../../TestData/Xliff12");

    [Test]
    public void LoadFromPath_WithExistingGroup_FillsDictionaryWithAllLanguages()
    {
        var sourceFilePath = Path.Combine(TestDataDir, "TestData.xlf");
        if (!File.Exists(sourceFilePath))
        {
            sourceFilePath = Path.GetFullPath("TestData/Xliff12/TestData.xlf");
        }

        var service = new TranslationLoaderService();
        var dict = service.LoadFromPath(sourceFilePath);

        dict.Should().NotBeNull();
        dict.Should().NotBeEmpty();

        // Check keys exist
        dict.ContainsKey("sample.text.that-has-not-changed").Should().BeTrue();

        var textUnit = dict["sample.text.that-has-not-changed"];
        textUnit.Should().NotBeNull();

        // Default culture should be loaded from source file
        textUnit.GetText("").Should().Be("This is text A");

        // German and French translations should be loaded from sibling files
        textUnit.GetText("de-DE").Should().Be("Dies is Text A");
        textUnit.GetText("fr-FR").Should().Be("Ceci est le texte A");
    }

    [Test]
    public void LoadFromDirectory_CrawlsAndLoadsAllGroups()
    {
        var targetDir = TestDataDir;
        if (!Directory.Exists(targetDir))
        {
            targetDir = Path.GetFullPath("TestData/Xliff12");
        }

        var service = new TranslationLoaderService();
        var dict = service.LoadFromDirectory(targetDir);

        dict.Should().NotBeNull();
        dict.Should().NotBeEmpty();
        dict.ContainsKey("sample.text.that-has-not-changed").Should().BeTrue();
    }

    [Test]
    public void FillDictionaryFromJson_ParsesAndMergesEntries()
    {
        var service = new TranslationLoaderService();
        var dict = new TranslationDictionary();

        var json = """
        {
            "login.header": {
                "": "Welcome back",
                "de-DE": "Willkommen zurück",
                "fr-FR": "Bienvenue"
            },
            "login.button": {
                "": "Sign In",
                "de-DE": "Anmelden"
            }
        }
        """;

        service.FillDictionaryFromJson(dict, json);

        dict.Count.Should().Be(2);
        dict.GetText("login.header", "").Should().Be("Welcome back");
        dict.GetText("login.header", "de-DE").Should().Be("Willkommen zurück");
        dict.GetText("login.header", "fr-FR").Should().Be("Bienvenue");
        dict.GetText("login.button", "de-DE").Should().Be("Anmelden");
    }

    [Test]
    public void LoadFromDocument_PopulatesFromSingleDocument()
    {
        using var tempDir = new TestDirectory();
        var filePath = tempDir.CreateFile("translations.de-DE.xlf");
        File.WriteAllText(filePath.FullName, """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
                <file source-language="en" target-language="de-DE" datatype="plaintext" original="app.txt">
                    <body>
                        <trans-unit id="btn.ok">
                            <source>OK</source>
                            <target>Bestätigen</target>
                        </trans-unit>
                    </body>
                </file>
            </xliff>
            """);

        var service = new TranslationLoaderService();
        var dict = service.LoadFromPath(filePath.FullName);

        dict.Should().NotBeNull();
        dict.ContainsKey("btn.ok").Should().BeTrue();
        dict.GetText("btn.ok", "de-DE").Should().Be("Bestätigen");
    }
}
