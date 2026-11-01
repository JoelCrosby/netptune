---
title: 'Access, privacy and cost'
description: 'The assistant sees only what you can see, leaves a trail of what it did, and shows you what it costs.'
---

## It works as you

The assistant runs with your permissions, not its own. It is offered only the tools your role already allows, so a member who can't read automations is never told the automation tools exist. Every request is checked again when it runs, and again when a change is applied.

- **Files are listed, never opened.** The assistant can tell you a file is on a task, not what is inside it.
- **Repositories are read-only.** It can read linked code and pull requests, but never push, comment or merge.
- **Web access is a separate permission.** Members have it by default, and administrators can take it away.

## Everything it does is on the record

Changes made through the assistant show in the activity feed and audit log under the person who applied them, marked with the assistant that proposed them. A human edit and an assistant edit are never blurred into one entry.

## Who can read conversations

Conversations are saved so you can pick them up later. You can delete your own from personal settings.

Conversations are workspace records, like comments, so administrators can read every conversation in the workspace and delete any of them from the assistant settings page. Keep that in mind before pasting anything you wouldn't put in a task.

## What it costs

Nothing is billed through Netptune. The charge lands on whichever provider key answered.

- **In the chat**, a running total above the message box shows the tokens used and the estimated cost of the open conversation, priced from published model rates. While a reply is being prepared, the work log counts up the time and tokens spent so far.
- **For the workspace**, administrators see spend for the month on the assistant settings page, broken down per member and per conversation, with a daily chart and a projected month-end figure.

Chat, in-place writing and task reviews all count towards the same totals.

## Spend cap

Administrators can set a monthly spend cap in US dollars. As it gets close, a banner shows how much is left. Once it is reached, the assistant stops accepting new messages and reviews until the next month. Leave the cap empty for no limit.

## Switching it off

Administrators can turn the assistant off for the whole workspace. That stops new messages and blocks pending changes from being applied. Keys and spend history are kept, so turning it back on picks up where you left off.

When the assistant helps map columns during an [import](/docs/import-export), it sends a few example cell values along with the column names. The privacy setting on the assistant settings page turns that off, so only column names and types are sent.
