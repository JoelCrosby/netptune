import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/configuration.md?raw';

export default function ConfigurationPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/kubernetes', label: 'Kubernetes / Helm' }}
      next={{ href: '/docs/external-services', label: 'External Services' }}
    />
  );
}
