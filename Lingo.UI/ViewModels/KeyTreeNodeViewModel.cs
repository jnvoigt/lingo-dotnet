using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lingo.Core.Hierarchy;

namespace Lingo.UI.ViewModels;

public partial class KeyTreeNodeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _isLeaf;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _missingCount;

    public KeyTreeNodeViewModel? Parent { get; set; }

    public ObservableCollection<KeyTreeNodeViewModel> Children { get; } = new();

    public static KeyTreeNodeViewModel FromKeyNode(KeyNode node, KeyTreeNodeViewModel? parent = null)
    {
        var vm = new KeyTreeNodeViewModel
        {
            Name = node.Name,
            FullPath = node.FullPath,
            IsLeaf = node.IsLeaf,
            Parent = parent
        };

        foreach (var child in node.Children)
        {
            vm.Children.Add(FromKeyNode(child, vm));
        }

        return vm;
    }

    public string Icon => IsLeaf ? "🏷️" : "📁";

    public string DisplayText => IsLeaf ? $"{Name}" : $"{Name} ({Children.Count})";

    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }

    public void EnsureVisible()
    {
        var current = Parent;
        while (current != null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }
}
