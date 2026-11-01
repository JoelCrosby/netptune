---
title: 'Asking questions'
description: 'Ask about your work in plain words and get answers read from the workspace itself.'
---

The assistant answers from what is actually in your workspace. It looks things up as it goes, and the work log under each reply shows what it read and how long it took.

## What you can ask about

| Area               | Examples                                                                |
| ------------------ | ----------------------------------------------------------------------- |
| Tasks and projects | What is assigned to me, what is blocked, what changed on a task         |
| Sprints            | What is in the current sprint, what slipped, what is still unestimated  |
| Reporting          | How did last sprint go, is throughput improving, who is overloaded      |
| Pages              | What the onboarding guide says, which pages mention the billing service |
| Automations        | Which rule changed this task, why a rule did not fire                   |
| Code               | Which pull requests touch this task, where a function is defined        |
| The web            | What changed in a library's latest release, what an error message means |
| People             | Who owns the workspace, whose invite is still pending                   |
| Files              | What is attached to a task, what was uploaded last week                 |

Reporting is the one worth calling out. The assistant reads the same flow, workload, burndown and velocity data as the reporting views, in tasks, story points or hours. "How did last sprint go" gets an answer with real numbers behind it, not a guess from task titles.

## Ask about what you are looking at

The assistant knows what is on your screen: the task, project, sprint, board, page or report you have open. "Who is this assigned to" works without naming the task.

Chips above the message box show what is going along with your question. Remove any you don't want before you send, and bring them back if you change your mind.

Text you select in a page goes along too, so "is this paragraph still true?" knows which paragraph you mean.

## Code and the web

When a [repository is linked](/docs/source-control), the assistant can read it: list recent commits, browse and open files, search the code, and read a pull request with its changes. It never writes to a repository.

With a web search provider connected (Brave, Google or SearXNG, set up by an administrator), it can search the web and read the pages it finds. Without one, it can still read a page when you give it the link.

![The assistant reading a web page and comparing it with a plan](/images/assistant-research-light.webp)

![The assistant reading a web page and comparing it with a plan](/images/assistant-research-dark.webp)

## When it asks you something

Where your answer genuinely changes what it would do, the assistant asks instead of guessing. The question arrives as a card of two to four options. Pick one and it carries on.

Nothing about the card is binding. "Something else" opens a box for an answer in your own words, or you can ignore the card and type in the message box as usual. Anything it can look up for itself, like which sprint is running, it looks up.

## Steering the conversation

- **Stop** a reply that is heading the wrong way with Esc or the stop button.
- **Edit** your last question. Sending it again replaces the reply.
- **Regenerate** a reply for another attempt, or **copy** it.
- **Come back later.** Conversations are saved. Reopen one from history, or reload the page mid-answer and the reply is still there.

Very long conversations drop their oldest messages from what the assistant remembers, and a note in the chat marks where that happened. Start a new chat when you change topic.
