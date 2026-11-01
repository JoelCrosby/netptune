import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/task-review.md?raw';

export default function AssistantTaskReviewPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/assistant/writing', label: 'Writing in tasks and pages' }}
      next={{ href: '/docs/assistant/access-and-cost', label: 'Access, privacy and cost' }}
    />
  );
}
