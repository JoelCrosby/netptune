import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/external-services.md?raw';

export default function ExternalServicesPage() {
  return (
    <MarkdownDoc source={source} prev={{ href: '/docs/configuration', label: 'Configuration' }} />
  );
}
