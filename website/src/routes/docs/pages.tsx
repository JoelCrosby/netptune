import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/pages.md?raw';

export default function PagesDocPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant/access-and-cost', label: 'Access, privacy and cost' }}
      next={{ href: '/docs/source-control', label: 'Source control' }}
    />
  );
}
