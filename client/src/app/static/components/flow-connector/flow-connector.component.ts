import { Component, computed, input } from '@angular/core';
import { cn } from '@static/components/button/button.variants';
import { FlowStepSize } from '@static/components/flow-step/flow-step.component';

@Component({
  selector: 'app-flow-connector',
  host: { 'aria-hidden': 'true', '[class]': 'connectorClass()' },
  template: '',
})
export class FlowConnectorComponent {
  readonly size = input<FlowStepSize>('default');

  protected readonly connectorClass = computed(() => {
    const compact = this.size() === 'compact';

    return cn('bg-border block w-px', compact ? 'ml-5.25 h-2.5' : 'ml-6 h-3');
  });
}
