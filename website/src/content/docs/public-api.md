---
title: 'Public API'
description: 'Create scoped service accounts and connect external tools to Netptune safely.'
---

Netptune's public API uses service accounts owned by workspace members. Each service account belongs to one workspace, has explicit permissions, and cannot sign in through password or OAuth flows.

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

For a self-hosted installation, use the public API host configured in the Kubernetes Gateway. Aspire exposes local launch profiles at `http://localhost:7500` and `https://localhost:7501`.

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
- `DELETE /api/v1/sprints/{id}/tasks/{taskId}`
- `GET /api/v1/tasks`
- `GET /api/v1/tasks/{id}`
- `POST /api/v1/tasks`
- `POST /api/v1/tasks/bulk-update`
- `PATCH /api/v1/tasks/{id}`

## Permission model

Public API credentials support member-read, project, status, sprint, and task scopes. Access is constrained twice: first by the service account's permissions and then by the credential's scopes. Revoking a credential takes effect without changing the owning users or other credentials.

Use `GET /api/v1/assignees` with `members.read` to resolve task assignee IDs without exposing email addresses. `POST /api/v1/tasks/bulk-update` updates multiple workspace-scoped tasks without a board identifier. Sprint membership is managed with `POST /api/v1/sprints/{id}/tasks` and `DELETE /api/v1/sprints/{id}/tasks/{taskId}`, both guarded by `sprints.manage_tasks`.

When `tags` is supplied to `PATCH /api/v1/tasks/{id}`, it replaces the task's tags and requires `tags.assign` in addition to `tasks.update`. Every tag must already exist in the credential's workspace; an empty array clears the tags, while invalid or cross-workspace values return `400`.

The public API is deployed separately from the interactive application. It shares Netptune's PostgreSQL, Valkey, and NATS services but can be exposed and scaled independently.
