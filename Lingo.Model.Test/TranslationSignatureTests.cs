namespace Lingo.Model.Test;

public class TranslationSignatureTests
{
    private record UserGreeting(string UserName, int ItemCount) : TranslationSignature("user.greeting");
    private record SimpleKey() : TranslationSignature("simple.key");
    private record ComplexSignature(string Title, double Price, bool IsActive) : TranslationSignature("catalog.item");

    [Test]
    public void BaseConstructor_SetsKey()
    {
        var sig = new TranslationSignature("test.key");
        sig.Key.Should().Be("test.key");
        sig.ToString().Should().Be("test.key");
    }

    [Test]
    public void BaseConstructor_ThrowsOnNullOrEmptyKey()
    {
        Assert.Throws<ArgumentNullException>(() => new TranslationSignature(null!));
        Assert.Throws<ArgumentException>(() => new TranslationSignature(""));
        Assert.Throws<ArgumentException>(() => new TranslationSignature("   "));
    }

    [Test]
    public void ImplicitConversion_ReturnsKey()
    {
        var greeting = new UserGreeting("Alice", 5);
        string keyStr = greeting;

        keyStr.Should().Be("user.greeting");
    }

    [Test]
    public void GetVariables_ExtractsConstructorProperties()
    {
        var greeting = new UserGreeting("Bob", 12);
        var variables = greeting.GetVariables();

        variables.Should().NotBeNull();
        variables.Should().HaveCount(2);
        variables["UserName"].Should().Be("Bob");
        variables["ItemCount"].Should().Be(12);

        // Case-insensitive lookup
        variables.ContainsKey("username").Should().BeTrue();
        variables["username"].Should().Be("Bob");
    }

    [Test]
    public void GetVariables_ExcludesKeyAndEqualityContract()
    {
        var sig = new SimpleKey();
        var variables = sig.GetVariables();

        variables.Should().BeEmpty();
        variables.ContainsKey("Key").Should().BeFalse();
        variables.ContainsKey("EqualityContract").Should().BeFalse();
    }

    [Test]
    public void GetVariables_WithVariousTypes_ExtractsCorrectly()
    {
        var sig = new ComplexSignature("Laptop", 999.99, true);
        var variables = sig.GetVariables();

        variables.Should().HaveCount(3);
        variables["Title"].Should().Be("Laptop");
        variables["Price"].Should().Be(999.99);
        variables["IsActive"].Should().Be(true);
    }
}
