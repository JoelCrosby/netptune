import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { LucideTrash2 } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { AutomationRule } from '../models/automation.models';
import { AutomationEnabledBadgeComponent } from './automation-enabled-badge.component';

@Component({
  selector: 'app-automation-detail-heading',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    IconButtonComponent,
    PrettyDatePipe,
    AutomationEnabledBadgeComponent,
    LucideTrash2,
  ],
  template: `
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div class="min-w-0">
        <div class="flex flex-wrap items-center gap-2">
          <h1 class="text-2xl font-semibold">{{ rule().name }}</h1>
          <app-automation-enabled-badge [enabled]="rule().isEnabled" />
        </div>
        <p class="text-muted mt-1 text-sm">
          Created {{ rule().createdAt | prettyDate }}
          @if (rule().updatedAt) {
            · Updated {{ rule().updatedAt | prettyDate }}
          }
        </p>
      </div>

      @if (canManage()) {
        <button
          app-icon-button
          type="button"
          title="Delete rule"
          [disabled]="saving()"
          (click)="deleteRule.emit(rule())">
          <svg lucideTrash2 class="h-4 w-4"></svg>
        </button>
      }
    </div>
  `,
})
export class AutomationDetailHeadingComponent {
  readonly rule = input.required<AutomationRule>();
  readonly canManage = input.required<boolean>();
  readonly saving = input(false);

  readonly deleteRule = output<AutomationRule>();
}
