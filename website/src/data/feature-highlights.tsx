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
    eyebrow: 'Automations',
    title: 'Set the rules. Skip the busywork.',
    description:
      'Build trigger-based rules that react when a task changes status, has fields updated, or sits unassigned for too long — then notify assignees, flag the task, or update it automatically.',
    reversed: true,
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Every run is recorded, so you can see exactly which rules fired, on which tasks, and what
        they changed.
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
