import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/task-review.md?raw';

export default function AssistantTaskReviewPage() {
  return <MarkdownDoc source={source} />;
}
