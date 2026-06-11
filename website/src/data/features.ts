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
      'When your teammate moves a task, you see it move. When someone posts a comment, it appears instantly — no polling, no refreshing, no guesswork.',
  },
  {
    icon: ClipboardList,
    title: 'Full audit trails, always',
    description:
      'Every change is logged automatically — who made it, what changed, and when. More than 20 tracked activity types give you complete visibility into how your project evolved.',
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
      'A powerful editor, file attachments via any S3-compatible store, tags, due dates, priorities, and estimates — without the bloat of tools that try to do everything.',
  },
  {
    icon: Workflow,
    title: 'Automations that do the busywork',
    description:
      'Trigger rules when a task changes status, sits unassigned too long, or has fields updated — then notify assignees, flag the task, or update it automatically.',
  },
];
