# Public API v1

Netptune's public API uses user-owned service accounts. A service account belongs to one workspace, has one or more human owners, and cannot sign in through password or OAuth flows.

## Create a service account

Call the management API with a human user's bearer token and the workspace header:

```http
POST /api/service-accounts
Authorization: Bearer <user-token>
workspace: my-workspace
Content-Type: application/json

{
  "name": "Codex",
  "description": "Tracks implementation work",
  "permissions": [
    "members.read",
    "projects.read",
    "statuses.read",
    "sprints.read",
    "sprints.create",
    "sprints.update",
    "sprints.delete",
    "sprints.manage_tasks",
    "tags.assign",
    "tasks.read",
    "tasks.create",
    "tasks.update"
  ]
}
```

The creator becomes an owner. Additional workspace users can be supplied in `ownerUserIds`.

## Create a credential

```http
POST /api/service-accounts/{serviceAccountId}/credentials
Authorization: Bearer <user-token>
workspace: my-workspace
Content-Type: application/json

{
  "name": "Local Codex",
  "scopes": ["members.read", "projects.read", "statuses.read", "sprints.read", "sprints.create", "sprints.update", "sprints.delete", "sprints.manage_tasks", "tags.assign", "tasks.read", "tasks.create", "tasks.update"],
  "expiresAt": "2027-01-01T00:00:00Z"
}
```

The response contains the token once. Store it as a secret. Netptune stores only its SHA-256 hash. Credentials can be listed with `GET /api/service-accounts` and revoked with `DELETE /api/service-accounts/{serviceAccountId}/credentials/{credentialId}`.

## Use the public API

The public API is hosted independently from the interactive application. Aspire exposes its local launch profile at `http://localhost:7500` and `https://localhost:7501`; the Kubernetes Gateway routes the external `/api/v1` path to its dedicated service.

Send the credential using the `ApiKey` authorization scheme. The workspace is fixed by the credential and must not be supplied by the client.

```http
GET /api/v1/projects
Authorization: ApiKey ntp_<credential-id>_<secret>
```

Create a task:

```http
POST /api/v1/tasks
Authorization: ApiKey ntp_<credential-id>_<secret>
Content-Type: application/json

{
  "name": "Implement reporting event ledger",
  "description": "Add the append-only storage foundation",
  "projectId": 12
}
```

Discover valid task assignees without exposing member email addresses or other profile data:

```http
GET /api/v1/assignees?search=joel&page=1&pageSize=25
Authorization: ApiKey ntp_<credential-id>_<secret>
```

Bulk-update tasks. Every supplied task and assignee must belong to the credential's workspace. When `sprintId` is supplied, every task must belong to that sprint's project.

```http
POST /api/v1/tasks/bulk-update
Authorization: ApiKey ntp_<credential-id>_<secret>
Content-Type: application/json

{
  "taskIds": [41, 42],
  "assigneeIds": ["410e28ea-0f01-47a4-b889-68d283f39aa7"],
  "sprintId": 7,
  "priority": 3
}
```

Add and remove existing tasks from a sprint:

```http
POST /api/v1/sprints/7/tasks
Authorization: ApiKey ntp_<credential-id>_<secret>
Content-Type: application/json

{
  "taskIds": [41, 42]
}
```

```http
DELETE /api/v1/sprints/7/tasks/41
Authorization: ApiKey ntp_<credential-id>_<secret>
```

`PATCH /api/v1/tasks/{id}` replaces task tags when `tags` is supplied and requires `tags.assign` in addition to `tasks.update`. Tag names must already exist in the credential's workspace; use an empty array to clear all tags. Invalid, duplicate, or cross-workspace tag values return `400` instead of being ignored.

Available v1 routes are:

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

Interactive documentation is available at `https://api.netptune.co.uk/docs`. The underlying OpenAPI document remains available at `/openapi/v1.json` on the same host for client generation and other tooling.
