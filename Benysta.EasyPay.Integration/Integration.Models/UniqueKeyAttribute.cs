using System.ComponentModel.DataAnnotations;

namespace Integration.Models;

/// <summary>
///     Used on an EntityFramework Entity class to mark a property to be used as a Unique Key
/// </summary>
/// <remarks>
///     Marker attribute for unique key
/// </remarks>
/// <param name="groupId">Optional, used to group multiple entity properties together into a combined Unique Key</param>
/// <param name="order">Optional, used to order the entity properties that are part of a combined Unique Key</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class UniqueKeyAttribute(string groupId = null, int order = 0) : ValidationAttribute
{
    public string GroupId { get; set; } = groupId;
    public int Order { get; set; } = order;
}

