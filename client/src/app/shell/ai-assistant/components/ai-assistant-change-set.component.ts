import { Component, computed, input, output } from '@angular/core';
import { AiChangeSet, AiChangeSetStatus } from '@core/models/ai-conversation';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-ai-assistant-change-set',
  host: { class: 'border-border block border-t' },
  imports: [FlatButtonComponent, StrokedButtonComponent],
  template: `
    <div class="mx-auto w-full px-4 py-3" [class]="contentWidth()">
      <h3
        class="font-overpass mb-2 text-sm font-medium"
        i18n="Heading above the list of proposed workspace changes">
        Proposed changes
      </h3>

      <div class="flex flex-col gap-2">
        @for (change of changeSet().changes; track change.id) {
          <label class="flex items-start gap-2 text-sm">
            <input
              type="checkbox"
              class="mt-1"
              [checked]="isIncluded(change.id)"
              [disabled]="!isPending()"
              (change)="toggled.emit(change.id)" />
            <span class="flex flex-col gap-0.5">
              <span>{{ change.summary }}</span>
              @for (field of change.fields; track field.name) {
                <span class="text-muted text-xs">
                  {{ field.name }}:
                  @if (field.before) {
                    <span class="line-through">{{ field.before }}</span>
                  }
                  <span>{{ field.after }}</span>
                </span>
              }
              @if (change.applyError) {
                <span class="text-error text-xs">{{ change.applyError }}</span>
              }
            </span>
          </label>
        }
      </div>

      @if (isPending()) {
        <div class="mt-3 flex items-center gap-2">
          <button
            app-flat-button
            type="button"
            [disabled]="isApplying()"
            (click)="applied.emit()">
            <span i18n="Button that applies the proposed changes">Apply</span>
          </button>
          <button app-stroked-button type="button" (click)="discarded.emit()">
            <span i18n="Button that discards the proposed changes"
              >Discard</span
            >
          </button>
        </div>
      } @else {
        <p
          class="text-muted mt-3 text-xs"
          i18n="Shown after changes were applied">
          These changes have been applied.
        </p>
      }
    </div>
  `,
})
export class AiAssistantChangeSetComponent {
  readonly changeSet = input.required<AiChangeSet>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly isApplying = input(false);
  readonly contentWidth = input('');

  readonly toggled = output<number>();
  readonly applied = output();
  readonly discarded = output();

  protected readonly isPending = computed(() => {
    return this.changeSet().status === AiChangeSetStatus.pending;
  });

  protected isIncluded(changeId: number): boolean {
    return !this.excludedChangeIds().has(changeId);
  }
}
