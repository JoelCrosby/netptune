import { Component, inject } from '@angular/core';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiPanelService } from '@core/services/ai-panel.service';
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
  private readonly panel = inject(AiPanelService);

  constructor() {
    this.panel.close();

    void this.assistant.ensureLoaded();
  }
}
