using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>功能選單（三層式樹狀結構）</summary>
[Table("MenuFunction")]
public class MenuFunction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public int Level { get; set; } // 1, 2, 3

    [MaxLength(50)]
    public string FuncCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RouteUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? Remark { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
