export interface FeatureTab {
  label: string;
  eyebrow: string;
  title: string;
  description: string;
  extra: string;
  image: string;
  width: number;
  height: number;
}

export const featureTabs: FeatureTab[] = [
  {
    label: 'Boards',
    eyebrow: 'Kanban boards',
    title: 'The way your team works, on one board.',
    description:
      "Drag tasks between columns, assign them to teammates, and watch the board update for everyone as it happens. No refreshing, and no wondering who's doing what.",
    extra:
      'Make your own columns, keep several boards per project, and put a task on more than one board when it belongs to both.',
    image: '/images/kanban-boards-light.webp',
    width: 1352,
    height: 840,
  },
  {
    label: 'Sprints',
    eyebrow: 'Sprints',
    title: 'Plan in time-boxes. Ship on schedule.',
    description: "Gather the work into a sprint, start it, and keep an eye on how it's going.",
    extra: 'When it ends, anything unfinished moves on with you.',
    image: '/images/sprints-light.webp',
    width: 1600,
    height: 900,
  },
  {
    label: 'Calendar',
    eyebrow: 'Calendar',
    title: 'Make every deadline visible.',
    description:
      "See what's due on a monthly calendar, check everything due on a particular day, and move work to a new date without leaving the view.",
    extra:
      'Filter by project, sprint, person or tag, and plan the unscheduled work from a list right beside it.',
    image: '/images/calendar-light.webp',
    width: 1600,
    height: 900,
  },
  {
    label: 'Roadmap',
    eyebrow: 'Roadmap',
    title: 'See the path from now to shipped.',
    description:
      'Lay out the bigger picture on a timeline, nudge dates as plans change, and pull unscheduled work in when there is room.',
    extra:
      "Sprints and dependencies sit on the same timeline, so it's easy to spot what is waiting on what.",
    image: '/images/roadmap-light.webp',
    width: 1600,
    height: 900,
  },
  {
    label: 'Pages',
    eyebrow: 'Pages',
    title: 'Write it down where the work lives.',
    description:
      'Specs, runbooks, meeting notes and onboarding guides sit right next to the tasks they talk about. Link to tasks with an @, and roll back to any earlier version.',
    extra:
      "Pages nobody has looked at in a while get flagged for a check-up, so your docs don't quietly go stale.",
    image: '/images/pages-light.webp',
    width: 1600,
    height: 900,
  },
  {
    label: 'Code',
    eyebrow: 'GitHub',
    title: 'See the code behind every task.',
    description:
      'Put a task ID in a branch name, commit or pull request. It links itself to the right task, so you can see whether it is merged and whether the checks passed.',
    extra: 'Merged a pull request? An automation can move the task along and let the team know.',
    image: '/images/code-light.webp',
    width: 1600,
    height: 900,
  },
  {
    label: 'Automations',
    eyebrow: 'Automations',
    title: 'Set the rules. Skip the busywork.',
    description:
      'Nudge people when something is due, tidy up work nobody has picked up, or move a task along when its pull request merges.',
    extra: 'Every run is recorded, and the assistant can draft a rule for you from a sentence.',
    image: '/images/automations-light.webp',
    width: 1600,
    height: 900,
  },
];
