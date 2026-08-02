import { Component, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiProposedChange } from '@core/models/ai-conversation';
import { SelectionCheckboxComponent } from '@static/components/checkbox/selection-checkbox.component';
import {
  LucideChevronDown,
  LucideCircleAlert,
  LucideCircleCheck,
} from '@lucide/angular';
import {
  AiChangeGroup,
  changeRoute,
  fieldLabel,
  isApplied,
  isProseField,
  isValid,
} from './ai-assistant-change-group';
import { AiAssistantEntityIconComponent } from './ai-assistant-entity-icon.component';

@Component({
  selector: 'app-ai-assistant-change-list',
  host: { class: 'flex flex-col gap-2' },
  imports: [
    RouterLink,
    SelectionCheckboxComponent,
    LucideChevronDown,
    LucideCircleAlert,
    LucideCircleCheck,
    AiAssistantEntityIconComponent,
  ],
  template: `
    @for (group of groups(); track group.key) {
      <div class="bg-hover rounded-2xl px-3 py-2">
        <div class="text-muted mb-1 flex items-center gap-1.5 text-xs">
          <app-ai-assistant-entity-icon [entityType]="group.entityType" />
          <span>{{ group.label }}</span>
        </div>

        <div class="flex flex-col gap-1.5">
          @for (change of group.changes; track change.id) {
            <div class="flex items-start gap-2 text-sm">
              @if (isPending()) {
                <app-selection-checkbox
                  class="mt-0.5"
                  [checked]="isIncluded(change)"
                  [disabled]="!canSelect(change)"
                  [label]="change.summary"
                  (changed)="toggled.emit(change.id)" />
              } @else {
                <span class="mt-0.5 flex h-4 w-4 items-center justify-center">
                  @if (wasApplied(change)) {
                    <svg
                      lucideCircleCheck
                      class="text-primary h-3.5 w-3.5"></svg>
                  } @else {
                    <svg lucideCircleAlert class="text-muted h-3.5 w-3.5"></svg>
                  }
                </span>
              }

              <span class="flex min-w-0 flex-1 flex-col gap-1">
                <span
                  class="flex min-w-0 items-start gap-1"
                  [class.cursor-pointer]="hasDetails(change)"
                  (click)="toggleDetails(change)">
                  @if (routeFor(change); as route) {
                    <a
                      [routerLink]="route"
                      class="min-w-0 flex-1 hover:underline"
                      (click)="stopPropagation($event)"
                      >{{ change.summary }}</a
                    >
                  } @else {
                    <span class="min-w-0 flex-1">{{ change.summary }}</span>
                  }

                  @if (hasDetails(change)) {
                    <button
                      type="button"
                      class="text-muted hover:text-foreground -mr-1 shrink-0 rounded p-1"
                      [attr.aria-expanded]="isExpanded(change)"
                      i18n-aria-label="
                        Accessible label for the toggle that reveals what a
                        proposed change contains
                      "
                      aria-label="Change details"
                      (click)="toggleDetails(change, $event)">
                      <svg
                        lucideChevronDown
                        class="h-3.5 w-3.5 transition-transform"
                        [class.rotate-180]="isExpanded(change)"></svg>
                    </button>
                  }
                </span>

                @if (isExpanded(change)) {
                  @for (field of change.fields; track field.name) {
                    @if (isProse(field)) {
                      <span class="text-muted flex flex-col gap-0.5 text-xs">
                        <span class="font-medium">{{ label(field.name) }}</span>
                        @if (field.before) {
                          <span class="whitespace-pre-wrap line-through">{{
                            field.before
                          }}</span>
                        }
                        @if (field.after) {
                          <span class="whitespace-pre-wrap">{{
                            field.after
                          }}</span>
                        } @else {
                          <span
                            class="italic"
                            i18n="Shown when a change removes a value">
                            cleared
                          </span>
                        }
                      </span>
                    } @else {
                      <span
                        class="text-muted flex flex-wrap items-center gap-1 text-xs">
                        <span class="font-medium">{{ label(field.name) }}</span>
                        @if (field.before) {
                          <span class="line-through">{{ field.before }}</span>
                          <span aria-hidden="true">→</span>
                        }
                        @if (field.after) {
                          <span>{{ field.after }}</span>
                        } @else {
                          <span
                            class="italic"
                            i18n="Shown when a change removes a value">
                            cleared
                          </span>
                        }
                      </span>
                    }
                  }
                }

                @if (!canSelect(change)) {
                  <span class="text-muted text-xs italic">
                    {{ validationMessage(change) }}
                  </span>
                }

                @if (change.applyError) {
                  <span class="text-error text-xs">{{
                    change.applyError
                  }}</span>
                }
              </span>
            </div>
          }
        </div>
      </div>
    }
  `,
})
export class AiAssistantChangeListComponent {
  readonly groups = input.required<AiChangeGroup[]>();
  readonly excludedChangeIds = input.required<Set<number>>();
  readonly isPending = input(false);
  readonly workspace = input<string | null>(null);

  readonly toggled = output<number>();

  protected readonly label = fieldLabel;
  protected readonly isProse = isProseField;

  private readonly expandedChangeIds = signal<Set<number>>(new Set());

  protected isExpanded(change: AiProposedChange): boolean {
    return this.expandedChangeIds().has(change.id);
  }

  protected hasDetails(change: AiProposedChange): boolean {
    return change.fields.length > 0;
  }

  protected stopPropagation(event: Event) {
    event.stopPropagation();
  }

  protected toggleDetails(change: AiProposedChange, event?: Event) {
    event?.stopPropagation();

    if (!this.hasDetails(change)) {
      return;
    }

    this.expandedChangeIds.update((current) => {
      const next = new Set(current);

      if (next.has(change.id)) {
        next.delete(change.id);
      } else {
        next.add(change.id);
      }

      return next;
    });
  }

  protected isIncluded(change: AiProposedChange): boolean {
    return this.canSelect(change) && !this.excludedChangeIds().has(change.id);
  }

  protected canSelect(change: AiProposedChange): boolean {
    return isValid(change);
  }

  protected wasApplied(change: AiProposedChange): boolean {
    return isApplied(change);
  }

  protected validationMessage(change: AiProposedChange): string {
    const message = change.validationMessage;

    if (message) {
      return message;
    }

    return $localize`:Shown on a proposal that cannot be applied:This change cannot be applied.`;
  }

  protected routeFor(change: AiProposedChange): string[] | null {
    return changeRoute(change, this.workspace());
  }
}
