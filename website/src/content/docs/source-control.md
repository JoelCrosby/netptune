---
title: 'Source control'
description: 'Connect GitHub and see the branches, commits and pull requests behind every task.'
---

Connect GitHub to Netptune and code finds its own way to the right task. Mention a task in a branch name, commit or pull request, and the task shows what's happening with it: whether it's merged, and whether the checks passed.

![A task's Code tab showing its pull requests, checks and branch](/images/code-light.webp)

## Connecting GitHub

A workspace admin connects GitHub from the workspace's **Source Control** settings. Choose **Connect GitHub**, then pick the account or organisation to install the Netptune app on. A workspace can connect several accounts and organisations.

Then choose **Choose Repositories** to link the repositories you want. Netptune only reads activity in linked repositories and ignores the rest.

When you link a repository, you can also pull in its pull requests and branches from the last 90 days, so tasks you're already working on show their code straight away.

## Linking code to tasks

Put a task's ID, like `NETP-42`, in any of these and it links to that task:

- a **branch name**, such as `netp-42-retry-webhooks`
- a **commit message**
- a **pull request** title or description

IDs are matched against your workspace's real project keys, ignoring case, so `netp-42` works as well as `NETP-42`. Text that only looks like an ID, such as `utf-8`, is left alone.

Commits pushed to a branch pick up that branch's tasks too, so work on `netp-42-retry-webhooks` links even when the commit messages don't repeat the ID. Commits to the default branch don't, otherwise every merge would link everything.

## On the task

A task's **Code** tab lists its linked pull requests, branches and commits:

- **Pull requests** show whether they're open, draft, merged or closed, and how their checks are doing: passed, failed, running or skipped.
- **Branches** show when they've been deleted.
- **Commits** link straight to GitHub.

Code activity also appears in the task's history and in the board's activity feed, and open boards update as it happens.

## Your GitHub account

Commits and pull requests by your GitHub account show up as yours. If you sign in to Netptune with GitHub, your account is added automatically. Otherwise, add it under **Source control accounts** in your profile.

## Automations

Code can drive [automations](/docs/automations). Three triggers react to it:

- **Pull request opened**, optionally only into certain branches
- **Pull request merged**, optionally only into certain branches
- **Checks fail** on the task's linked code

For example: when a pull request is merged into `main`, move the task to Done and let the assignee know.

## The assistant

With repositories linked, the [assistant](/docs/assistant/asking) can read them: recent commits, files, code search and pull requests. It never writes to a repository.

## Disconnecting

Disconnecting an account unlinks its repositories, and Netptune stops reading their activity. To revoke access completely, also uninstall the Netptune app in GitHub.
