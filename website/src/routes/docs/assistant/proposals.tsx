import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/proposals.md?raw';

export default function AssistantProposalsPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant/asking', label: 'Asking questions' }}
      next={{ href: '/docs/assistant/writing', label: 'Writing in tasks and pages' }}
    />
  );
}
