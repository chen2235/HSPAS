using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSPAS.Api.Entities;

/// <summary>定期定額約定表</summary>
[Table("DcaPlan")]
public class DcaPlan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(100)]
    public string PlanName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string StockId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string StockName { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EndDate { get; set; }

    [MaxLength(20)]
    public string CycleType { get; set; } = string.Empty; // MONTHLY, WEEKLY

    public int CycleDay { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    public ICollection<DcaExecution> Executions { get; set; } = new List<DcaExecution>();
}
