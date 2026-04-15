import type { FooterColumn } from '~/types/footer';

export const footerColumns: FooterColumn[] = [
  {
    heading: 'Product',
    links: [
      { label: 'Features', href: '#features' },
      { label: 'Self-hosting', href: '#self-host' },
      { label: 'Changelog', href: '#' },
      { label: 'Roadmap', href: '#' },
    ],
  },
  {
    heading: 'Resources',
    links: [
      { label: 'Documentation', href: '/docs' },
      { label: 'Docker Compose', href: '/docs/docker-compose' },
      { label: 'Kubernetes / Helm', href: '/docs/kubernetes' },
      {
        label: 'Contributing',
        href: 'https://github.com/JoelCrosby/netptune/blob/main/CONTRIBUTING.md',
      },
    ],
  },
  {
    heading: 'Community',
    links: [
      { label: 'GitHub', href: 'https://github.com/JoelCrosby/netptune' },
      { label: 'Issues', href: 'https://github.com/JoelCrosby/netptune/issues' },
      { label: 'Discussions', href: 'https://github.com/JoelCrosby/netptune/discussions' },
    ],
  },
  {
    heading: 'Legal',
    links: [
      { label: 'MIT License', href: 'https://github.com/JoelCrosby/netptune/blob/main/LICENSE' },
      { label: 'Privacy policy', href: '#' },
    ],
  },
];
