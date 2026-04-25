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
      <picture>
        <div data-src="/images/kanban-boards-dark.webp" />
        <div data-src="/images/kanban-boards-light.webp" />

        <img
          width={580}
          height={290}
          class="mt-6 rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/60 inset-ring-brand dark:border-white/15"
          src="/images/kanban-boards-light.webp"
          alt="Screenshot of the Netptune kanbanboards"
        />
      </picture>
    ),
  },
];
