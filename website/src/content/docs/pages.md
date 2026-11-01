---
title: 'Pages'
description: 'Specs, runbooks and notes that live next to the work they describe.'
---

Pages are your workspace's shared docs. Write specs, meeting notes, runbooks and onboarding guides alongside the tasks they're about, so nobody has to go looking in another tool.

![A page with its outline and the workspace page tree](/images/pages-light.webp)

![A page with its outline and the workspace page tree](/images/pages-dark.webp)

## Organising pages

The Pages sidebar holds a tree of pages and folders. Drag to rearrange, filter by title, and star the pages you visit often to keep them under **Favourites**.

- **Workspace pages** are for everyone in the workspace.
- **Project spaces** give each project its own tree of pages.

A folder opens as a list or a card grid of the pages inside it, sortable by title, status or last edit.

## Starting a page

Create a blank page or start from a template:

| Template      | What's in it                      |
| ------------- | --------------------------------- |
| Meeting notes | Agenda, notes, action items       |
| Spec          | Goals, requirements, linked tasks |
| RFC           | Problem, proposal, open questions |
| Runbook       | Steps, rollback, owners           |
| Retro         | Went well, didn't, actions        |
| From a sprint | Release notes from completed work |

You can also import existing Markdown files from the Pages home, or describe a page and have the [assistant draft it](/docs/assistant/writing).

## Writing

The editor handles the usual formatting: headings, lists, checklists, links, quotes, callouts and code blocks with syntax highlighting. Type `/` for the command menu. Prefer typing Markdown? Switch the editor to Markdown mode from the page toolbar.

- **Link to work.** Type `@` to mention a task or another page. Each page lists the tasks it links to and the pages that link back to it.
- **Embed a live task list.** The `/` menu can drop in a [saved query](/docs/queries). It stays up to date as the tasks change.
- **See who's around.** The toolbar shows who else is viewing the page and who has edited it.
- **Get help writing.** Select text to summarise, tighten or translate it, or type `/ai` to have the assistant write at your cursor. See [Writing in tasks and pages](/docs/assistant/writing).

The outline in the side panel follows your headings and highlights where you are as you scroll. Toggle full width or hide the side panel when you want more room.

## Drafts and publishing

New pages start as drafts. Publish one when it's ready, and tag it so it's easy to find. Everything you write is searchable from the workspace search.

## Version history

Netptune keeps a version of the page as you work, grouping a quick run of edits by the same person into one. Open **Version history** to compare any version with the one before it, side by side or inline, down to the word. Restoring an older version adds it as a new version, so nothing is lost.

## Keeping pages fresh

The Pages home has a **Needs review** list of published pages that may be out of date:

- not reviewed in six months
- nobody has opened them in 90 days
- they link to a task or page that has since been deleted

Give a page a once-over and choose **Mark reviewed** to clear it from the list.

## Sharing, exporting and archiving

- **Share** copies a link to the page.
- **Export as Markdown** downloads the page as a `.md` file.
- **Archive** moves a page, and anything inside it, to the archive. Restore it from there, or delete it for good.

In a [public workspace](/docs), pages can be shared with visitors too. They can read but not edit.
