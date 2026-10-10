import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiProposedChange } from '@core/models/ai-conversation';
import {
  LucideCheck,
  LucideExternalLink,
  LucideMessageSquare,
  LucidePencil,
  LucideTriangleAlert,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { ButtonComponent } from '@static/components/button/button.component';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import {
  AiChangeGroup,
  changeRoute,
  groupAction,
  groupKeyLabel,
  groupTarget,
  groupTone,
  isTextField,
  isValid,
  selectableIds,
} from './ai-assistant-change-group';
import { changeAction, changeTone } from './ai-assistant-change-kind';
import { AiDiffMode } from './ai-assistant-diff';
import { AiAssistantReviewDiffComponent } from './ai-assistant-review-diff.component';

/** One field of one change, the unit a reviewer rewrites by hand. */
export interface AiFieldTarget {
  changeId: number;
  name: string;
}

export interface AiFieldEdit extends AiFieldTarget {
  value: string;
}

interface AiChangeNotice {
  id: number;
  title: string;
  message: string;
}

/** The right hand pane of a review: what one entity's changes do, field by field. */
@Component({
  selector: 'app-ai-assistant-review-detail',
  host: { class: 'flex min-h-0 min-w-0 flex-col' },
  imports: [
    RouterLink,
    BadgeComponent,
    ButtonComponent,
    ButtonLinkComponent,
    CalloutComponent,
    EmptyStateComponent,
    LucideCheck,
    LucideExternalLink,
    LucideMessageSquare,
    LucidePencil,
    AiAssistantReviewDiffComponent,
    TaskScopeIdComponent,
  ],
  template: `
    <div class="border-border flex flex-col gap-2 border-b px-5 py-4">
      <div class="flex items-center gap-4">
        <div class="flex min-w-0 flex-1 items-center gap-2.5">
          <app-badge shape="rounded" class="font-bold" [color]="tone()">
            {{ action() }}
          </app-badge>
          @if (systemId(); as systemId) {
            <app-task-scope-id class="shrink-0" [id]="systemId" />
          } @else {
            <span class="text-muted font-avatar text-xs">{{
              entityKey()
            }}</span>
          }
          @if (route(); as route) {
            <a
              app-button-link
              color="primary"
              class="-my-1 h-7 min-h-0 shrink-0 gap-1.5 px-2 text-[13px]"
              [routerLink]="route"
              i18n-title="Tooltip on the link that opens the changed entity"
              title="Open in a new view">
              <span i18n="Link that opens the entity a change targets">
                Open
              </span>
              <svg lucideExternalLink class="h-3.5 w-3.5"></svg>
            </a>
          }
        </div>

        <div class="flex shrink-0 items-center gap-2">
          @if (isPending()) {
            <button
              type="button"
              role="switch"
              class="border-border hover:bg-hover flex h-10 items-center gap-2.5 rounded-lg border pr-3.5 pl-3 text-sm transition-colors disabled:cursor-not-allowed disabled:opacity-45 disabled:hover:bg-transparent"
              [attr.aria-checked]="isIncluded()"
              [disabled]="!isSelectable() || isApplying()"
              [title]="includeHint()"
              (click)="toggled.emit(selectable())">
              <span
                class="relative h-4 w-7 shrink-0 rounded-full transition-colors"
                [class]="isIncluded() ? 'bg-primary' : 'bg-foreground/20'">
                <span
                  class="absolute top-0.5 left-0.5 h-3 w-3 rounded-full transition-transform"
                  [class]="
                    isIncluded()
                      ? 'translate-x-3 bg-white dark:bg-black/80'
                      : 'bg-white dark:bg-white/75'
                  "></span>
              </span>
              <span>{{ includeLabel() }}</span>
            </button>
            <span class="bg-border mx-0.5 h-5 w-px"></span>
          }
          @if (canRevise()) {
            @if (editableField(); as target) {
              <app-button
                variant="outlined"
                color="neutral"
                class="h-10 rounded-lg px-3.5 text-sm"
                (click)="editStarted.emit(target)">
                <svg lucidePencil class="h-4 w-4"></svg>
                <span i18n="Button that edits the value a change proposes">
                  Edit
                </span>
              </app-button>
            }
            <app-button
              variant="outlined"
              color="neutral"
              class="h-10 rounded-lg px-3.5 text-sm"
              (click)="revised.emit(group().changes[0].id)">
              <svg lucideMessageSquare class="h-4 w-4"></svg>
              <span i18n="Button that asks the assistant to rework one change">
                Ask to revise
              </span>
            </app-button>
          }
          @if (isPending() && isSelectable()) {
            <app-button
              variant="outlined"
              color="primary"
              class="border-primary/40 bg-primary/10 hover:bg-primary/15 h-10 rounded-lg px-4 text-sm font-medium"
              [disabled]="isApplying()"
              (click)="applied.emit(selectable())">
              <svg lucideCheck class="h-4 w-4" strokeWidth="2.2"></svg>
              <span i18n="Button that applies only the change being viewed">
                Apply this
              </span>
            </app-button>
          }
        </div>
      </div>

      <h2
        class="font-overpass m-0 text-[19px] leading-snug font-medium text-pretty">
        {{ target() }}
      </h2>
      <p class="text-muted m-0 text-sm">{{ summary() }}</p>
    </div>

    <div class="custom-scroll flex-1 overflow-auto px-5 pt-4 pb-6">
      <div class="flex flex-col gap-4">
        @for (notice of notices(); track notice.id) {
          <app-callout
            color="warn"
            role="alert"
            [icon]="alertIcon"
            [title]="notice.title">
            <p class="text-muted m-0 mt-0.5 text-sm wrap-break-word">
              {{ notice.message }}
            </p>
          </app-callout>
        }

        @for (change of group().changes; track change.id) {
          @if (isMultiple()) {
            <div class="flex min-w-0 items-center gap-2 pt-1">
              <app-badge
                shape="rounded"
                class="shrink-0 px-1.5 text-[10.5px] font-bold"
                [color]="changeTone(change)">
                {{ changeAction(change) }}
              </app-badge>
              <span class="text-muted min-w-0 truncate text-[13px]">
                {{ change.summary }}
              </span>
            </div>
          }
          @for (field of change.fields; track field.name) {
            <app-ai-assistant-review-diff
              [field]="field"
              [mode]="mode()"
              [canEdit]="canRevise() && isPending() && isText(field)"
              [isEditing]="isEditing(change, field.name)"
              [isSaving]="isSaving()"
              [error]="isEditing(change, field.name) ? editError() : null"
              (editStarted)="
                editStarted.emit({ changeId: change.id, name: $event })
              "
              (editCancelled)="editCancelled.emit()"
              (saved)="
                saved.emit({
                  changeId: change.id,
                  name: field.name,
                  value: $event,
                })
              " />
          } @empty {
            <app-empty-state compact [title]="noFieldsLabel" />
          }
        }
      </div>
    </div>
  `,
})
export class AiAssistantReviewDetailComponent {
  readonly group = input.required<AiChangeGroup>();
  readonly excludedChangeIds = input<Set<number>>(new Set());
  readonly mode = input<AiDiffMode>('split');
  readonly isPending = input(false);
  readonly isApplying = input(false);
  readonly canRevise = input(true);
  readonly editingField = input<AiFieldTarget | null>(null);
  readonly editError = input<string | null>(null);
  readonly isSaving = input(false);
  readonly workspace = input<string | null>(null);

  readonly applied = output<number[]>();
  readonly toggled = output<number[]>();
  readonly revised = output<number>();
  readonly editStarted = output<AiFieldTarget>();
  readonly editCancelled = output();
  readonly saved = output<AiFieldEdit>();

  protected readonly changeAction = changeAction;
  protected readonly changeTone = changeTone;
  protected readonly isText = isTextField;
  protected readonly alertIcon = LucideTriangleAlert;

  protected readonly noFieldsLabel = $localize`:Shown when a proposed change carries no field values:This change has no field values to compare.`;

  protected readonly action = computed(() => groupAction(this.group()));
  protected readonly tone = computed(() => groupTone(this.group()));
  protected readonly entityKey = computed(() => groupKeyLabel(this.group()));
  protected readonly systemId = computed(() => {
    return this.group().changes[0].entitySystemId ?? null;
  });
  protected readonly target = computed(() => groupTarget(this.group()));
  protected readonly isMultiple = computed(() => {
    return this.group().changes.length > 1;
  });

  protected readonly summary = computed(() => {
    return this.group()
      .changes.map((change) => change.summary)
      .join(' · ');
  });

  protected readonly selectable = computed(() => selectableIds(this.group()));
  protected readonly isSelectable = computed(() => {
    return this.selectable().length > 0;
  });

  protected readonly isIncluded = computed(() => {
    const excluded = this.excludedChangeIds();

    return this.selectable().some((id) => !excluded.has(id));
  });

  protected readonly includeLabel = computed(() => {
    if (!this.isSelectable()) {
      return $localize`:Shown on the include switch of a change that cannot be applied:Can’t include`;
    }

    if (this.isIncluded()) {
      return $localize`:Shown on the include switch of a change that will be applied:Included`;
    }

    return $localize`:Shown on the include switch of a change left out of the apply:Skipped`;
  });

  protected readonly includeHint = computed(() => {
    if (!this.isSelectable()) {
      return $localize`:Tooltip on the include switch of a change that cannot be applied:This change can’t be applied`;
    }

    if (this.isIncluded()) {
      return $localize`:Tooltip on the include switch of an included change:Skip this change when applying`;
    }

    return $localize`:Tooltip on the include switch of a skipped change:Include this change when applying`;
  });

  /** Only prose can be rewritten here; a status or an assignee needs the entity behind it. */
  protected readonly editableField = computed<AiFieldTarget | null>(() => {
    if (!this.isPending()) {
      return null;
    }

    for (const change of this.group().changes) {
      const field = change.fields.find(isTextField);

      if (field) {
        return { changeId: change.id, name: field.name };
      }
    }

    return null;
  });

  protected readonly route = computed(() => {
    return changeRoute(this.group().changes[0], this.workspace());
  });

  protected readonly notices = computed<AiChangeNotice[]>(() => {
    return this.group().changes.flatMap((change) => {
      const notice = this.notice(change);

      return notice ? [notice] : [];
    });
  });

  protected isEditing(change: AiProposedChange, name: string): boolean {
    const editing = this.editingField();

    return editing?.changeId === change.id && editing.name === name;
  }

  private notice(change: AiProposedChange): AiChangeNotice | null {
    if (change.applyError) {
      return {
        id: change.id,
        title: $localize`:Heading above the reason a change failed:This change failed to apply.`,
        message: change.applyError,
      };
    }

    if (isValid(change)) {
      return null;
    }

    return {
      id: change.id,
      title: $localize`:Heading above the reason a change is blocked:This change cannot be applied.`,
      message:
        change.validationMessage ??
        $localize`:Shown on a proposal that cannot be applied:This change cannot be applied.`,
    };
  }
}
