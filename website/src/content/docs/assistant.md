---
title: 'AI Assistant'
description: 'Ask questions about your workspace and review every change before it happens.'
---

The assistant reads your workspace and answers questions about it. When you ask it to change something, it does not act — it writes a proposal you review and apply yourself.

It runs on your own Anthropic or OpenAI key. Netptune adds no charge and no key of its own, so a self-hosted installation works exactly like the hosted one.

![The Netptune assistant answering a question about a sprint](/images/assistant-light.webp)

![The Netptune assistant answering a question about a sprint](/images/assistant-dark.webp)

## Nothing changes without your approval

Every tool that writes is a proposal. The assistant gathers what it needs, states what it intends to change, and stops. You get a review table with a row per change: what it targets, which fields move, and what each one goes from and to.

Approve the ones you want and apply. Skip a row and it is never touched.

- **Review in detail.** Every value is carried as a real id, so assignees, statuses and tags render as pills you can read at a glance and dates in your own format — added in green, removed struck through.
- **Apply partially.** Uncheck anything you disagree with. The rest applies without it.
- **Undo.** An applied change set can be reversed, with the original values restored.
- **Retry.** If one change fails — a task moved, a permission changed — the others still land, and the failures can be retried on their own.

![Reviewing proposed changes before applying them](/images/assistant-proposals-light.webp)

![Reviewing proposed changes before applying them](/images/assistant-proposals-dark.webp)

## It only sees what you see

The assistant runs with your permissions, not its own. Each tool declares what it needs, and you are offered only the tools your role already allows — a member who cannot read automations is never told the automation tools exist. Every request is checked again when it runs.

Attachments are listed, never opened: the assistant can tell you a file is on a task, not what is inside it.

## What it can answer

| Area               | Examples                                                           |
| ------------------ | ------------------------------------------------------------------ |
| Tasks and projects | What is assigned to me, what is blocked, what changed on a task    |
| Sprints            | What is in the current sprint, what slipped, what is unestimated   |
| Reporting          | How did last sprint go, is throughput improving, who is overloaded |
| Automations        | Which rule changed this task, why a rule did not fire              |
| People             | Who owns the workspace, whose invite is still pending              |
| Files              | What is attached to a task, what was uploaded last week            |

Reporting is the one worth calling out. The assistant reads the same flow, workload, burndown and velocity data as the reporting views, so "how did last sprint go" gets a real answer with numbers behind it rather than a guess from task titles.

## Working with it

- **Ask about what you are looking at.** The assistant knows which task, project or sprint is on your screen, so "who is this assigned to" works without naming it.
- **Start from a task.** The task detail view has a button that opens the assistant with that task in context.
- **Steer mid-answer.** Stop a reply that is heading the wrong way, reword your question, or ask for another attempt.
- **Pick a model.** Switch models in the composer at any point. The conversation moves with you.
- **Come back later.** Conversations are saved. Reopen one from history and carry on, or reload the page mid-answer — the reply is still written and waiting.
- **Read it in your language.** Replies come back in the language the app is set to.

## When it asks you something

Where an answer genuinely changes what it would do, the assistant asks instead of guessing. The question arrives as a card of two to four options — pick one and it carries on from there.

Nothing about the card is binding. "Something else" opens a box for an answer in your own words, and you can ignore the card entirely and type in the message box as usual; either way the assistant takes the reply and moves on. A question left behind stays in the transcript as a record of what it offered.

It should not ask often. Anything the assistant can look up for itself — which task you meant, which sprint is running — it looks up.

## What it costs

The assistant shows a running total above the message box — tokens used and estimated spend for the open conversation, priced from published model rates.

While a reply is being prepared, the thinking line counts up how long the turn has been running and the tokens it has spent so far. The finished reply keeps the time it took.

Workspace administrators see the same figures for the whole workspace, per member and per conversation, on the assistant settings page. Every conversation in the workspace can be read there in full.

Nothing is billed through Netptune. The charge lands on whichever API key answered the request.

## Turning it on

Add a provider key in personal settings and the assistant appears. An administrator can add a shared workspace key instead, so members can use it without supplying their own — anyone who has added their own key keeps using it, and their own account.

Administrators can also switch the assistant off for the whole workspace, which stops new messages and blocks pending changes from being applied.

Keys are encrypted at rest and never shown again after they are saved.

:::note
For the settings that shape the harness itself — model catalogue, tool limits, history trimming, rate limits — see [Configuration](/docs/configuration).
:::
