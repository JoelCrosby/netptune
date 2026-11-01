---
title: 'Writing in tasks and pages'
description: 'Have the assistant write where you are already typing, then undo it in one step if it misses.'
---

Some things are quicker to write in place than to ask about in a chat. In a task description or a page, the assistant writes right at your cursor.

![The assistant tightening a selected paragraph on a page](/images/assistant-writing-light.webp)

![The assistant tightening a selected paragraph on a page](/images/assistant-writing-dark.webp)

## Ask in place

Type `/ai` at the start of a word, or pick **Ask the assistant** from the `/` menu, then say what you want: a checklist, acceptance criteria, a summary of the notes above. Press Enter to send, Shift+Enter for a new line, or Esc to cancel and put everything back as it was.

- **Watch it write.** The reply types out where you asked, formatted as it goes, including lists, checklists, headings and code blocks. The rest of the text stays editable meanwhile, and Esc stops it.
- **Undo in one step.** The finished reply goes in as a single change, so one undo takes it all out. Your text is saved once the reply is complete, never halfway through.

In a task, the assistant knows the task's name, status, project, sprint, tags and assignees. In a page, it knows the title. Either way it reads the text around your cursor, so it continues in place rather than starting over. In-place writing uses your provider's smaller, faster model.

## In pages

Pages have a few more ways in.

**Select text** and the formatting menu offers:

- **Summarise** — a short summary of the selection
- **Tighten** — the same point in fewer words
- **Translate** — the selection in a language you pick

Each result shows next to the selection first. Choose **Replace selection** or **Insert below**, or dismiss it.

**Turn into tasks** is there too, for a selected checklist. It hands the items to the assistant, which proposes a task for each one, linked back into the page. As with any proposal, you [review them](/docs/assistant/proposals) before anything is created.

**From the `/` menu:**

- **Continue writing** picks up where the page leaves off.
- **Summarise above** writes a summary of everything above the cursor.

**From the Pages home:**

- **Ask** a question across all your pages. The answer links to the pages it used.
- **Draft a page** from a short description, such as "a runbook for rotating the deploy credentials". The assistant proposes it as a new page for you to review.

Page edits the assistant makes from the chat show up in the open page as they are applied, so you don't need to reload.

## Who can use it

It is offered only where you could edit the text yourself, and only to people allowed to use the assistant. The same permissions are checked again when the request runs. Text written this way counts towards the workspace's usage and spending cap, just like chat.
