---
title: 'Automations'
description: 'Rules that take care of the busywork, so nobody has to remember to.'
---

An automation is a rule: **when** something happens, **if** the task matches your conditions, **then** do one or more things. Rules run in the background, and every run is recorded.

![Editing an automation rule as a when, if, then flow](/images/automations-light.webp)

![Editing an automation rule as a when, if, then flow](/images/automations-dark.webp)

## Triggers

| Trigger                  | Runs when                                                             |
| ------------------------ | --------------------------------------------------------------------- |
| Task is created          | A new task is added                                                   |
| Task changes             | Fields you choose change, such as status, assignees or tags           |
| Task due date approaches | A task is due today, or in so many days                               |
| Task becomes overdue     | A task passes its due date                                            |
| Task has no due date     | A task is missing a due date                                          |
| Task is unassigned       | A task has had nobody on it for so many days                          |
| Task remains inactive    | A task has had no activity for so many days                           |
| Task becomes blocked     | A task is blocked by an unfinished task                               |
| Task becomes unblocked   | A task is no longer blocked                                           |
| All subtasks complete    | Every subtask of a task is done                                       |
| Sprint starts            | A sprint starts, for every task in it                                 |
| Sprint end approaches    | A sprint ends today, or in so many days, for every task in it         |
| Sprint completes         | A sprint completes, for every task in it                              |
| Pull request opened      | A pull request is opened for a task, optionally into certain branches |
| Pull request merged      | A task's pull request is merged, optionally into certain branches     |
| Checks fail              | Checks fail on a task's linked code                                   |

The three code triggers need [source control](/docs/source-control) connected.

## Conditions

Conditions narrow a rule down to the tasks you care about. Check whether a field equals, doesn't equal or contains a value, whether it's empty, whether it changed, or whether something was added or removed. Group conditions as _all of_, _any of_ or _none of_.

A rule can also be limited to a single project, board or sprint instead of the whole workspace.

## Actions

| Action               | What it does                                                                                                                       |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Notify               | Sends an in-app notification to the assignees, the task owner, the person whose change ran the rule, chosen people or chosen roles |
| Update task          | Changes status, priority, sprint, board group, assignees, tags, owner, estimate, dates, name or description                        |
| Add comment          | Posts a comment on the task                                                                                                        |
| Flag task            | Marks the task for attention                                                                                                       |
| Create task          | Adds a new task, and can link it to the original                                                                                   |
| Manage task relation | Adds or removes a link between tasks                                                                                               |
| Delete task          | Removes the task, straight away or after a delay                                                                                   |

Chain as many actions as you need. Dates can be set relative to the run, in calendar or business days.

A delayed deletion is cancelled if the task moves on before the delay is up, and deleted tasks can still be restored from the archive.

## Who a rule runs as

Each rule runs as a service account and uses that account's permissions. Its changes show up as automation activity, so it's always clear a rule made them.

## Testing a rule

**Test run** checks a rule against any task you pick and shows what it would do, without changing anything. If it wouldn't run, it tells you why: the task is outside the rule's project, the trigger doesn't apply, or the conditions don't match. When you're happy, **Run now** applies the actions to that task for real.

## Run history

Every rule has a run history showing each run, the task it ran against, whether the conditions matched, and how each action went. Filter it by outcome or search for a task.

If a rule fails too many times in a short space of time, it is switched off automatically and says so, so it doesn't keep failing quietly. Fix the cause before switching it back on. A rule that points at something since deleted, like a status or board, warns you too.

## Changing rules

Every change to a rule, including switching it on or off, is recorded in the activity feed and audit log. Changing a rule, switching it off or deleting it cancels any of its delayed actions still waiting to run.

## With the assistant

Describe the rule you want in plain words and the [assistant drafts it](/docs/assistant/proposals). New rules from the assistant start switched off, so you can look them over first.

## Who can manage automations

Admins can create and change automations by default.
