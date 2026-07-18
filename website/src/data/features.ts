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
    title: 'Boards without ceremony',
    description:
      'Model the workflow your team already uses with custom columns, drag-and-drop ordering, and multiple boards per project.',
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
      'Keep decisions, reactions, and implementation notes attached to the task instead of losing them in another chat thread.',
  },
  {
    icon: FileText,
    title: 'Useful issue primitives',
    description:
      'Descriptions, file attachments, tags, priorities, estimates, due dates, relations, and multiple assignees are built in.',
  },
  {
    icon: Workflow,
    title: 'Automate the repetitive path',
    description:
      'React to workflow events with rules that notify assignees, flag tasks, or update fields—and inspect every run afterward.',
  },
  {
    icon: ChartNoAxesCombined,
    title: 'Reporting grounded in history',
    description:
      'Inspect flow, workload, sprint burndown, and velocity using task counts, story points, or hours.',
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
