import { Component, inject } from '@angular/core';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiAssistantPanelComponent } from '@app/shell/ai-assistant/ai-assistant-panel.component';

@Component({
  selector: 'app-assistant-view',
  host: { class: 'block h-full min-h-0' },
  imports: [AiAssistantPanelComponent],
  template: ` <app-ai-assistant-panel variant="page" /> `,
})
export class AssistantViewComponent {
  private readonly assistant = inject(AiAssistantService);

  constructor() {
    this.assistant.close();
  }
}
