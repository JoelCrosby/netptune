import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/asking.md?raw';

export default function AssistantAskingPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant', label: 'AI Assistant' }}
      next={{ href: '/docs/assistant/proposals', label: 'Proposals and review' }}
    />
  );
}
