import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiProposedChange } from '@core/models/ai-conversation';
import { referenceRoute } from '@core/util/ai-references';
import {
  LucideCalendarRange,
  LucideCircleAlert,
  LucideCircleCheck,
  LucideCircleDashed,
  LucideFolder,
  LucideKanban,
  LucideSquareCheckBig,
} from '@lucide/angular';
import {
  AiChangeGroup,
  fieldLabel,
  isApplied,
  isValid,
} from './ai-assistant-change-group';

@Component({
  selector: 'app-ai-assistant-change-list',
  host: { class: 'flex flex-col gap-3' },
  imports: [
    RouterLink,
    LucideCalendarRange,
    LucideCircleAlert,
    LucideCircleCheck,
    LucideCircleDashed,
    LucideFolder,
    LucideKanban,
    LucideSquareCheckBig,
  ],
  template: `
    @for (group of groups(); track group.key) {
      <div class="border-border rounded-lg border">
        <div
          class="text-muted border-border flex items-center gap-1.5 border-b px-2 py-1 text-xs">
          @switch (group.entityType) {
            @case ('task') {
              <svg lucideSquareCheckBig class="h-3 w-3"></svg>
            }
            @case ('project') {
              <svg lucideFolder class="h-3 w-3"></svg>
            }
            @case ('sprint') {
              <svg lucideCalendarRange class="h-3 w-3"></svg>
            }
            @case ('board') {
              <svg lucideKanban class="h-3 w-3"></svg>
            }
            @default {
              <svg lucideCircleDashed class="h-3 w-3"></svg>
            }
          }
          <span>{{ group.label }}</span>
        </div>

        <div class="flex flex-col gap-2 p-2">
          @for (change of group.changes; track change.id) {
            <div class="flex items-start gap-2 text-sm">
              @if (isPending()) {
                <input
                  type="checkbox"
                  class="mt-1"
                  [checked]="isIncluded(change)"
                  [disabled]="!canSelect(change)"
                  (change)="toggled.emit(change.id)" />
              } @else {
                <span class="mt-0.5 flex h-4 w-4 items-center justify-center">
                  @if (wasApplied(change)) {
                    <svg lucideCircleCheck class="text-primary h-3.5 w-3.5"></svg>
                  } @else {
                    <svg lucideCircleAlert class="text-muted h-3.5 w-3.5"></svg>
                  }
                </span>
              }

              <span class="flex min-w-0 flex-col gap-0.5">
                @if (routeFor(change); as route) {
                  <a [routerLink]="route" class="hover:underline">{{
                    change.summary
                  }}</a>
                } @else {
                  <span>{{ change.summary }}</span>
                }

                @for (field of change.fields; track field.name) {
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

                @if (!canSelect(change)) {
                  <span class="text-muted text-xs italic">
                    {{ validationMessage(change) }}
                  </span>
                }

                @if (change.applyError) {
                  <span class="text-error text-xs">{{ change.applyError }}</span>
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
    const workspace = this.workspace();
    const canLink = workspace !== null && isApplied(change);

    if (!canLink) {
      return null;
    }

    const identifier =
      change.entitySystemId ?? change.appliedEntityId ?? change.entityId;

    if (!identifier) {
      return null;
    }

    return referenceRoute(workspace, change.entityType, `${identifier}`);
  }
}
