import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { visibleTaskColumnIds } from '@core/tasks/task-columns';
import {
  LucideColumns3,
  LucideListFilter,
  LucidePin,
  LucidePinOff,
  LucideTrash2,
  LucideUsers,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import {
  TaskQueryCatalog,
  TaskQueryGroup,
  TaskView,
} from '../models/task-view.models';
import { QueryFieldOptionsService } from '../services/query-field-options.service';

@Component({
  selector: 'app-task-view-card',
  imports: [
    RouterLink,
    DatePipe,
    AvatarComponent,
    BadgeComponent,
    IconButtonComponent,
    IconTileComponent,
    LucideColumns3,
    LucideListFilter,
    LucidePin,
    LucidePinOff,
    LucideTrash2,
    LucideUsers,
  ],
  host: { class: 'block h-full min-w-0' },
  template: `
    <article
      class="border-border bg-card hover:border-foreground/20 flex h-full min-w-0 flex-col overflow-hidden rounded-lg border shadow-sm transition-colors">
      <header
        class="border-border flex items-start justify-between gap-2 border-b px-5 py-4">
        <div class="flex min-w-0 items-start gap-3">
          <app-icon-tile [icon]="viewIcon" />

          <div class="min-w-0">
            <a
              class="font-overpass text-foreground block truncate text-base font-semibold hover:underline"
              [routerLink]="[view().slug]">
              {{ view().name }}
            </a>

            <div class="mt-1 flex flex-wrap items-center gap-1.5">
              @if (view().isShared) {
                <app-badge color="info" class="gap-1">
                  <svg lucideUsers class="h-3 w-3"></svg>
                  <span i18n="Badge marking a view shared with the workspace">
                    Shared
                  </span>
                </app-badge>
              } @else {
                <app-badge>
                  <span i18n="Badge marking a view only its owner can see">
                    Private
                  </span>
                </app-badge>
              }

              @if (pinned()) {
                <app-badge color="primary" class="gap-1">
                  <svg lucidePin class="h-3 w-3"></svg>
                  <span i18n="Badge marking a view pinned to the sidebar">
                    Pinned
                  </span>
                </app-badge>
              }
            </div>
          </div>
        </div>

        <div class="flex shrink-0 items-center">
          <button
            app-icon-button
            type="button"
            [attr.aria-pressed]="pinned()"
            [attr.aria-label]="pinLabel()"
            [title]="pinLabel()"
            (click)="pinToggled.emit()">
            @if (pinned()) {
              <svg lucidePin class="text-primary h-4 w-4"></svg>
            } @else {
              <svg lucidePinOff class="h-4 w-4"></svg>
            }
          </button>

          @if (canDelete() && view().canEdit) {
            <button
              app-icon-button
              color="warn"
              type="button"
              i18n-aria-label="
                Accessible label for the button that deletes a view
              "
              aria-label="Delete view"
              (click)="deleted.emit()">
              <svg lucideTrash2 class="h-4 w-4"></svg>
            </button>
          }
        </div>
      </header>

      <div class="flex flex-1 flex-col gap-4 px-5 py-4">
        @if (view().description) {
          <p class="text-muted line-clamp-2 text-sm">
            {{ view().description }}
          </p>
        }

        <div class="min-w-0">
          <p
            class="text-muted text-xs font-semibold tracking-wide uppercase"
            i18n="Eyebrow above the plain-language query summary">
            Shows tasks where
          </p>
          <p class="mt-1 line-clamp-3 text-sm">{{ summary() }}</p>
        </div>

        <div
          class="text-muted mt-auto flex flex-wrap items-center gap-x-4 gap-y-1 text-xs">
          <span class="flex items-center gap-1.5 tabular-nums">
            <svg lucideListFilter class="h-3.5 w-3.5"></svg>
            {{ filterLabel() }}
          </span>
          <span class="flex items-center gap-1.5 tabular-nums">
            <svg lucideColumns3 class="h-3.5 w-3.5"></svg>
            {{ columnLabel() }}
          </span>
        </div>
      </div>

      <footer
        class="border-border text-muted flex flex-wrap items-center gap-x-2 gap-y-1 border-t px-5 py-3 text-xs">
        @if (view().createdByDisplayName; as author) {
          <app-avatar size="xs" [name]="author" />
          <span class="min-w-0 truncate">{{ author }}</span>
          <span aria-hidden="true">·</span>
        }
        <span>
          {{ view().updatedAt ?? view().createdAt | date: 'mediumDate' }}
        </span>
      </footer>
    </article>
  `,
})
export class TaskViewCardComponent {
  private readonly fieldOptions = inject(QueryFieldOptionsService);

  readonly view = input.required<TaskView>();
  readonly catalog = input.required<TaskQueryCatalog>();
  readonly pinned = input(false);
  readonly canDelete = input(false);

  readonly pinToggled = output();
  readonly deleted = output();

  protected readonly viewIcon = LucideListFilter;

  protected readonly pinLabel = computed(() => {
    return this.pinned()
      ? $localize`:Button that removes a view from the sidebar:Unpin from sidebar`
      : $localize`:Button that adds a view to the sidebar:Pin to sidebar`;
  });

  protected readonly summary = computed(() => {
    const query = this.view().definition?.query;

    if (!query) return '';

    return this.fieldOptions.explain(query, this.catalog());
  });

  protected readonly filterLabel = computed(() => {
    const count = countConditions(this.view().definition?.query);

    if (count === 0) {
      return $localize`:Shown on a saved view that filters nothing out:No filters`;
    }

    return count === 1
      ? $localize`:Shown on a saved view with exactly one filter:1 filter`
      : $localize`:Number of filters on a saved view. COUNT is how many there are:${count}:COUNT: filters`;
  });

  protected readonly columnLabel = computed(() => {
    const count = visibleTaskColumnIds(
      this.view().definition?.display?.columns ?? []
    ).length;

    return count === 1
      ? $localize`:Shown on a saved view that displays one column:1 column`
      : $localize`:Number of columns a saved view displays. COUNT is how many there are:${count}:COUNT: columns`;
  });
}

function countConditions(group: TaskQueryGroup | null | undefined): number {
  if (!group) return 0;

  const nested = group.groups.reduce((total, child) => {
    return total + countConditions(child);
  }, 0);

  return group.conditions.length + nested;
}
