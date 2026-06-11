import {
  Kanban,
  Zap,
  History,
  ShieldCheck,
  Paperclip,
  MessageSquare,
  LogIn,
  Server,
  Timer,
  Workflow,
  Search,
  FileSpreadsheet,
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
    icon: Timer,
    title: 'Sprints',
    description: 'Time-boxed sprints with task counts and status tracking',
  },
  {
    icon: Workflow,
    title: 'Automations',
    description: 'Rules that notify, flag, or update tasks automatically',
  },
  {
    icon: Search,
    title: 'Instant search',
    description: 'Find tasks across the workspace from the command palette',
  },
  {
    icon: FileSpreadsheet,
    title: 'CSV import & export',
    description: 'Move tasks in or out of any board — no lock-in',
  },
  {
    icon: LogIn,
    title: 'OAuth sign-in',
    description: 'Sign in with GitHub, Google, or Microsoft',
  },
  {
    icon: Server,
    title: 'Open source & self-hostable',
    description: 'MIT licensed — deploy on Docker, Kubernetes, or bare metal',
  },
];
