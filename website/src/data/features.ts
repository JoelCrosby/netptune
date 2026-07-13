import { Kanban, Zap, ClipboardList, MessageSquare, FileText, Workflow } from 'lucide-solid';
import type { Feature } from '~/types/feature';

export const features: Feature[] = [
  {
    icon: Kanban,
    title: 'Kanban boards built for speed',
    description:
      'Custom columns, drag-and-drop reordering, multiple boards per project. Structure your workflow exactly how your team thinks — no configuration overhead.',
  },
  {
    icon: Zap,
    title: 'Real-time, out of the box',
    description:
      'Board and task changes are delivered over server-sent events, keeping teammates in sync without repeatedly refreshing the page.',
  },
  {
    icon: ClipboardList,
    title: 'Detailed activity and audit history',
    description:
      'Tracked changes record who acted, what changed, and when. More than 20 activity types cover task, board, workspace, member, and authentication events.',
  },
  {
    icon: MessageSquare,
    title: 'Comments on every task',
    description:
      'Discussion lives on tasks, not in Slack threads that get buried. React with emoji, ask questions, share context — all attached to the work it relates to.',
  },
  {
    icon: FileText,
    title: 'Rich task details',
    description:
      'A rich editor, AWS S3-backed attachments, tags, priorities, estimates, relations, and comments keep context attached to the work.',
  },
  {
    icon: Workflow,
    title: 'Automations that do the busywork',
    description:
      'Trigger rules when a task changes status, sits unassigned too long, or has fields updated — then notify assignees, flag the task, or update it automatically.',
  },
];
