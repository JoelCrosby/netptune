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
    "projects.read",
    "statuses.read",
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
  "scopes": ["projects.read", "statuses.read", "tasks.read", "tasks.create", "tasks.update"],
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

Available v1 routes are:

- `GET /api/v1/projects`
- `GET /api/v1/statuses`
- `GET /api/v1/tasks`
- `GET /api/v1/tasks/{id}`
- `POST /api/v1/tasks`
- `PATCH /api/v1/tasks/{id}`

Interactive documentation is available at `https://api.netptune.co.uk/docs`. The underlying OpenAPI document remains available at `/openapi/v1.json` on the same host for client generation and other tooling.
