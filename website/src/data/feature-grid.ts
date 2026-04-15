import {
  Kanban,
  Zap,
  History,
  ShieldCheck,
  Paperclip,
  MessageSquare,
  GitBranch,
  Star,
  Server,
} from 'lucide-solid';
import type { Feature } from '~/types/feature';

export const featureGridItems: Feature[] = [
  {
    icon: Kanban,
    title: 'Kanban boards',
    description: 'Visualize work with drag-and-drop columns',
  },
  {
    icon: Zap,
    title: 'Real-time updates',
    description: 'Every team member sees changes instantly via SSE',
  },
  {
    icon: History,
    title: 'Activity logs',
    description: 'Full audit trail on every task and board',
  },
  {
    icon: ShieldCheck,
    title: 'Role-based access',
    description: 'Owner, Admin, Member, and Viewer roles per workspace',
  },
  {
    icon: Paperclip,
    title: 'File attachments',
    description: 'S3-compatible storage — bring your own bucket',
  },
  {
    icon: MessageSquare,
    title: 'Comments & reactions',
    description: 'Discussion threads on every task',
  },
  {
    icon: GitBranch,
    title: 'GitHub OAuth',
    description: 'Sign in with GitHub or email',
  },
  {
    icon: Star,
    title: 'Open source',
    description: 'MIT licensed, fully transparent',
  },
  {
    icon: Server,
    title: 'Self-hostable',
    description: 'Deploy anywhere — Docker, Kubernetes, bare metal',
  },
];
