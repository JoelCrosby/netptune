import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/index.md?raw';

export default function AssistantPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs', label: 'Getting started' }}
      next={{ href: '/docs/assistant/asking', label: 'Asking questions' }}
    />
  );
}
