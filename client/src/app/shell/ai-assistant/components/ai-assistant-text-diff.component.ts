import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { TextDiffComponent } from '@static/components/text-diff/text-diff.component';
import { isProseField } from './ai-assistant-change-group';
import { AiDiffMode } from './ai-assistant-diff';

@Component({
  selector: 'app-ai-assistant-text-diff',
  host: { class: 'block' },
  imports: [TextDiffComponent],
  template: `
    <app-text-diff
      [before]="field().before"
      [after]="field().after"
      [mode]="effectiveMode()" />
  `,
})
export class AiAssistantTextDiffComponent {
  readonly field = input.required<AiChangeField>();
  readonly mode = input<AiDiffMode>('split');

  protected readonly effectiveMode = computed<AiDiffMode>(() => {
    return isProseField(this.field()) ? this.mode() : 'inline';
  });
}
