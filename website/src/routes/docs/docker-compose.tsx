import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/docker-compose.md?raw';

export default function DockerComposePage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant', label: 'AI Assistant' }}
      next={{ href: '/docs/kubernetes', label: 'Kubernetes / Helm' }}
    />
  );
}
