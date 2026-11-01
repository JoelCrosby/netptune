import {
  Archive,
  BellRing,
  Braces,
  CalendarRange,
  ChartGantt,
  ClipboardCheck,
  Copy,
  FileStack,
  GitBranch,
  GitPullRequest,
  Globe,
  History,
  Kanban,
  Keyboard,
  Languages,
  LayoutTemplate,
  ListFilter,
  LogIn,
  Paperclip,
  PackageOpen,
  Search,
  ShieldCheck,
  Smartphone,
  Timer,
} from 'lucide-solid';
import type { Feature } from '~/types/feature';

export const featureGridItems: Feature[] = [
  {
    icon: Kanban,
    title: 'Kanban boards',
    description: 'Drag work across columns and watch it update live',
  },
  {
    icon: Copy,
    title: 'Tasks on several boards',
    description: 'One task, wherever it needs to be seen',
  },
  {
    icon: Timer,
    title: 'Sprints',
    description: 'Plan, start and wrap up work in time-boxes',
  },
  {
    icon: CalendarRange,
    title: 'Calendar',
    description: 'Every due date on one monthly view',
  },
  {
    icon: ChartGantt,
    title: 'Roadmap',
    description: 'The bigger picture on a timeline you can edit',
  },
  {
    icon: FileStack,
    title: 'Pages',
    description: 'Docs, notes and runbooks next to the work',
  },
  {
    icon: LayoutTemplate,
    title: 'Templates',
    description: 'Start workspaces and pages from something good',
  },
  {
    icon: ClipboardCheck,
    title: 'Task review',
    description: 'Let the assistant spot the gaps before work starts',
  },
  {
    icon: GitBranch,
    title: 'GitHub integration',
    description: 'Branches and commits find their own way to tasks',
  },
  {
    icon: GitPullRequest,
    title: 'Pull requests on tasks',
    description: 'See merged, open and failing checks at a glance',
  },
  {
    icon: ListFilter,
    title: 'Saved queries',
    description: 'Build a view once and share it with the team',
  },
  {
    icon: Search,
    title: 'Search everything',
    description: 'Tasks, pages and projects in one search, archive included',
  },
  {
    icon: History,
    title: 'Activity history',
    description: 'Who changed what, and when',
  },
  {
    icon: ShieldCheck,
    title: 'Roles and permissions',
    description: 'Owner, admin, member and viewer, fine-tuned per person',
  },
  {
    icon: Globe,
    title: 'Public workspaces',
    description: 'Share a read-only workspace with anyone',
  },
  {
    icon: Languages,
    title: 'Four languages',
    description: 'English, French, German and Spanish',
  },
  {
    icon: Smartphone,
    title: 'Works on phones',
    description: 'Check in and move work from anywhere',
  },
  {
    icon: Keyboard,
    title: 'Keyboard shortcuts',
    description: 'Jump anywhere without touching the mouse',
  },
  {
    icon: BellRing,
    title: 'Notifications your way',
    description: 'Choose what you hear about, workspace by workspace',
  },
  {
    icon: Paperclip,
    title: 'File attachments',
    description: 'Keep the screenshots and specs on the task',
  },
  {
    icon: PackageOpen,
    title: 'Bring your work with you',
    description: 'Import from spreadsheets, Jira, Trello or Asana',
  },
  {
    icon: Archive,
    title: 'Export any time',
    description: 'Take a full copy of your workspace whenever you like',
  },
  {
    icon: Braces,
    title: 'API',
    description: 'Connect your other tools with their own keys',
  },
  {
    icon: LogIn,
    title: 'Sign in your way',
    description: 'GitHub, Google, Microsoft or email',
  },
];
