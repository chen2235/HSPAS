using HSPAS.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HSPAS.Api.Controllers;

/// <summary>功能選單 API</summary>
[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>取得完整三層選單樹</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var tree = await _menuService.GetMenuTreeAsync(ct);
        return Ok(tree);
    }

    /// <summary>儲存拖拉排序與階層變更</summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] List<MenuReorderItem> items, CancellationToken ct)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new { success = false, error = "請提供至少一筆資料" });

        try
        {
            await _menuService.ReorderAsync(items, ct);
            return Ok(new { success = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
