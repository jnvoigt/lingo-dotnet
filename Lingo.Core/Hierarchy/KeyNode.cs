namespace Lingo.Core.Hierarchy;

public class KeyNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsLeaf { get; set; }
    public KeyNode? Parent { get; set; }
    public List<KeyNode> Children { get; } = new();
    public List<Unit> Units { get; } = new();

    public KeyNode? FindChild(string name)
    {
        return Children.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<KeyNode> GetAllLeaves()
    {
        if (IsLeaf)
        {
            yield return this;
        }

        foreach (var child in Children)
        {
            foreach (var leaf in child.GetAllLeaves())
            {
                yield return leaf;
            }
        }
    }

    public IEnumerable<KeyNode> GetAllDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var desc in child.GetAllDescendants())
            {
                yield return desc;
            }
        }
    }
}
