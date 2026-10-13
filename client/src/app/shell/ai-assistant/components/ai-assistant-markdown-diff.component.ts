import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import {
  AiMarkdownBlock,
  parseAssistantMarkdown,
} from '@core/util/ai-markdown';
import {
  BlockDiffComponent,
  BlockDiffItem,
} from '@static/components/block-diff/block-diff.component';
import { AiDiffMode } from './ai-assistant-diff';
import { markdownDiff } from './ai-assistant-field-diff';
import { AiAssistantMarkdownComponent } from './ai-assistant-markdown.component';

@Component({
  selector: 'app-ai-assistant-markdown-diff',
  host: { class: 'block' },
  imports: [AiAssistantMarkdownComponent, BlockDiffComponent],
  template: `
    <app-block-diff [items]="items()" [mode]="mode()">
      <ng-template let-blocks>
        <app-ai-assistant-markdown [blocks]="blocks" />
      </ng-template>
    </app-block-diff>
  `,
})
export class AiAssistantMarkdownDiffComponent {
  readonly field = input.required<AiChangeField>();
  readonly mode = input<AiDiffMode>('split');

  protected readonly items = computed<BlockDiffItem<AiMarkdownBlock[]>[]>(
    () => {
      const field = this.field();

      return markdownDiff(field.before, field.after).map((op) => {
        return { kind: op.kind, value: parseAssistantMarkdown(op.value) };
      });
    }
  );
}
