import type { FooterColumn, FooterLink } from '~/types/footer';

export const footerColumns: FooterColumn[] = [
  {
    heading: 'Product',
    links: [
      { label: 'Features', href: '/#features' },
      { label: 'AI assistant', href: '/assistant' },
      { label: 'Create an account', href: 'https://app.netptune.co.uk/auth/register' },
      { label: 'Sign in', href: 'https://app.netptune.co.uk/auth/login' },
    ],
  },
  {
    heading: 'Assistant',
    links: [
      { label: 'Overview', href: '/docs/assistant' },
      { label: 'Asking questions', href: '/docs/assistant/asking' },
      { label: 'Proposals and review', href: '/docs/assistant/proposals' },
      { label: 'Writing in place', href: '/docs/assistant/writing' },
      { label: 'Task review', href: '/docs/assistant/task-review' },
      { label: 'Access, privacy and cost', href: '/docs/assistant/access-and-cost' },
    ],
  },
  {
    heading: 'Docs',
    links: [
      { label: 'Getting started', href: '/docs' },
      { label: 'Pages', href: '/docs/pages' },
      { label: 'Source control', href: '/docs/source-control' },
      { label: 'Queries', href: '/docs/queries' },
      { label: 'Import & export', href: '/docs/import-export' },
      { label: 'Automations', href: '/docs/automations' },
      { label: 'API', href: '/docs/api' },
    ],
  },
];

export const legalLinks: FooterLink[] = [
  { label: 'Terms', href: '/terms' },
  { label: 'Privacy', href: '/privacy' },
];
