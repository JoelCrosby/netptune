import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/kubernetes.md?raw';

export default function KubernetesPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/docker-compose', label: 'Docker Compose' }}
      next={{ href: '/docs/configuration', label: 'Configuration' }}
    />
  );
}
