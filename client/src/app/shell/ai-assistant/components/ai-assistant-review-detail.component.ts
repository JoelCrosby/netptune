import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  AiChangeField,
  AiChangeValueKind,
  AiProposedChange,
} from '@core/models/ai-conversation';
import {
  LucideCheck,
  LucideExternalLink,
  LucideMessageSquare,
  LucidePencil,
  LucideTriangleAlert,
} from '@lucide/angular';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { ButtonComponent } from '@static/components/button/button.component';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { changeRoute, entityLabel, isValid } from './ai-assistant-change-group';
import {
  changeAction,
  changeSummary,
  changeTone,
} from './ai-assistant-change-kind';
import { AiDiffMode } from './ai-assistant-diff';
import { AiAssistantReviewDiffComponent } from './ai-assistant-review-diff.component';

export interface AiFieldEdit {
  name: string;
  value: string;
}

const isTextField = (field: AiChangeField): boolean => {
  return field.kind === AiChangeValueKind.text;
};

/** The right hand pane of a review: what one change does, field by field. */
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
  ],
  template: `
    <div class="border-border flex items-start gap-3 border-b px-4 pt-3.5 pb-3">
      <div class="flex min-w-0 flex-1 flex-col gap-1.5">
        <div class="flex min-w-0 items-center gap-2">
          <app-badge [color]="tone()">{{ action() }}</app-badge>
          <span class="text-muted shrink-0 text-[11px] tracking-wide uppercase">
            {{ entity() }}
          </span>
          <h2
            class="font-overpass m-0 min-w-0 truncate text-[15px] font-medium">
            {{ target() }}
          </h2>
          @if (route(); as route) {
            <a
              app-button-link
              color="primary"
              class="h-7 min-h-0 shrink-0 gap-1 px-2 text-xs"
              [routerLink]="route"
              i18n-title="Tooltip on the link that opens the changed entity"
              title="Open in a new view">
              <span i18n="Link that opens the entity a change targets">
                Open
              </span>
              <svg lucideExternalLink class="h-3 w-3"></svg>
            </a>
          }
        </div>
        <p class="text-muted m-0 text-xs">{{ summary() }}</p>
      </div>

      <div class="flex shrink-0 items-center gap-1.5">
        @if (canRevise()) {
          @if (editableField(); as name) {
            <app-button
              variant="outlined"
              color="neutral"
              class="h-8 px-2.5 text-xs"
              (click)="editStarted.emit(name)">
              <svg lucidePencil class="h-3.5 w-3.5"></svg>
              <span i18n="Button that edits the value a change proposes">
                Edit
              </span>
            </app-button>
          }
          <app-button
            variant="outlined"
            color="neutral"
            class="h-8 px-2.5 text-xs"
            (click)="revised.emit(change().id)">
            <svg lucideMessageSquare class="h-3.5 w-3.5"></svg>
            <span i18n="Button that asks the assistant to rework one change">
              Ask to revise
            </span>
          </app-button>
        }
        @if (isPending() && isSelectable()) {
          <app-button
            variant="outlined"
            color="primary"
            class="h-8 px-3 text-xs font-medium"
            [disabled]="isApplying()"
            (click)="applied.emit(change().id)">
            <svg lucideCheck class="h-3.5 w-3.5" strokeWidth="2.2"></svg>
            <span i18n="Button that applies only the change being viewed">
              Apply this
            </span>
          </app-button>
        }
      </div>
    </div>

    @if (message(); as message) {
      <app-callout
        color="warn"
        role="alert"
        class="mx-4 mt-3"
        [icon]="alertIcon"
        [title]="messageTitle()">
        <p class="text-muted m-0 mt-0.5 text-xs break-words">{{ message }}</p>
      </app-callout>
    }

    <div class="custom-scroll flex-1 overflow-auto px-4 pt-3.5 pb-5">
      <div class="flex flex-col gap-3">
        @for (field of change().fields; track field.name) {
          <app-ai-assistant-review-diff
            [field]="field"
            [mode]="mode()"
            [canEdit]="canRevise() && isPending() && isText(field)"
            [isEditing]="editingField() === field.name"
            [isSaving]="isSaving()"
            [error]="editingField() === field.name ? editError() : null"
            (editStarted)="editStarted.emit($event)"
            (editCancelled)="editCancelled.emit()"
            (saved)="saved.emit({ name: field.name, value: $event })" />
        } @empty {
          <app-empty-state compact [title]="noFieldsLabel" />
        }
      </div>
    </div>
  `,
})
export class AiAssistantReviewDetailComponent {
  readonly change = input.required<AiProposedChange>();
  readonly mode = input<AiDiffMode>('split');
  readonly isPending = input(false);
  readonly isApplying = input(false);
  readonly canRevise = input(true);
  readonly editingField = input<string | null>(null);
  readonly editError = input<string | null>(null);
  readonly isSaving = input(false);
  readonly workspace = input<string | null>(null);

  readonly applied = output<number>();
  readonly revised = output<number>();
  readonly editStarted = output<string>();
  readonly editCancelled = output();
  readonly saved = output<AiFieldEdit>();

  protected readonly action = computed(() => changeAction(this.change()));
  protected readonly tone = computed<BadgeColor>(() =>
    changeTone(this.change())
  );
  protected readonly entity = computed(() => {
    return entityLabel(this.change().entityType);
  });

  protected readonly isSelectable = computed(() => isValid(this.change()));

  /** Only prose can be rewritten here; a status or an assignee needs the entity behind it. */
  protected readonly editableField = computed(() => {
    if (!this.isPending()) {
      return null;
    }

    const field = this.change().fields.find(isTextField);

    return field?.name ?? null;
  });

  protected readonly isText = isTextField;
  protected readonly alertIcon = LucideTriangleAlert;

  protected readonly noFieldsLabel = $localize`:Shown when a proposed change carries no field values:This change has no field values to compare.`;

  protected readonly target = computed(() => {
    return changeSummary(this.change()).target ?? this.change().summary;
  });

  protected readonly summary = computed(() => this.change().summary);

  protected readonly route = computed(() => {
    return changeRoute(this.change(), this.workspace());
  });

  protected readonly message = computed(() => {
    const change = this.change();

    if (change.applyError) {
      return change.applyError;
    }

    if (isValid(change)) {
      return null;
    }

    return (
      change.validationMessage ??
      $localize`:Shown on a proposal that cannot be applied:This change cannot be applied.`
    );
  });

  protected readonly messageTitle = computed(() => {
    const hasFailed = !!this.change().applyError;

    if (hasFailed) {
      return $localize`:Heading above the reason a change failed:This change failed to apply.`;
    }

    return $localize`:Heading above the reason a change is blocked:This change cannot be applied.`;
  });
}
