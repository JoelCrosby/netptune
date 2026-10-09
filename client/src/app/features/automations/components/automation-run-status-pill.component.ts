import { Component, computed, input } from '@angular/core';
import { automationRunStatusLabels } from '../models/automation-copy';
import { AutomationRunStatus } from '../models/automation.models';
import {
  AutomationStatePillComponent,
  AutomationStateTone,
} from './automation-state-pill.component';

@Component({
  selector: 'app-automation-run-status-pill',
  imports: [AutomationStatePillComponent],
  template: `
    <app-automation-state-pill [tone]="tone()">
      {{ label() }}
    </app-automation-state-pill>
  `,
})
export class AutomationRunStatusPillComponent {
  readonly status = input.required<AutomationRunStatus>();

  protected readonly label = computed(() => {
    return automationRunStatusLabels[this.status()];
  });

  protected readonly tone = computed<AutomationStateTone>(() => {
    switch (this.status()) {
      case AutomationRunStatus.succeeded:
        return 'success';
      case AutomationRunStatus.failed:
        return 'warn';
      case AutomationRunStatus.skipped:
        return 'neutral';
    }
  });
}
