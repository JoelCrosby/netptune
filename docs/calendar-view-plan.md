# Calendar view implementation plan

## Outcome

Add a dedicated `/calendar` workspace feature focused on a month view. It will reuse the existing scheduled-task query and update workflow while presenting work in a seven-column calendar rather than a horizontal roadmap.

## MVP scope

- Month grid with previous, today, next, and refresh controls.
- Project and sprint filters stored in URL query parameters.
- Tasks with start and due dates rendered as multi-day ranges.
- Start-only and due-only tasks rendered as single-day items.
- Stable lanes for ranges crossing days and week boundaries.
- Day overflow indicator and selected-day agenda on narrow screens.
- Existing task-detail dialog when a task is selected.
- Permission-aware schedule updates with optimistic state and rollback.
- Real-time refresh through the workspace task SSE group.
- Keyboard navigation, meaningful labels, and visible focus states.

Week/day layouts, time-of-day events, recurring tasks, external calendar sync, and a dedicated unscheduled-task tray are follow-up work.

## Architecture

Create a lazy-loaded `features/calendar` area containing the routed view, toolbar, planning-month coordinator, month grid, selected-day datatable, task item, models, and pure date/layout utilities.

Extract reusable scheduled-task models and task-scheduling behavior from Roadmap so Calendar does not depend directly on another feature. Continue using `GET /api/roadmap` for the month grid because it already returns scheduled tasks overlapping a date range and supports project and sprint filters. Load the paginated selected-day datatable from `GET /api/calendar/tasks`, including its active date, project, sprint, paging, and sorting parameters. Continue using `PUT /api/tasks` for schedule changes.

Use Angular signals for URL-derived state and layout, `permissionResource` for loading, `linkedSignal` for optimistic task placement, and the existing SSE service for refresh notifications.

## Interaction rules

- Moving a ranged task preserves its duration.
- Moving a start-only or due-only task preserves its single-boundary meaning.
- Failed updates restore the previous schedule and show an error.
- Users without `tasks.update` can inspect but not reschedule tasks.
- Dragging is supplemented by a keyboard-accessible move-to-date action.
- Completed tasks remain visible but are visually muted.

## Delivery sequence

1. Extract the shared schedule contract, resource, and update service without changing Roadmap behavior.
2. Add the Calendar route, guard, sidebar link, URL state, loading/error states, and read-only month layout.
3. Add task detail, optimistic rescheduling, rollback, permission checks, and real-time refresh.
4. Add overflow handling, responsive agenda behavior, and complete grid keyboard navigation.
5. Add focused unit/component/router coverage and run formatting, lint, tests, and the production build.

## Definition of done

- Calendar can be opened independently from workspace navigation.
- Its URL restores the displayed month and active filters.
- Every supported schedule shape renders on the correct dates.
- Multi-day tasks clip correctly at visible and weekly boundaries.
- Task detail and scheduling respect permissions and recover from failed updates.
- Real-time updates refresh safely around pending changes.
- The view is usable by keyboard, screen reader, and on narrow screens.
- Existing Roadmap behavior remains unchanged.

## Product decision

The initial view will use the same Sunday-first convention as the existing date picker. A locale or workspace-level first-day-of-week preference should be introduced before changing both controls together.
