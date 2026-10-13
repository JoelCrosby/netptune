import { LucideSparkles } from '@lucide/angular';
import {
  InlinePromptConfig,
  InlinePromptLabels,
  InlinePromptRunner,
} from '@static/components/editor/editor-inline-prompt';

const labels: InlinePromptLabels = {
  placeholder: $localize`:Placeholder after /ai in an editor, before anything is typed:Ask the assistant to write…`,
  send: $localize`:Action that sends the inline assistant prompt:Send`,
  cancel: $localize`:Action that cancels the inline assistant prompt:Cancel`,
  thinking: $localize`:Shown while the assistant is preparing its reply:Thinking…`,
  retry: $localize`:Action that retries a failed inline assistant prompt:Retry`,
  dismiss: $localize`:Action that dismisses a failed inline assistant prompt:Dismiss`,
  failed: $localize`:Shown in an editor when the assistant could not write a reply:The assistant could not write this.`,
};

const command = {
  label: $localize`:Slash command that opens a prompt for the assistant to write at the cursor:Ask the assistant`,
  group: $localize`:Heading over the assistant actions in the slash command menu:Assistant`,
  icon: LucideSparkles,
  keywords: ['ai', 'assistant', 'write', 'generate', 'draft'],
};

export function assistantInlinePrompt(
  run: InlinePromptRunner
): InlinePromptConfig {
  // Inline writing runs on the provider's fast model, which streams a few times quicker than the
  // assistant panel's, so the reply is held to roughly the panel's typing pace.
  return { trigger: '/ai', run, labels, command, maxRevealPerSecond: 100 };
}
