---
title: 'Proposals and review'
description: 'Every change the assistant suggests waits for you. Check it, adjust it, apply what you want, and undo it later.'
---

When you ask the assistant to change something, it does not act. It gathers what it needs, says what it intends to do, and leaves a set of proposed changes for you to review. Skip a change and it is never touched.

:::note{title="The one exception"}

Text you ask it to write straight into a task description or a page goes in while you watch, and a single undo takes it out. See [Writing in tasks and pages](/docs/assistant/writing).

:::

![Reviewing proposed changes before applying them](/images/assistant-proposals-light.webp)

## Reviewing changes

Open **Review changes** to see every proposal, grouped by the task, page or other item it touches. Each row says what kind of change it is (create, update, move, assign, link, archive and so on) and shows what moves from what to what.

- **Read it at a glance.** Assignees, statuses and tags show as the pills you know from the app, dates in your own format, and how many days a date moves. Added values are green, removed ones struck through.
- **Compare long text properly.** Description and page edits show a word-level diff, side by side or unified, rendered or as source.
- **Edit before applying.** Change a proposed value in place: pick a different assignee, adjust an estimate, reword a title.
- **Ask for a rework.** Send a proposal back with a note and the assistant revises it.
- **Move quickly.** `j` and `k` step through the list, space includes or skips a change, `e` edits it, and Enter applies the selection.

Proposals are checked against the same rules the app applies when you make the change yourself. A change that would break one is marked as one that can't be applied, with the reason.

## Applying, undoing and retrying

- **Apply in part.** Untick anything you disagree with. The rest applies without it.
- **Watch it land.** Large sets apply one change at a time with a progress count, and you can stop after the current one.
- **Retry failures.** If one change fails because a task moved or a permission changed, the others still land. The failures can be retried on their own.
- **Undo.** An applied set can be undone, which restores the original values.

Applied changes show in the activity feed and audit log under your name, marked as made through the assistant.

## What it can change

| Area            | Changes                                                                            |
| --------------- | ---------------------------------------------------------------------------------- |
| Tasks           | Create, update, delete, assign, tag, move between sprints and boards, link, unlink |
| Sprints         | Create, update, start and complete                                                 |
| Boards          | Create, update and delete boards; create, rename, reorder and delete their groups  |
| Projects        | Create, update and delete                                                          |
| Pages           | Create, edit, move, archive, restore, delete, and restore an earlier version       |
| Comments        | Add, edit and delete                                                               |
| Workspace setup | Statuses, tags and relation types                                                  |
| Automations     | Create rules, edit them, and switch them on or off                                 |

It can also resolve a flag on a task. Everything is limited to what your own role lets you change. See [Access, privacy and cost](/docs/assistant/access-and-cost).

## Automation rules

Describe the rule you want ("when a pull request is merged, move the task to Done and tell the assignee") and the assistant drafts it. The review shows the rule as a readable flow of when, if and then, with what was added, changed or removed.

- New rules arrive **switched off** unless you asked for them to be on, so you can look them over first.
- Changing a rule, switching it off or deleting it cancels any of its delayed actions still waiting to run.
- Every rule change, from the assistant or anyone else, is recorded in the activity feed and audit log.

![An automation rule drafted by the assistant, ready to review](/images/assistant-automation-light.webp)

See [Automations](/docs/automations) for the triggers and actions rules can use.
