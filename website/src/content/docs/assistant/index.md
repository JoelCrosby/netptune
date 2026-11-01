---
title: 'AI Assistant'
description: 'An assistant that knows your workspace, does the legwork, and asks before it changes anything.'
---

The assistant lives beside your work. Ask it what slipped last sprint, who is overloaded, or what a page says about the release, and it reads your workspace to answer. Ask it to change something and it drafts the change for you to review. Nothing is touched until you apply it.

![The Netptune assistant answering a question about a sprint](/images/assistant-light.webp)

## What it does

:::cards

- [Asking questions](/docs/assistant/asking) — answers from your tasks, sprints, reports, pages, automations, linked repositories and the web.
- [Proposals and review](/docs/assistant/proposals) — every change it suggests arrives as a list you can check, edit, apply in part, and undo.
- [Writing in tasks and pages](/docs/assistant/writing) — type `/ai` where you are writing, or select text to summarise, tighten or translate it.
- [Task review](/docs/assistant/task-review) — a second pair of eyes on a task before work starts, with suggestions you accept or dismiss.
- [Access, privacy and cost](/docs/assistant/access-and-cost) — what it can see, who can see your conversations, and what it costs.

:::

## Turning it on

The assistant runs on an Anthropic or OpenAI key. Netptune adds no charge of its own, and the cost lands on whichever key answered.

- **A workspace key.** An administrator adds one under the workspace's assistant settings, and every member without a key of their own can use it.
- **Your own key.** Add one in personal settings and your requests use it instead of the workspace key. Remove it and you fall back to the workspace key, if there is one.

Keys are encrypted when you save them and never shown again. Each connection also picks the model used for chat and the model used for [task reviews](/docs/assistant/task-review).

Members can use the assistant by default. Viewers cannot, unless an administrator grants them the permission.

## Opening it

Press **Ctrl I**, or use the assistant button in the top bar. It opens as an overlay, and you can dock it beside the page or give it the full window from the display menu. On a task, the task's own assistant button opens it with that task already in context.

In the composer you can:

- **Pick a model** at any point. The conversation carries on with the new one.
- **Choose an effort level**, from low to max, or leave it on automatic. Higher effort spends more time and tokens thinking before it answers.

Replies come back in the language the app is set to.
