---
title: 'Queries'
description: 'Build a view of your tasks once, save it, and share it with the team.'
---

A query is a saved list of tasks: a set of conditions, the columns you want to see, and the order to read them in. Build one for "my overdue work", "bugs in this sprint" or "everything waiting on review", and it's always one click away.

## Building a query

Choose **Create Query** and add conditions. Each condition picks a field, a comparison and a value:

- **Fields** include status, project, sprint, assignee, tags, board, priority, dates, task relations and text.
- **Comparisons** depend on the field: _is_, _is any of_, _is none of_, _contains_, _starts with_, _is empty_, _is before_, _is between_, _is in the last_ or _next_ so many days, _is overdue_, and more.

Group conditions to say whether a task must match all of them or any of them, and nest groups for anything trickier. The results update as you build, so you can see what you'll get before you save.

Then pick the columns to show and how to sort them.

## Saving and sharing

Give the query a name and save it.

- **Private** queries are just for you.
- **Shared** queries are for the whole workspace. Sharing needs the shared-queries permission, which admins have by default.

If you change someone else's shared query and save it, you get your own copy and theirs stays as it was.

**Copy link** shares a query as a URL, even one you haven't saved yet, which is handy for a quick "here's what I'm looking at".

## Pinning

Pin the queries you use most to the sidebar so they're always close by.

## Working with the results

Select tasks in a query's results to bulk edit them or delete them in one go.

## In pages

A query can be embedded in a [page](/docs/pages) as a live task list. Type `/` and choose **Live task list**. The list keeps itself up to date, and readers only see the tasks they have access to.

## When a query needs attention

If something a query relies on goes away, such as a deleted status or a renamed tag, the query says so and points at the condition that needs fixing.
