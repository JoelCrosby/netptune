import { Component, inject } from '@angular/core';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiAssistantPanelComponent } from '@app/shell/ai-assistant/ai-assistant-panel.component';
import { PageContainerComponent } from '@app/static/components/page-container/page-container.component';

@Component({
  selector: 'app-assistant-view',
  host: { class: 'block h-full min-h-0' },
  imports: [AiAssistantPanelComponent, PageContainerComponent],
  template: ` <app-page-container
    [centerPage]="false"
    [horizontalPadding]="false"
    [verticalPadding]="false">
    <app-ai-assistant-panel variant="page" />
  </app-page-container>`,
})
export class AssistantViewComponent {
  private readonly assistant = inject(AiAssistantService);

  constructor() {
    this.assistant.close();

    void this.assistant.ensureLoaded();
  }
}
