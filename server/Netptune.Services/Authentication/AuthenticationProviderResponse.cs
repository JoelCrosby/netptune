using System;
using System.Text.Json.Serialization;

namespace Netptune.Services.Authentication;

public class GithubUserResponse
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = null!;

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = null!;

    [JsonPropertyName("avatar_url")]
    public Uri AvatarUrl { get; set; } = null!;

    [JsonPropertyName("gravatar_id")]
    public string GravatarId { get; set; } = null!;

    [JsonPropertyName("url")]
    public Uri Url { get; set; } = null!;

    [JsonPropertyName("html_url")]
    public Uri HtmlUrl { get; set; } = null!;

    [JsonPropertyName("followers_url")]
    public Uri FollowersUrl { get; set; } = null!;

    [JsonPropertyName("following_url")]
    public string FollowingUrl { get; set; } = null!;

    [JsonPropertyName("gists_url")]
    public string GistsUrl { get; set; } = null!;

    [JsonPropertyName("starred_url")]
    public string StarredUrl { get; set; } = null!;

    [JsonPropertyName("subscriptions_url")]
    public Uri SubscriptionsUrl { get; set; } = null!;

    [JsonPropertyName("organizations_url")]
    public Uri OrganizationsUrl { get; set; } = null!;

    [JsonPropertyName("repos_url")]
    public Uri ReposUrl { get; set; } = null!;

    [JsonPropertyName("events_url")]
    public string EventsUrl { get; set; } = null!;

    [JsonPropertyName("received_events_url")]
    public Uri ReceivedEventsUrl { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("company")]
    public string Company { get; set; } = null!;

    [JsonPropertyName("blog")]
    public Uri Blog { get; set; } = null!;

    [JsonPropertyName("location")]
    public string Location { get; set; } = null!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = null!;

    [JsonPropertyName("twitter_username")]
    public string TwitterUsername { get; set; } = null!;

    [JsonPropertyName("public_repos")]
    public long PublicRepos { get; set; }

    [JsonPropertyName("public_gists")]
    public long PublicGists { get; set; }

    [JsonPropertyName("followers")]
    public long Followers { get; set; }

    [JsonPropertyName("following")]
    public long Following { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
