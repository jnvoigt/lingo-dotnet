namespace Lingo.Core.Hierarchy;

public class KeyHierarchyBuilder
{
    private readonly char _delimiter;

    public KeyHierarchyBuilder(char delimiter = '.')
    {
        _delimiter = delimiter;
    }

    public List<KeyNode> Build(IEnumerable<string> keys)
    {
        var rootNodes = new List<KeyNode>();

        foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct())
        {
            InsertKey(rootNodes, key);
        }

        SortNodes(rootNodes);
        return rootNodes;
    }

    public List<KeyNode> Build(IEnumerable<Unit> units)
    {
        var rootNodes = new List<KeyNode>();

        foreach (var unit in units.Where(u => !string.IsNullOrWhiteSpace(u.Id)))
        {
            var node = InsertKey(rootNodes, unit.Id);
            node.Units.Add(unit);
        }

        SortNodes(rootNodes);
        return rootNodes;
    }

    public List<KeyNode> Build(IEnumerable<ILingoDocument> documents)
    {
        var rootNodes = new List<KeyNode>();

        foreach (var doc in documents)
        {
            foreach (var unit in doc.GetAllUnits())
            {
                if (string.IsNullOrWhiteSpace(unit.Id)) continue;
                var node = InsertKey(rootNodes, unit.Id);
                node.Units.Add(unit);
            }
        }

        SortNodes(rootNodes);
        return rootNodes;
    }

    private KeyNode InsertKey(List<KeyNode> rootNodes, string key)
    {
        var segments = key.Split(_delimiter, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            segments = [key];
        }

        List<KeyNode> currentList = rootNodes;
        KeyNode? currentParent = null;
        var accumulatedPath = new StringBuilder();

        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (accumulatedPath.Length > 0)
            {
                accumulatedPath.Append(_delimiter);
            }
            accumulatedPath.Append(segment);

            var isLast = (i == segments.Length - 1);
            var node = currentList.FirstOrDefault(n => string.Equals(n.Name, segment, StringComparison.OrdinalIgnoreCase));

            if (node == null)
            {
                node = new KeyNode
                {
                    Name = segment,
                    FullPath = accumulatedPath.ToString(),
                    IsLeaf = isLast,
                    Parent = currentParent
                };
                currentList.Add(node);
            }
            else if (isLast)
            {
                node.IsLeaf = true;
            }

            currentParent = node;
            currentList = node.Children;
        }

        return currentParent!;
    }

    private static void SortNodes(List<KeyNode> nodes)
    {
        nodes.Sort((a, b) =>
        {
            if (a.Children.Count > 0 && b.Children.Count == 0) return -1;
            if (a.Children.Count == 0 && b.Children.Count > 0) return 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                SortNodes(node.Children);
            }
        }
    }
}
