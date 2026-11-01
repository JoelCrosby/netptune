import {
  ChartNoAxesCombined,
  FileText,
  GitPullRequest,
  History,
  Kanban,
  LayoutTemplate,
  ListFilter,
  MessageSquare,
  Workflow,
} from 'lucide-solid';
import type { Feature } from '~/types/feature';

export const features: Feature[] = [
  {
    icon: Kanban,
    title: 'Plan from every angle',
    description:
      'Flip between boards, a calendar and a roadmap without rebuilding the plan. Changes show up for everyone straight away.',
  },
  {
    icon: FileText,
    title: 'Docs that live with the work',
    description:
      'Pages keep your specs, notes and runbooks next to the tasks they describe, with templates and full version history.',
  },
  {
    icon: GitPullRequest,
    title: 'Code, connected',
    description:
      'Branches, commits and pull requests from GitHub land on the right task, along with whether the checks passed.',
  },
  {
    icon: ListFilter,
    title: 'Save the views you keep rebuilding',
    description:
      'Build a query once, save it, share it with the team, and pin it where you can find it again.',
  },
  {
    icon: Workflow,
    title: 'Hand off the busywork',
    description:
      'Rules nudge people, tidy up stale work and move tasks along, so nobody has to remember to.',
  },
  {
    icon: ChartNoAxesCombined,
    title: 'Know how delivery is going',
    description:
      'Throughput, cycle time, burndown, velocity and workload, counted in tasks, story points or hours.',
  },
  {
    icon: History,
    title: 'See what changed, and who changed it',
    description:
      'Every edit is in the history, including the ones the assistant helped with and the code that landed.',
  },
  {
    icon: MessageSquare,
    title: 'Keep the conversation on the task',
    description:
      'Comments, mentions, files, estimates and links between tasks all live in one place, not in another chat thread.',
  },
  {
    icon: LayoutTemplate,
    title: 'Start with a workflow, not a blank page',
    description:
      'Pick a template for software, content or general work and get statuses, tags and a board ready to go.',
  },
];
