import {
  Braces,
  ChartNoAxesCombined,
  ClipboardList,
  FileText,
  Kanban,
  LayoutTemplate,
  MessageSquare,
  Workflow,
  Zap,
} from 'lucide-solid';
import type { Feature } from '~/types/feature';

export const features: Feature[] = [
  {
    icon: Kanban,
    title: 'Plan from every angle',
    description:
      'Move between custom Kanban boards, a month calendar, and an editable roadmap without rebuilding the plan.',
  },
  {
    icon: Zap,
    title: 'Live by default',
    description:
      'Task and board changes stream over server-sent events, so every open workspace reflects the latest state.',
  },
  {
    icon: ClipboardList,
    title: 'History you can debug',
    description:
      'See who changed what and when across tasks, boards, members, workspaces, and authentication events.',
  },
  {
    icon: MessageSquare,
    title: 'Context beside the work',
    description:
      'Keep editable comments, mentions, decisions, and implementation notes attached to the task instead of losing them in another chat thread.',
  },
  {
    icon: FileText,
    title: 'Useful issue primitives',
    description:
      'Start and due dates, files, tags, estimates, multiple assignees, parent-child relationships, and blocking dependencies are built in.',
  },
  {
    icon: Workflow,
    title: 'Automate the repetitive path',
    description:
      'Trigger on field changes, unassigned work, or approaching due dates, then chain notifications, comments, updates, flags, or safe delayed deletion.',
  },
  {
    icon: ChartNoAxesCombined,
    title: 'Reporting grounded in history',
    description:
      'Track throughput, cycle time, sprint burndown, velocity, and current workload—computed from an append-only event history in task counts, story points, or hours, with the coverage date shown rather than reconstructed.',
  },
  {
    icon: Braces,
    title: 'An API built for integrations',
    description:
      'Connect internal tools through scoped service accounts, revocable credentials, and a documented public API.',
  },
  {
    icon: LayoutTemplate,
    title: 'Useful from the first workspace',
    description:
      'Start from software delivery, content, basic, or minimal templates with statuses, tags, relations, and boards ready to use.',
  },
];
