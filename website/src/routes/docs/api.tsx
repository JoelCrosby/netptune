import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/api.md?raw';

export default function ApiPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/external-services', label: 'External Services' }}
    />
  );
}
