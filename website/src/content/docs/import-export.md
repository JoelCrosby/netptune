---
title: 'Import & export'
description: 'Bring your work in from other tools, and take a copy of it whenever you like.'
---

Everything lives under **Data** in the workspace settings: your imports, your exports, and their history.

## Importing tasks

Choose **Create Import** and follow the steps.

1. **Upload a file.** CSV, TSV, Excel, JSON and NDJSON files all work. Netptune reads the file and shows what it found, so check the delimiter, encoding and header row look right, and re-read it if not. Pick the board the tasks should go to.
2. **Map the columns.** Bind each column to a task field, or leave it as _Ignore_. Exports from **Jira**, **Trello** and **Asana** are recognised and mapped for you. For anything else, Netptune suggests a mapping from the column names and contents.
3. **Preview.** See what will be created, updated or skipped before anything changes.
4. **Import.** The import runs in the background and shows its progress.

A finished import can be **undone** for as long as its session is kept, which removes what it created and restores what it changed.

### Help from the assistant

If the suggested mapping isn't quite right, **Improve with assistant** asks the [assistant](/docs/assistant) to have a go. It sees your column names and types, plus a few example values unless an administrator has turned that off in the assistant's privacy settings.

## Exporting tasks

Choose **Create Export**, then:

1. **Choose what to export**: which tasks, filtered as you like, and which fields.
2. **Choose a format**: CSV, TSV, Excel, JSON or NDJSON. Set the delimiter, date format and time zone, and decide how fields with several values (assignees, tags) are written.
3. **Run it.** When the export is ready, download it from the Data page.

Save an export's setup to run the same one again later. Exports are kept for seven days, then removed.

## Moving a whole workspace

An **archive export** packs up the entire workspace (projects, tasks, boards, sprints, pages, comments and the rest) into a single `.nptz` file.

**Import Archive** restores one:

- into a **new workspace**, which you give its own address, or
- into **this workspace**, if it has no projects yet.

Netptune checks the archive first and shows what's in it, along with any warnings. Members from the original workspace who don't have an account here can be invited as part of the import.

## Who can do what

Anyone in the workspace can export tasks. Importing, and archive exports and imports, are for admins by default.
