import {
  Kanban,
  Zap,
  History,
  ShieldCheck,
  CalendarDays,
  MessageSquare,
  LogIn,
  Server,
  Timer,
  Workflow,
  Search,
  FileSpreadsheet,
  ChartNoAxesCombined,
  Braces,
  LayoutTemplate,
  Paperclip,
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
    description: 'Tracked workspace changes with actor and timestamp',
  },
  {
    icon: ShieldCheck,
    title: 'Role-based access',
    description: 'Owner, Admin, Member, and Viewer roles per workspace',
  },
  {
    icon: CalendarDays,
    title: 'Task due dates',
    description: 'Keep deadlines visible alongside the work',
  },
  {
    icon: MessageSquare,
    title: 'Task comments',
    description: 'Keep discussion and context attached to each task',
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
    icon: ChartNoAxesCombined,
    title: 'Delivery reporting',
    description: 'Flow, workload, burndown, and velocity in one workspace',
  },
  {
    icon: Braces,
    title: 'Public API',
    description: 'Integrate projects, statuses, sprints, and tasks',
  },
  {
    icon: LayoutTemplate,
    title: 'Workspace templates',
    description: 'Launch with a complete workflow instead of a blank slate',
  },
  {
    icon: Paperclip,
    title: 'Task files & workspace storage',
    description: 'Attach files to work and manage storage from one place',
  },
  {
    icon: LogIn,
    title: 'OAuth sign-in',
    description: 'Sign in with GitHub, Google, or Microsoft',
  },
  {
    icon: Server,
    title: 'Open source & self-hostable',
    description: 'MIT licensed with a maintained Kubernetes Helm chart',
  },
];
