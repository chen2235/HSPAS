using HSPAS.Api.Infrastructure;
using HSPAS.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HSPAS.Api.Services;

public class MenuService : IMenuService
{
    private readonly HspasDbContext _db;

    public MenuService(HspasDbContext db)
    {
        _db = db;
    }

    public async Task<List<MenuTreeNode>> GetMenuTreeAsync(CancellationToken ct = default)
    {
        var all = await _db.MenuFunctions
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        var lookup = all.ToLookup(m => m.ParentId);

        List<MenuTreeNode> BuildChildren(long? parentId)
        {
            return lookup[parentId]
                .Select(m => new MenuTreeNode
                {
                    Id = m.Id,
                    ParentId = m.ParentId,
                    Level = m.Level,
                    FuncCode = m.FuncCode,
                    DisplayName = m.DisplayName,
                    RouteUrl = m.RouteUrl,
                    SortOrder = m.SortOrder,
                    IsActive = m.IsActive,
                    Children = BuildChildren(m.Id)
                })
                .ToList();
        }

        return BuildChildren(null);
    }

    public async Task ReorderAsync(List<MenuReorderItem> items, CancellationToken ct = default)
    {
        // 驗證規則
        var idSet = items.Select(i => i.Id).ToHashSet();
        var itemMap = items.ToDictionary(i => i.Id);

        foreach (var item in items)
        {
            if (item.SortOrder <= 0)
                throw new ArgumentException($"Id={item.Id} 的 SortOrder 必須大於 0");

            switch (item.Level)
            {
                case 1:
                    if (item.ParentId != null)
                        throw new ArgumentException($"Id={item.Id}: Level 1 的 ParentId 必須為 NULL");
                    break;
                case 2:
                    if (item.ParentId == null)
                        throw new ArgumentException($"Id={item.Id}: Level 2 的 ParentId 不可為 NULL");
                    if (itemMap.ContainsKey(item.ParentId.Value) && itemMap[item.ParentId.Value].Level != 1)
                        throw new ArgumentException($"Id={item.Id}: Level 2 的 ParentId 必須指向 Level 1");
                    break;
                case 3:
                    if (item.ParentId == null)
                        throw new ArgumentException($"Id={item.Id}: Level 3 的 ParentId 不可為 NULL");
                    if (itemMap.ContainsKey(item.ParentId.Value) && itemMap[item.ParentId.Value].Level != 2)
                        throw new ArgumentException($"Id={item.Id}: Level 3 的 ParentId 必須指向 Level 2");
                    break;
                default:
                    throw new ArgumentException($"Id={item.Id}: Level 必須為 1, 2, 或 3");
            }
        }

        // 批次更新
        var ids = items.Select(i => i.Id).ToList();
        var entities = await _db.MenuFunctions
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(ct);

        foreach (var entity in entities)
        {
            var item = itemMap[entity.Id];
            entity.ParentId = item.ParentId;
            entity.Level = item.Level;
            entity.SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(ct);
    }
}
