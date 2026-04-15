import type { Role } from '~/types/role';

export const roles: Role[] = [
  {
    name: 'Owner',
    description: 'Full control over the workspace, members, and settings',
    color:
      'text-violet-600 bg-violet-50 border-violet-200 dark:text-violet-300 dark:bg-violet-500/10 dark:border-violet-500/20',
    dot: 'bg-violet-500',
    permissions: [
      'Manage workspace',
      'Billing & settings',
      'Add / remove members',
      'All admin permissions',
    ],
  },
  {
    name: 'Admin',
    description: 'Manage projects, boards, and team membership',
    color:
      'text-brand bg-violet-50 border-violet-200 dark:text-brand dark:bg-violet-500/10 dark:border-violet-500/20',
    dot: 'bg-brand',
    permissions: [
      'Create / archive projects',
      'Manage boards',
      'Invite members',
      'All member permissions',
    ],
  },
  {
    name: 'Member',
    description: 'Create and update tasks, collaborate on boards',
    color:
      'text-sky-600 bg-sky-50 border-sky-200 dark:text-sky-400 dark:bg-sky-500/10 dark:border-sky-500/20',
    dot: 'bg-sky-500',
    permissions: [
      'Create & edit tasks',
      'Post comments',
      'Add attachments',
      'All viewer permissions',
    ],
  },
  {
    name: 'Viewer',
    description: 'Read-only access for stakeholders and clients',
    color:
      'text-slate-600 bg-slate-50 border-slate-200 dark:text-white/60 dark:bg-white/5 dark:border-white/10',
    dot: 'bg-slate-400',
    permissions: ['View boards & tasks', 'View activity logs', 'View comments', 'No edit access'],
  },
];
