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
  BellRing,
  CalendarRange,
  ChartGantt,
  GitBranch,
  Keyboard,
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
    description: 'Assign and change Owner, Admin, Member, and Viewer roles',
  },
  {
    icon: CalendarDays,
    title: 'Task due dates',
    description: 'Keep deadlines visible alongside the work',
  },
  {
    icon: MessageSquare,
    title: 'Task comments',
    description: 'Editable discussion and mentions attached to each task',
  },
  {
    icon: Timer,
    title: 'Sprints',
    description: 'Time-boxed sprints with task counts and status tracking',
  },
  {
    icon: Workflow,
    title: 'Automations',
    description: 'Due-date and field rules with chainable actions and run history',
  },
  {
    icon: Search,
    title: 'Instant search',
    description: 'Scope command-palette results with task and project prefixes',
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
    icon: CalendarRange,
    title: 'Calendar planning',
    description: 'Schedule, filter, and reschedule work on a monthly calendar',
  },
  {
    icon: ChartGantt,
    title: 'Roadmap timeline',
    description: 'Edit dates, plan unscheduled work, and see sprint context',
  },
  {
    icon: GitBranch,
    title: 'Task relationships',
    description: 'Model parent-child work and cycle-safe blocking dependencies',
  },
  {
    icon: BellRing,
    title: 'Notification controls',
    description: 'Choose event types globally or for an individual workspace',
  },
  {
    icon: Keyboard,
    title: 'Keyboard navigation',
    description: 'Jump to projects, tasks, boards, search, and automations',
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
