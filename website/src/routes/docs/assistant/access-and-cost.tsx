import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/access-and-cost.md?raw';

export default function AssistantAccessPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant/task-review', label: 'Task review' }}
      next={{ href: '/docs/pages', label: 'Pages' }}
    />
  );
}
