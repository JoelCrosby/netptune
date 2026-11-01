import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/writing.md?raw';

export default function AssistantWritingPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant/proposals', label: 'Proposals and review' }}
      next={{ href: '/docs/assistant/task-review', label: 'Task review' }}
    />
  );
}
