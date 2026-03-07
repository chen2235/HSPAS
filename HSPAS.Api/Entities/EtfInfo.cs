using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>ETF 基本資訊維度表</summary>
[Table("EtfInfo")]
public class EtfInfo
{
    [Key]
    [MaxLength(10)]
    public string EtfId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EtfName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Issuer { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
