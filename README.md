# Netptune

An Open Source self-hostable project management application built for teams. Organise work into projects, boards, and tasks with real-time collaboration.

[**Website**](https://netptune.co.uk)

[![Build App Image](https://github.com/joelcrosby/netptune/actions/workflows/build-image-app.yml/badge.svg)](https://github.com/joelcrosby/netptune/actions/workflows/build-image-app.yml)
[![Build Client Image](https://github.com/joelcrosby/netptune/actions/workflows/build-image-client.yml/badge.svg)](https://github.com/joelcrosby/netptune/actions/workflows/build-image-client.yml)
[![Build Jobs Image](https://github.com/joelcrosby/netptune/actions/workflows/build-image-jobs.yml/badge.svg)](https://github.com/joelcrosby/netptune/actions/workflows/build-image-jobs.yml)

![Netptune screenshot](https://netptune.co.uk/assets/screenshot-web.png)

---

## Built With

[![Angular](https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-FF4438?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![NATS](https://img.shields.io/badge/NATS-27AAE1?style=for-the-badge&logo=natsdotio&logoColor=white)](https://nats.io/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-326CE5?style=for-the-badge&logo=kubernetes&logoColor=white)](https://kubernetes.io/)
[![Helm](https://img.shields.io/badge/Helm-0F1689?style=for-the-badge&logo=helm&logoColor=white)](https://helm.sh/)

---

## Features

- **Workspaces** — separate spaces for different teams or organisations, each with their own members and projects
- **Projects** — group related work and track progress across tasks
- **Kanban boards** — drag-and-drop task management with customisable columns and groups
- **Tasks** — create, assign, tag, comment on, and track the status of individual work items
- **Real-time updates** — live board state via Server-Sent Events so all members see changes instantly
- **Activity feed** — per-board audit log of all changes
- **Authentication** — email/password registration and GitHub OAuth
- **User management** — invite and manage workspace members with role-based access
- **File attachments** — S3-compatible object storage for task attachments
- **Background jobs** — async job processing for notifications and heavy operations

---

## Tech Stack

The frontend is built with **Angular** and **NgRx**, communicating with an **ASP.NET Core** API backed by **PostgreSQL** (via Entity Framework Core) and **Redis** for caching. Real-time board updates are delivered over **Server-Sent Events**.

---

## License

MIT
