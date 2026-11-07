namespace Lingo.Model.Test;

public class TranslationFormatterTests
{
    private readonly TranslationFormatter _formatter = new();

    private record ItemCountSignature(int Count, string ItemName) : TranslationSignature("items.count");
    private record UserSignature(string Name, string Gender) : TranslationSignature("user.status");

    [Test]
    public void Format_SimpleVariableSubstitution()
    {
        var pattern = "Hello {name}, welcome to {place}!";
        var vars = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["place"] = "Wonderland"
        };

        var result = _formatter.Format(pattern, vars);
        result.Should().Be("Hello Alice, welcome to Wonderland!");
    }

    [Test]
    public void Format_CaseInsensitiveVariableLookup()
    {
        var pattern = "User: {userName}, Score: {score}";
        var vars = new Dictionary<string, object?>
        {
            ["UserName"] = "Bob",
            ["SCORE"] = 100
        };

        var result = _formatter.Format(pattern, vars);
        result.Should().Be("User: Bob, Score: 100");
    }

    [Test]
    public void Format_WithSignature()
    {
        var sig = new ItemCountSignature(3, "apples");
        var pattern = "You have {count} {itemName}.";

        var result = _formatter.Format(pattern, sig);
        result.Should().Be("You have 3 apples.");
    }

    [Test]
    public void Format_WithAnonymousObject()
    {
        var pattern = "Total: {amount}";
        var result = _formatter.Format(pattern, new { amount = 42 });
        result.Should().Be("Total: 42");
    }

    [Test]
    public void Format_EscapingWithSingleQuotes()
    {
        var pattern = "This '{is}' literal and ''single quote'' and '{name}' is not substituted.";
        var vars = new Dictionary<string, object?> { ["name"] = "Bob" };

        var result = _formatter.Format(pattern, vars);
        result.Should().Be("This {is} literal and 'single quote' and {name} is not substituted.");
    }

    [Test]
    public void Format_SelectPattern()
    {
        var pattern = "{gender, select, female {She is online} male {He is online} other {They are online}}";

        _formatter.Format(pattern, new { gender = "female" }).Should().Be("She is online");
        _formatter.Format(pattern, new { gender = "male" }).Should().Be("He is online");
        _formatter.Format(pattern, new { gender = "unknown" }).Should().Be("They are online");
    }

    [Test]
    public void Format_PluralPattern_ExactMatches()
    {
        var pattern = "{count, plural, =0 {No messages} =1 {One message} other {# messages}}";

        _formatter.Format(pattern, new { count = 0 }, CultureInfo.InvariantCulture).Should().Be("No messages");
        _formatter.Format(pattern, new { count = 1 }, CultureInfo.InvariantCulture).Should().Be("One message");
        _formatter.Format(pattern, new { count = 5 }, CultureInfo.InvariantCulture).Should().Be("5 messages");
    }

    [Test]
    public void Format_PluralPattern_WithOffset()
    {
        var pattern = "{count, plural, offset:1 =0 {No one} =1 {You} one {You and one other} other {You and # others}}";

        _formatter.Format(pattern, new { count = 0 }, CultureInfo.InvariantCulture).Should().Be("No one");
        _formatter.Format(pattern, new { count = 1 }, CultureInfo.InvariantCulture).Should().Be("You");
        _formatter.Format(pattern, new { count = 2 }, CultureInfo.InvariantCulture).Should().Be("You and one other");
        _formatter.Format(pattern, new { count = 5 }, CultureInfo.InvariantCulture).Should().Be("You and 4 others");
    }

    [Test]
    public void Format_SelectOrdinal()
    {
        var pattern = "{rank, selectordinal, one {#st} two {#nd} few {#rd} other {#th}} place";

        _formatter.Format(pattern, new { rank = 1 }, new CultureInfo("en-US")).Should().Be("1st place");
        _formatter.Format(pattern, new { rank = 2 }, new CultureInfo("en-US")).Should().Be("2nd place");
        _formatter.Format(pattern, new { rank = 3 }, new CultureInfo("en-US")).Should().Be("3rd place");
        _formatter.Format(pattern, new { rank = 4 }, new CultureInfo("en-US")).Should().Be("4th place");
        _formatter.Format(pattern, new { rank = 21 }, new CultureInfo("en-US")).Should().Be("21st place");
    }

    [Test]
    public void Format_NumberFormatting()
    {
        var pattern = "Price: {price, number, currency}";
        var culture = new CultureInfo("en-US");
        var result = _formatter.Format(pattern, new { price = 1234.5 }, culture);

        result.Should().Be("Price: $1,234.50");
    }

    [Test]
    public void Format_DateAndTime()
    {
        var dt = new DateTime(2026, 11, 7, 14, 30, 0);
        var pattern = "On {d, date, yyyy-MM-dd} at {t, time, HH:mm}";

        var result = _formatter.Format(pattern, new { d = dt, t = dt }, CultureInfo.InvariantCulture);
        result.Should().Be("On 2026-11-07 at 14:30");
    }

    [Test]
    public void Format_NestedSelectAndPlural()
    {
        var pattern = "{gender, select, female {{count, plural, =0 {She has no items} =1 {She has one item} other {She has # items}}} other {{count, plural, =0 {They have no items} other {They have # items}}}}";

        _formatter.Format(pattern, new { gender = "female", count = 0 }).Should().Be("She has no items");
        _formatter.Format(pattern, new { gender = "female", count = 1 }).Should().Be("She has one item");
        _formatter.Format(pattern, new { gender = "female", count = 3 }).Should().Be("She has 3 items");
        _formatter.Format(pattern, new { gender = "other", count = 5 }).Should().Be("They have 5 items");
    }

    [Test]
    public void Format_PluralRules_French()
    {
        // In French, 0 is 'one', 1 is 'one', 2 is 'other'
        var pattern = "{count, plural, one {message} other {messages}}";
        var culture = new CultureInfo("fr-FR");

        _formatter.Format(pattern, new { count = 0 }, culture).Should().Be("message");
        _formatter.Format(pattern, new { count = 1 }, culture).Should().Be("message");
        _formatter.Format(pattern, new { count = 2 }, culture).Should().Be("messages");
    }

    [Test]
    public void Format_PluralRules_Russian()
    {
        // In Russian: 1 -> one, 2-4 -> few, 5-20 -> many, 21 -> one
        var pattern = "{count, plural, one {# яблоко} few {# яблока} many {# яблок} other {# яблока}}";
        var culture = new CultureInfo("ru-RU");

        _formatter.Format(pattern, new { count = 1 }, culture).Should().Be("1 яблоко");
        _formatter.Format(pattern, new { count = 2 }, culture).Should().Be("2 яблока");
        _formatter.Format(pattern, new { count = 5 }, culture).Should().Be("5 яблок");
        _formatter.Format(pattern, new { count = 21 }, culture).Should().Be("21 яблоко");
    }

    [Test]
    public void Format_NumberStyles_PercentAndCustom()
    {
        var pattern = "Progress: {pct, number, percent}, Formatted: {num, number, 0000}";
        var result = _formatter.Format(pattern, new { pct = 0.75, num = 42 }, CultureInfo.InvariantCulture);

        result.Should().Be("Progress: 75.00 %, Formatted: 0042");
    }

    [Test]
    public void Format_MissingVariable_LeavesPlaceholder()
    {
        var pattern = "Hello {missingVar}!";
        var result = _formatter.Format(pattern, new Dictionary<string, object?>());

        result.Should().Be("Hello {missingVar}!");
    }

    [Test]
    public void Format_NullOrEmptyPattern_ReturnsEmpty()
    {
        _formatter.Format("", new { }).Should().BeEmpty();
        _formatter.Format(null!, new { }).Should().BeEmpty();
    }
}
