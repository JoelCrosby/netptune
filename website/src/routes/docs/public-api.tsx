import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/public-api.md?raw';

export default function PublicApiPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/external-services', label: 'External Services' }}
    />
  );
}
