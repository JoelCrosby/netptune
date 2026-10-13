---
title: 'API'
description: 'Create scoped service accounts and connect external tools to Netptune safely.'
---

Netptune's API uses service accounts owned by workspace members. Each service account belongs to one workspace, has explicit permissions, and cannot sign in through password or OAuth flows.

Interactive documentation is available at `https://api.netptune.co.uk/docs`. The OpenAPI document is available at `https://api.netptune.co.uk/openapi/v1.json` for client generation and other tooling.

## Create a service account

Workspace owners and administrators can create and manage service accounts from workspace settings. Choose only the permissions the integration needs, then create a credential with equal or narrower scopes.

Credentials are shown once. Store the token as a secret: Netptune stores only its SHA-256 hash. Credentials can be given an expiry date and revoked independently without deleting the service account.

## Authenticate

Send the credential using the `ApiKey` authorization scheme. The workspace is fixed by the credential and must not be supplied by the client.

```http
GET /api/v1/projects
Authorization: ApiKey ntp_<credential-id>_<secret>
```

For a self-hosted installation, use the API host configured in the Kubernetes Gateway. Aspire exposes local launch profiles at `http://localhost:7500` and `https://localhost:7501`.

## Create a task

```http
POST /api/v1/tasks
Authorization: ApiKey ntp_<credential-id>_<secret>
Content-Type: application/json

{
  "name": "Publish the release notes",
  "description": "Summarise the completed sprint",
  "projectId": 12
}
```

## Identify tasks

Tasks are identified by their system id, the project key and task number shown in the app, such as `NPT-42`. Task routes take it in the path, and bulk routes take a `systemIds` array:

```http
POST /api/v1/tasks/bulk-update
Authorization: ApiKey ntp_<credential-id>_<secret>
Content-Type: application/json

{
  "systemIds": ["NPT-42", "NPT-43"],
  "priority": 3
}
```

Task responses carry `systemId` and no other task identifier. Moving a task to another project gives it that project's key and a new number, and renaming a project's key changes the system id of every task in it, so re-read `systemId` after either change rather than keeping the old one. A request that names a task the workspace does not have returns `404` and lists the system ids it could not find.

## Search and filter tasks

`GET /api/v1/tasks` accepts `search`, `statusIds`, `priorities`, `assignees`, and `tags` query parameters. Statuses use IDs returned by `GET /api/v1/statuses`, assignees use IDs returned by `GET /api/v1/assignees`, and tags use their workspace names.

```http
GET /api/v1/tasks?search=release&statusIds=4&priorities=3&assignees=410e28ea-0f01-47a4-b889-68d283f39aa7&tags=Documentation
Authorization: ApiKey ntp_<credential-id>_<secret>
```

Repeat a filter parameter to match any supplied value within that field. Different filter fields are combined, so a task must match every supplied field.

The boolean `hasTags`, `hasFlags`, and `noSprint` parameters select tasks by presence rather than value. Pass `hasTags=false` to list tasks that carry no tags at all, or `hasTags=true` for those that carry at least one.

```http
GET /api/v1/tasks?hasTags=false&pageSize=100
Authorization: ApiKey ntp_<credential-id>_<secret>
```

## Available routes

- `GET /api/v1/projects`
- `GET /api/v1/assignees`
- `GET /api/v1/statuses`
- `GET /api/v1/sprints`
- `GET /api/v1/sprints/{id}`
- `POST /api/v1/sprints`
- `PATCH /api/v1/sprints/{id}`
- `DELETE /api/v1/sprints/{id}`
- `POST /api/v1/sprints/{id}/tasks`
- `DELETE /api/v1/sprints/{id}/tasks/{systemId}`
- `GET /api/v1/tasks`
- `GET /api/v1/tasks/{systemId}`
- `POST /api/v1/tasks`
- `POST /api/v1/tasks/bulk-update`
- `PATCH /api/v1/tasks/{systemId}`

## Permission model

API credentials support member-read, project, status, sprint, and task scopes. Access is constrained twice: first by the service account's permissions and then by the credential's scopes. Revoking a credential takes effect without changing the owning users or other credentials.

Use `GET /api/v1/assignees` with `members.read` to resolve task assignee IDs without exposing email addresses. `POST /api/v1/tasks/bulk-update` updates multiple workspace-scoped tasks without a board identifier. Sprint membership is managed with `POST /api/v1/sprints/{id}/tasks` and `DELETE /api/v1/sprints/{id}/tasks/{systemId}`, both guarded by `sprints.manage_tasks`.

When `tags` is supplied to `PATCH /api/v1/tasks/{systemId}`, it replaces the task's tags and requires `tags.assign` in addition to `tasks.update`. Every tag must already exist in the credential's workspace; an empty array clears the tags, while invalid or cross-workspace values return `400`.

The API is deployed separately from the interactive application. It shares Netptune's PostgreSQL, Valkey, and NATS services but can be exposed and scaled independently.
