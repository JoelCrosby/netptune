import type { Comparison } from '~/types/comparison';

export const comparisons: Comparison[] = [
  {
    competitor: 'vs. Jira',
    headline: 'Less configuration overhead.',
    description:
      'No XML workflows, no permission schemes spanning three nested menus. Boards that work the way your team works — set up in minutes, not days.',
  },
  {
    competitor: 'vs. Trello',
    headline: 'More than just cards.',
    description:
      'Full audit logs, workspace permissions, real-time sync, and role-based access. Everything Trello leaves out when your team grows past five people.',
  },
  {
    competitor: 'vs. Linear',
    headline: 'Open source and self-hostable.',
    description:
      'Keep your data exactly where you want it. MIT licensed, no vendor dependency, and deploy on any infrastructure — not just theirs.',
  },
  {
    competitor: 'vs. Asana',
    headline: 'No per-seat pricing when self-hosted.',
    description:
      'Own your deployment completely. No invoices that scale with headcount. Run it on your own servers and pay nothing for additional seats.',
  },
];
