import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/docker-compose.md?raw';

export default function DockerComposePage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs', label: 'Overview' }}
      next={{ href: '/docs/kubernetes', label: 'Kubernetes / Helm' }}
    />
  );
}
