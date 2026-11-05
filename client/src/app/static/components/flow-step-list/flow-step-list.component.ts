import { Component, input, output } from '@angular/core';
import { LucideCheck, type LucideIconInput } from '@lucide/angular';
import { FlowConnectorComponent } from '@static/components/flow-connector/flow-connector.component';
import { FlowStepComponent } from '@static/components/flow-step/flow-step.component';

export interface FlowStepListItem {
  key: string;
  keyword: string;
  icon: LucideIconInput;
  title: string;
  summary: string;
  aside?: string;
  done?: boolean;
  locked?: boolean;
  startsGroup?: boolean;
}

@Component({
  selector: 'app-flow-step-list',
  imports: [FlowConnectorComponent, FlowStepComponent, LucideCheck],
  host: { class: 'flex flex-col' },
  template: `
    @for (item of items(); track item.key; let first = $first) {
      @if (item.startsGroup) {
        <div class="h-5" aria-hidden="true"></div>
      } @else if (!first) {
        <app-flow-connector />
      }

      <app-flow-step
        [keyword]="item.keyword"
        [icon]="item.icon"
        [title]="item.title"
        [selected]="item.key === selected()"
        [interactive]="!item.locked"
        (selectStep)="selectStep.emit(item.key)">
        @if (item.done) {
          <svg
            flowStepAside
            lucideCheck
            class="text-primary h-3.5 w-3.5"
            aria-hidden="true"></svg>
        } @else if (item.aside) {
          <span flowStepAside class="text-foreground/45 text-xs">
            {{ item.aside }}
          </span>
        }
        {{ item.summary }}
      </app-flow-step>
    }
  `,
})
export class FlowStepListComponent {
  readonly items = input.required<FlowStepListItem[]>();
  readonly selected = input.required<string>();

  readonly selectStep = output<string>();
}
