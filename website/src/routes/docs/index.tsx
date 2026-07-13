import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/index.md?raw';

export default function DocsOverview() {
  return (
    <MarkdownDoc source={source} next={{ href: '/docs/docker-compose', label: 'Docker Compose' }} />
  );
}
