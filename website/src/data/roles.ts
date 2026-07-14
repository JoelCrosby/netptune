import type { Role } from '~/types/role';

export const roles: Role[] = [
  {
    name: 'Owner',
    description: 'Full control over the workspace, members, and settings',
    color: 'text-violet-600 dark:text-violet-300',
    dot: 'bg-violet-500',
    permissions: [
      'Manage workspace',
      'Permanently delete workspace',
      'Add / remove members',
      'All admin permissions',
    ],
  },
  {
    name: 'Admin',
    description: 'Manage projects, boards, and team membership',
    color: 'text-brand dark:text-violet-400',
    dot: 'bg-brand',
    permissions: [
      'Create / delete projects & boards',
      'Manage automations',
      'Invite / remove members',
      'All member permissions',
    ],
  },
  {
    name: 'Member',
    description: 'Create and update tasks, collaborate on boards',
    color: 'text-fuchsia-600 dark:text-fuchsia-300',
    dot: 'bg-fuchsia-500',
    permissions: [
      'Create & edit tasks',
      'Post comments',
      'Set task due dates',
      'All viewer permissions',
    ],
  },
  {
    name: 'Viewer',
    description: 'Read-only access for stakeholders and clients',
    color: 'text-slate-600 dark:text-slate-300',
    dot: 'bg-slate-400',
    permissions: ['View boards & tasks', 'View activity logs', 'View comments', 'No edit access'],
  },
];
