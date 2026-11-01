import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/automations.md?raw';

export default function AutomationsPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/import-export', label: 'Import & export' }}
      next={{ href: '/docs/api', label: 'API' }}
    />
  );
}
