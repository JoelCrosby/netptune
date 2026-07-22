import type { FeatureHighlight } from '~/types/feature-highlight';

export const featureHighlights: FeatureHighlight[] = [
  {
    eyebrow: 'Kanban boards',
    title: 'The way your team works — visualized.',
    description:
      "Drag tasks between columns, assign them to teammates, and watch the board update for everyone in real time. No refreshing. No confusion about who's doing what.",
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Custom columns, drag-and-drop reordering, multiple boards per project. Structure your
        workflow exactly how your team thinks.
      </p>
    ),
    visual: () => (
      <div class="min-w-0 flex-1">
        <img
          width={580}
          height={290}
          class="mt-6 rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/60 inset-ring-brand dark:hidden"
          src="/images/kanban-boards-light.webp"
          alt="Screenshot of the Netptune kanban boards"
        />
        <img
          width={580}
          height={290}
          class="mt-6 hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/60 inset-ring-brand dark:block"
          src="/images/kanban-boards-dark.webp"
          alt="Screenshot of the Netptune kanban boards"
        />
      </div>
    ),
  },
  {
    eyebrow: 'Calendar',
    title: 'Make every deadline visible.',
    description:
      'Plan scheduled work on a monthly calendar, inspect everything due on a selected day, and reschedule tasks without leaving the view.',
    reversed: true,
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Filter the same schedule by project, sprint, status, priority, assignee, or tag, with an
        unscheduled queue ready for planning.
      </p>
    ),
    visual: () => (
      <div class="min-w-0 flex-1">
        <img
          width={1440}
          height={900}
          loading="lazy"
          class="rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/30 dark:hidden"
          src="/images/calendar-light.webp"
          alt="Netptune calendar view showing scheduled tasks and the selected-day task table"
        />
        <img
          width={1440}
          height={900}
          loading="lazy"
          class="hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/30 dark:block"
          src="/images/calendar-dark.webp"
          alt="Netptune calendar view in dark mode showing scheduled tasks and filters"
        />
      </div>
    ),
  },
  {
    eyebrow: 'Roadmap',
    title: 'See the path from now to shipped.',
    description:
      'Shape the longer view on an editable timeline, adjust task dates in place, and plan work directly from the unscheduled queue.',
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Sprint windows and dependency lines keep blocked work, sequencing, and delivery dates in one
        view.
      </p>
    ),
    visual: () => (
      <div class="min-w-0 flex-1">
        <img
          width={1440}
          height={900}
          loading="lazy"
          class="rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/30 dark:hidden"
          src="/images/roadmap-light.webp"
          alt="Netptune roadmap showing tasks and sprints across an editable timeline"
        />
        <img
          width={1440}
          height={900}
          loading="lazy"
          class="hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/30 dark:block"
          src="/images/roadmap-dark.webp"
          alt="Netptune roadmap in dark mode showing scheduled and unscheduled work"
        />
      </div>
    ),
  },
  {
    eyebrow: 'Automations',
    title: 'Set the rules. Skip the busywork.',
    description:
      'React to precise field changes, approaching due dates, or work left unassigned. Chain actions to notify assignees, add a comment, update fields, flag work, or delete stale tasks after a safety delay.',
    reversed: true,
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Field conditions keep rules targeted, every run is recorded, and delayed deletion is
        cancelled when the task moves on—with archived tasks still recoverable.
      </p>
    ),
    visual: () => (
      <div class="min-w-0 flex-1">
        <img
          width={800}
          height={429}
          class="rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/30 dark:hidden"
          src="/images/automations-light.webp"
          alt="Screenshot of the Netptune automation rule builder"
        />
        <img
          width={800}
          height={429}
          class="hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/30 dark:block"
          src="/images/automations-dark.webp"
          alt="Screenshot of the Netptune automation rule builder"
        />
      </div>
    ),
  },
  {
    eyebrow: 'Sprints',
    title: 'Plan in time-boxes. Ship on schedule.',
    description:
      'Group tasks into time-boxed sprints, start and complete them as a unit, and track progress with live task counts and status breakdowns.',
    visual: () => (
      <div class="min-w-0 flex-1">
        <img
          width={800}
          height={429}
          class="rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/30 dark:hidden"
          src="/images/sprints-light.webp"
          alt="Screenshot of the Netptune sprint backlog"
        />
        <img
          width={800}
          height={429}
          class="hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/30 dark:block"
          src="/images/sprints-dark.webp"
          alt="Screenshot of the Netptune sprint backlog"
        />
      </div>
    ),
  },
];
