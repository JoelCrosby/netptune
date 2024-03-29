using System;
using System.Text.Json.Serialization;

using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities;

public abstract record AuditableEntity<TValue> : KeyedEntity<TValue>, IAuditableEntity<TValue>
{
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    #region ForeignKeys

    public string? CreatedByUserId { get; set; }

    public string? ModifiedByUserId { get; set; }

    public string? DeletedByUserId { get; set; }

    public string? OwnerId { get; set; }

    #endregion

    #region NavigationProperties

    [JsonIgnore]
    public AppUser? CreatedByUser { get; set; }

    [JsonIgnore]
    public AppUser? ModifiedByUser { get; set; }

    [JsonIgnore]
    public AppUser? DeletedByUser { get; set; }

    [JsonIgnore]
    public AppUser? Owner { get; set; }

    #endregion

    #region Methods

    public void Delete(string userId)
    {
        IsDeleted = true;
        DeletedByUserId = userId;
    }

    #endregion
}
