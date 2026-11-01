import type { FeatureHighlight } from '~/types/feature-highlight';

function Extra(props: { text: string }) {
  return <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">{props.text}</p>;
}

function Screenshot(props: {
  name: string;
  alt: string;
  width: number;
  height: number;
  class?: string;
}) {
  return (
    <div class="min-w-0 flex-1">
      <img
        width={props.width}
        height={props.height}
        loading="lazy"
        class={`rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/30 dark:hidden ${props.class ?? ''}`}
        src={`/images/${props.name}-light.webp`}
        alt={props.alt}
      />
      <img
        width={props.width}
        height={props.height}
        loading="lazy"
        class={`hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/30 dark:block ${props.class ?? ''}`}
        src={`/images/${props.name}-dark.webp`}
        alt={props.alt}
      />
    </div>
  );
}

export const featureHighlights: FeatureHighlight[] = [
  {
    eyebrow: 'Pages',
    title: 'Write it down where the work lives.',
    description:
      'Specs, runbooks, meeting notes and onboarding guides sit right next to the tasks they talk about. Start from a template, link to tasks and other pages with an @, and roll back to any earlier version.',
    extra: () => (
      <Extra text="Pages nobody has looked at in a while get flagged for a check-up, so your docs don't quietly go stale." />
    ),
    visual: () => (
      <Screenshot
        name="pages"
        width={1600}
        height={900}
        alt="A Netptune page with its outline and the workspace page tree"
      />
    ),
  },
  {
    eyebrow: 'Kanban boards',
    title: 'The way your team works, on one board.',
    description:
      "Drag tasks between columns, assign them to teammates, and watch the board update for everyone as it happens. No refreshing, and no wondering who's doing what.",
    reversed: true,
    extra: () => (
      <Extra text="Make your own columns, keep several boards per project, and put a task on more than one board when it belongs to both." />
    ),
    visual: () => (
      <Screenshot
        name="kanban-boards"
        width={1352}
        height={840}
        alt="Screenshot of the Netptune kanban boards"
      />
    ),
  },
  {
    eyebrow: 'GitHub',
    title: 'See the code behind every task.',
    description:
      'Connect GitHub and put a task ID in a branch name, commit or pull request. It links itself to the right task, so you can see whether it is merged and whether the checks passed.',
    extra: () => (
      <Extra text="Merged a pull request? An automation can move the task along and let the team know." />
    ),
    visual: () => (
      <Screenshot
        name="code"
        width={1600}
        height={900}
        alt="A task's Code tab listing its pull requests, checks and branch"
      />
    ),
  },
  {
    eyebrow: 'Calendar',
    title: 'Make every deadline visible.',
    description:
      "See what's due on a monthly calendar, check everything due on a particular day, and move work to a new date without leaving the view.",
    reversed: true,
    extra: () => (
      <Extra text="Filter by project, sprint, person or tag, and plan the unscheduled work from a list right beside it." />
    ),
    visual: () => (
      <Screenshot
        name="calendar"
        width={1600}
        height={900}
        alt="Netptune calendar view showing scheduled tasks"
      />
    ),
  },
  {
    eyebrow: 'Roadmap',
    title: 'See the path from now to shipped.',
    description:
      'Lay out the bigger picture on a timeline, nudge dates as plans change, and pull unscheduled work in when there is room.',
    extra: () => (
      <Extra text="Sprints and dependencies sit on the same timeline, so it's easy to spot what is waiting on what." />
    ),
    visual: () => (
      <Screenshot
        name="roadmap"
        width={1600}
        height={900}
        alt="Netptune roadmap showing tasks and sprints on a timeline"
      />
    ),
  },
  {
    eyebrow: 'Automations',
    title: 'Set the rules. Skip the busywork.',
    description:
      'Nudge people when something is due, tidy up work nobody has picked up, or move a task along when its pull request merges. Rules run quietly in the background while you get on with things.',
    reversed: true,
    extra: () => (
      <Extra text="Every run is recorded, and the assistant can draft a rule for you from a sentence." />
    ),
    visual: () => (
      <Screenshot
        name="automations"
        width={1600}
        height={900}
        alt="Screenshot of the Netptune automation rule builder"
      />
    ),
  },
  {
    eyebrow: 'Sprints',
    title: 'Plan in time-boxes. Ship on schedule.',
    description:
      "Gather the work into a sprint, start it, and keep an eye on how it's going. When it ends, anything unfinished moves on with you.",
    visual: () => (
      <Screenshot
        name="sprints"
        width={1600}
        height={900}
        alt="Screenshot of the Netptune sprint backlog"
      />
    ),
  },
];
