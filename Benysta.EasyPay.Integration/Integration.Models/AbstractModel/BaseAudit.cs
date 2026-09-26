using System.ComponentModel.DataAnnotations;
namespace Integration.Models.AbstractModel;

public abstract class BaseAudit : ISoftDelete
{
    [StringLength(75)] public string CreatedBy { get; set; } = "SYSTEM";
    public DateTime DateCreated { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SoftDeleted { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }
}
