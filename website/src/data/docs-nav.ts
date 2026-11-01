export type DocLink = { href: string; label: string };

export type DocNavSection = { heading: string; items: DocLink[] };

export const docsNav: DocNavSection[] = [
  {
    heading: 'Getting started',
    items: [{ href: '/docs', label: 'Overview' }],
  },
  {
    heading: 'AI Assistant',
    items: [
      { href: '/docs/assistant', label: 'Overview' },
      { href: '/docs/assistant/asking', label: 'Asking questions' },
      { href: '/docs/assistant/proposals', label: 'Proposals and review' },
      { href: '/docs/assistant/writing', label: 'Writing in tasks and pages' },
      { href: '/docs/assistant/task-review', label: 'Task review' },
      { href: '/docs/assistant/access-and-cost', label: 'Access, privacy and cost' },
    ],
  },
  {
    heading: 'Features',
    items: [
      { href: '/docs/pages', label: 'Pages' },
      { href: '/docs/source-control', label: 'Source control' },
      { href: '/docs/queries', label: 'Queries' },
      { href: '/docs/import-export', label: 'Import & export' },
      { href: '/docs/automations', label: 'Automations' },
    ],
  },
  {
    heading: 'Reference',
    items: [{ href: '/docs/api', label: 'API' }],
  },
];

const pagerLinks = docsNav.flatMap((section) => {
  return section.items.map((item) => {
    return {
      href: item.href,
      label: item.label === 'Overview' ? section.heading : item.label,
      section: section.heading,
    };
  });
});

export function findDocSection(pathname: string): string | undefined {
  return pagerLinks.find((item) => item.href === pathname)?.section;
}

export function findDocNeighbours(pathname: string): { prev?: DocLink; next?: DocLink } {
  const index = pagerLinks.findIndex((item) => item.href === pathname);

  if (index === -1) {
    return {};
  }

  return {
    prev: index === 0 ? { href: '/', label: 'Home' } : pagerLinks[index - 1],
    next: pagerLinks[index + 1],
  };
}
