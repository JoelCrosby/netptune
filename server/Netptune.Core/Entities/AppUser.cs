using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;

using Netptune.Core.Authentication;
using Netptune.Core.BaseEntities;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.Entities;

public class AppUser : IdentityUser, IKeyedEntity<string>
{
    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string? PictureUrl { get; set; }

    public string DisplayName => GetDisplayName();

    public AuthenticationProvider AuthenticationProvider { get; set; }

    private string GetDisplayName()
    {
        if (string.IsNullOrWhiteSpace(Firstname) && string.IsNullOrWhiteSpace(Lastname))
            return UserName!;

        return $"{Firstname} {Lastname}";
    }

    public UserViewModel ToViewModel()
    {
        return new()
        {
            Id = Id,
            Firstname = Firstname,
            Lastname = Lastname,
            PictureUrl = PictureUrl,
            DisplayName = DisplayName,
            Email = Email!,
            UserName = UserName!,
            LastLoginTime = LastLoginTime,
            RegistrationDate = RegistrationDate,
        };
    }

    public WorkspaceUserViewModel ToWorkspaceViewModel(string workspaceOwnerId)
    {
        return new()
        {
            Id = Id,
            Firstname = Firstname,
            Lastname = Lastname,
            PictureUrl = PictureUrl,
            DisplayName = DisplayName,
            Email = Email!,
            UserName = UserName!,
            LastLoginTime = LastLoginTime,
            RegistrationDate = RegistrationDate,
            IsWorkspaceOwner = workspaceOwnerId == Id,
        };
    }

    #region NavigationProperties

    [JsonIgnore]
    public DateTime? LastLoginTime { get; set; }

    [JsonIgnore]
    public DateTime? RegistrationDate { get; set; }

    [JsonIgnore]
    public ICollection<WorkspaceAppUser> WorkspaceUsers { get; init; } = new HashSet<WorkspaceAppUser>();

    [JsonIgnore]
    public ICollection<ProjectUser> ProjectUsers { get; init; } = new HashSet<ProjectUser>();

    [JsonIgnore]
    public ICollection<ProjectTask> Tasks { get; init; } = new HashSet<ProjectTask>();

    [JsonIgnore]
    public ICollection<Workspace> Workspaces { get; init; } = new HashSet<Workspace>();

    [JsonIgnore]
    public ICollection<ProjectTaskAppUser> ProjectTaskAppUsers { get; set; } = new HashSet<ProjectTaskAppUser>();

    #endregion
}
