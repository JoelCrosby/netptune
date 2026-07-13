import type { FooterColumn } from '~/types/footer';

export const footerColumns: FooterColumn[] = [
  {
    heading: 'Product',
    links: [
      { label: 'Features', href: '/#features' },
      { label: 'Self-hosting', href: '/#self-host' },
      { label: 'Hosted application', href: 'https://app.netptune.co.uk/auth/register' },
    ],
  },
  {
    heading: 'Resources',
    links: [
      { label: 'Documentation', href: '/docs' },
      { label: 'Docker Compose status', href: '/docs/docker-compose' },
      { label: 'Kubernetes / Helm', href: '/docs/kubernetes' },
    ],
  },
  {
    heading: 'Community',
    links: [
      { label: 'GitHub', href: 'https://github.com/JoelCrosby/netptune' },
      { label: 'Issues', href: 'https://github.com/JoelCrosby/netptune/issues' },
      { label: 'Pull requests', href: 'https://github.com/JoelCrosby/netptune/pulls' },
    ],
  },
  {
    heading: 'Legal',
    links: [
      {
        label: 'MIT License',
        href: 'https://github.com/JoelCrosby/netptune/blob/main/LICENSE.txt',
      },
    ],
  },
];
