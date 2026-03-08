namespace HSPAS.Api.Services.Interfaces;

public interface IMenuService
{
    Task<List<MenuTreeNode>> GetMenuTreeAsync(CancellationToken ct = default);
    Task ReorderAsync(List<MenuReorderItem> items, CancellationToken ct = default);
}

public class MenuTreeNode
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public int Level { get; set; }
    public string FuncCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? RouteUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<MenuTreeNode> Children { get; set; } = new();
}

public class MenuReorderItem
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
}
