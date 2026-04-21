using AwesomeAssertions;
using Lingo.Core.Models;
using Lingo.Core.Test.Infrastructure;
using System.Linq;

namespace Lingo.Core.Test.Infrastructure;

public class InMemoryLingoDocumentTests
{
    [Test]
    public void SortByKey_ShouldSortAlphabetically()
    {
        // Arrange
        var doc = new InMemoryLingoDocument();
        doc.AddUnit(new Unit { Id = "z", Source = "z" });
        doc.AddUnit(new Unit { Id = "a", Source = "a" });
        doc.AddUnit(new Unit { Id = "m", Source = "m" });

        // Act
        doc.SortByKey();

        // Assert
        var unitIds = doc.GetAllUnits().Select(u => u.Id).ToList();
        unitIds.Should().ContainInOrder("a", "m", "z");
    }
}
