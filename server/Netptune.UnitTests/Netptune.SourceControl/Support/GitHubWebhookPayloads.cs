namespace Netptune.UnitTests.Netptune.SourceControl.Support;

internal static class GitHubWebhookPayloads
{
    private const string Repository = """
        {
          "id": 4242,
          "name": "netptune",
          "full_name": "acme/netptune",
          "private": true,
          "archived": false,
          "html_url": "https://github.com/acme/netptune",
          "default_branch": "main",
          "owner": { "id": 11, "login": "acme", "type": "Organization" }
        }
        """;

    private const string Sender = """
        { "id": 501, "login": "octocat", "type": "User", "avatar_url": "https://avatars.githubusercontent.com/u/501" }
        """;

    private const string Installation = """
        { "id": 987654 }
        """;

    public const string Push = $$"""
        {
          "ref": "refs/heads/feature/netp-564-search",
          "before": "1111111111111111111111111111111111111111",
          "after": "3333333333333333333333333333333333333333",
          "created": false,
          "deleted": false,
          "forced": false,
          "commits": [
            {
              "id": "2222222222222222222222222222222222222222",
              "message": "Add search index netp-564",
              "timestamp": "2026-10-16T08:10:00+01:00",
              "url": "https://github.com/acme/netptune/commit/2222222222222222222222222222222222222222",
              "author": { "name": "Octo Cat", "email": "octo@example.com", "username": "octocat" }
            },
            {
              "id": "3333333333333333333333333333333333333333",
              "message": "Fix search paging",
              "timestamp": "2026-10-16T08:20:00+01:00",
              "url": "https://github.com/acme/netptune/commit/3333333333333333333333333333333333333333",
              "author": { "name": "Octo Cat", "email": "octo@example.com", "username": "octocat" }
            }
          ],
          "repository": {{Repository}},
          "sender": {{Sender}},
          "installation": {{Installation}}
        }
        """;

    public const string PushToTag = $$"""
        {
          "ref": "refs/tags/v1.0.0",
          "deleted": false,
          "forced": false,
          "commits": [
            {
              "id": "2222222222222222222222222222222222222222",
              "message": "Release",
              "timestamp": "2026-10-16T08:10:00Z",
              "url": "https://github.com/acme/netptune/commit/2222222222222222222222222222222222222222",
              "author": { "name": "Octo Cat", "email": "octo@example.com", "username": "octocat" }
            }
          ],
          "repository": {{Repository}},
          "sender": {{Sender}},
          "installation": {{Installation}}
        }
        """;

    public const string PushDeletingBranch = $$"""
        {
          "ref": "refs/heads/feature/netp-564-search",
          "deleted": true,
          "forced": false,
          "commits": [],
          "repository": {{Repository}},
          "sender": {{Sender}},
          "installation": {{Installation}}
        }
        """;

    public const string CreateBranch = $$"""
        {
          "ref": "feature/netp-564-search",
          "ref_type": "branch",
          "master_branch": "main",
          "repository": {{Repository}},
          "sender": {{Sender}},
          "installation": {{Installation}}
        }
        """;

    public const string CreateTag = $$"""
        {
          "ref": "v1.0.0",
          "ref_type": "tag",
          "repository": {{Repository}},
          "sender": {{Sender}},
          "installation": {{Installation}}
        }
        """;

    public static string PullRequest(string action, bool merged, string state = "closed")
    {
        var mergedAt = merged ? "\"2026-10-16T09:30:00Z\"" : "null";
        var mergedFlag = merged ? "true" : "false";

        return $$"""
            {
              "action": "{{action}}",
              "number": 42,
              "pull_request": {
                "id": 9001,
                "number": 42,
                "title": "netp-564 Search index",
                "body": "Closes netp-564",
                "state": "{{state}}",
                "draft": false,
                "merged": {{mergedFlag}},
                "created_at": "2026-10-15T12:00:00Z",
                "updated_at": "2026-10-16T09:30:00Z",
                "closed_at": "2026-10-16T09:30:00Z",
                "merged_at": {{mergedAt}},
                "html_url": "https://github.com/acme/netptune/pull/42",
                "head": { "ref": "feature/netp-564-search", "sha": "3333333333333333333333333333333333333333" },
                "base": { "ref": "main", "sha": "1111111111111111111111111111111111111111" },
                "user": {{Sender}}
              },
              "repository": {{Repository}},
              "sender": {{Sender}},
              "installation": {{Installation}}
            }
            """;
    }

    public static string CheckRun(string action, string status = "completed", string conclusion = "\"failure\"")
    {
        return $$"""
            {
              "action": "{{action}}",
              "check_run": {
                "id": 777,
                "name": "build",
                "status": "{{status}}",
                "conclusion": {{conclusion}},
                "head_sha": "3333333333333333333333333333333333333333",
                "html_url": "https://github.com/acme/netptune/runs/777",
                "completed_at": "2026-10-16T09:45:00Z",
                "pull_requests": [ { "number": 42 } ]
              },
              "repository": {{Repository}},
              "sender": {{Sender}},
              "installation": {{Installation}}
            }
            """;
    }

    public const string InstallationDeleted = $$"""
        {
          "action": "deleted",
          "installation": {
            "id": 987654,
            "account": { "id": 11, "login": "acme", "type": "Organization" }
          },
          "sender": {{Sender}}
        }
        """;

    public const string InstallationRepositoriesAdded = $$"""
        {
          "action": "added",
          "installation": {
            "id": 987654,
            "account": { "id": 11, "login": "acme", "type": "Organization" }
          },
          "repository_selection": "selected",
          "repositories_added": [
            { "id": 5150, "name": "website", "full_name": "acme/website", "private": false }
          ],
          "repositories_removed": [],
          "sender": {{Sender}}
        }
        """;

    public const string Star = $$"""
        {
          "action": "created",
          "repository": {{Repository}},
          "sender": {{Sender}},
          "installation": {{Installation}}
        }
        """;
}
