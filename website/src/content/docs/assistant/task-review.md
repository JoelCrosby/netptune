---
title: 'Task review'
description: 'A second pair of eyes on a task before anyone starts on it.'
---

A review reads a task as a spec and points out what would trip someone up: missing details, unstated assumptions, vague wording, work that belongs in another task. Each point comes with a suggestion you can accept, adjust or dismiss.

![A task review with its verdict, summary and a scope finding](/images/task-review-light.webp)

![A task review with its verdict, summary and a scope finding](/images/task-review-dark.webp)

## Running a review

Open a task and go to its **Review** tab, then choose **Run review**. Reviews only run when someone asks for one.

While it runs, you can see what it is doing: reading the task, checking related work, looking at linked code, writing up what it found. Findings appear as they are ready. Closing the task and reopening it keeps the progress.

A review can use everything the chat assistant can read: other tasks, pages, sprints, linked repositories and, when it is connected, the web. When it finishes, the task gets a verdict of **Ready** or **Needs work**, along with how much it looked at.

## What it looks for

| Finding     | What it means                                                |
| ----------- | ------------------------------------------------------------ |
| Gap         | Something the work needs that the task doesn't say           |
| Assumption  | Something the task takes for granted without stating it      |
| Ambiguity   | Wording two people could read two ways                       |
| Scope       | Too much in one task, or something that doesn't belong in it |
| Overlap     | Work that duplicates or collides with another task           |
| Testability | No clear way to tell when the task is done                   |

## Suggestions

Most findings carry a suggestion. You see a preview first and can edit it before accepting.

- **Edit the description** — new or replacement text, shown as a diff against what is there
- **Split the task** — smaller tasks you can rename or drop, created as children of this one
- **Link a task** — mark another task as related, or this one as its duplicate
- **Fill in details** — a priority, an estimate, or tags (including new ones, if you can create tags)

If the task has changed since the review, the review says so. A description edit whose text has gone can't be applied, so edit by hand or run the review again.

## Deciding on findings

- **Accept** applies the suggestion. A finding without one is there to read; dismiss it once it's handled.
- **Dismiss** sets it aside, with an optional note on why it doesn't apply. Later reviews of the task take your dismissals into account, so the same point doesn't keep coming back.
- **Reopen** brings a dismissed finding back. An accepted description edit can be reverted. Accepted splits, links and details stay, because accepting them twice would create duplicates.

Every decision records who made it, and changes from an accepted suggestion show in the task's history like any other edit. Past reviews stay on the task under **Review history**.

## Which model reviews

Reviews use their own model, set on each connection in the assistant settings, separate from the chat model. They count towards the same usage totals and monthly cap as everything else.
