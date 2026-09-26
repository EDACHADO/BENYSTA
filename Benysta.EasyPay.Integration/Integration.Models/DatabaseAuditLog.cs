using System.ComponentModel.DataAnnotations;

namespace Integration.Models;

public class DatabaseAuditLog
{
    /// <summary>ULID primary key. See <see cref="EntityId"/>.</summary>
    [Key]
    [StringLength(EntityId.Length)]
    public string DatabaseAuditLogId { get; set; }
    public string TableName { get; set; }
    public string RecordId { get; set; }
    public string OperationType { get; set; }
    public string MetaData { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Succeded { get; set; }
    public string Author { get; set; }
    public DateTime? DateModified { get; set; }
    public string ErrorMessage { get; set; }
}