import { Component, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { LucideSparkles, LucideTrash2 } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { TaskDetailBoardsComponent } from '../task-detail-boards.component';
import { TaskDetailCommentsComponent } from '../task-detail-comments.component';
import { TaskDetailDescriptionComponent } from '../task-detail-description.component';
import { TaskDetailFilesComponent } from '../task-detail-files.component';
import { TaskDetailFlagsComponent } from '../task-detail-flags.component';
import { TaskDetailHeaderComponent } from '../task-detail-header.component';
import { TaskDetailRelationsComponent } from '../task-detail-relations.component';
import { AccordionComponent } from '@static/components/accordion/accordion.component';
import { AccordionRowComponent } from '@static/components/accordion/accordion-row.component';
import { DialogColumnsComponent } from '@static/components/dialog/dialog-columns.component';
import { DialogRailComponent } from '@static/components/dialog/dialog-rail.component';
import { DialogSectionComponent } from '@static/components/dialog/dialog-section.component';
import { TaskFilesSectionComponent } from '../parts/task-files-section.component';
import { TaskLinksSectionComponent } from '../parts/task-links-section.component';
import { TaskDetailChromeComponent } from '../shared/task-detail-chrome.component';
import { TaskDetailCommentsService } from '../shared/task-detail-comments.service';
import { TaskDetailComposerComponent } from '../shared/task-detail-composer.component';
import {
  TaskDetailField,
  TaskDetailFieldRowsComponent,
} from '../shared/task-detail-field-rows.component';
import { TaskDetailStatusSegmentsComponent } from '../shared/task-detail-status-segments.component';
import { TaskDetailTagRowComponent } from '../shared/task-detail-tag-row.component';
import { TaskDetailTimestampsComponent } from '../shared/task-detail-timestamps.component';
import { TaskDetailService } from '../task-detail.service';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { toggleInSet } from '@core/util/signals';

type Section = 'boards' | 'links' | 'files';

const RAIL_FIELDS: TaskDetailField[] = [
  'assignee',
  'reporter',
  'priority',
  'project',
  'sprint',
  'estimate',
  'startDate',
  'dueDate',
];

@Component({
  selector: 'app-task-detail-summary-rail',
  imports: [
    AvatarComponent,
    FromNowPipe,
    InlineButtonComponent,
    LucideSparkles,
    LucideTrash2,
    AccordionComponent,
    AccordionRowComponent,
    DialogColumnsComponent,
    DialogRailComponent,
    DialogSectionComponent,
    TaskFilesSectionComponent,
    TaskLinksSectionComponent,
    TaskDetailBoardsComponent,
    TaskDetailChromeComponent,
    TaskDetailCommentsComponent,
    TaskDetailComposerComponent,
    TaskDetailDescriptionComponent,
    TaskDetailFieldRowsComponent,
    TaskDetailFilesComponent,
    TaskDetailFlagsComponent,
    TaskDetailHeaderComponent,
    TaskDetailRelationsComponent,
    TaskDetailStatusSegmentsComponent,
    TaskDetailTagRowComponent,
    TaskDetailTimestampsComponent,
  ],
  providers: [TaskDetailCommentsService],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    @if (task(); as task) {
      <app-task-detail-chrome
        class="border-foreground/8 h-[50px] border-b"
        [showOverflow]="false" />

      <app-dialog-columns>
        <app-task-detail-header
          textClass="-mx-2 px-2 py-1 text-[28px]/[36px] font-semibold tracking-[-0.012em]" />

        @if (readTags()) {
          <app-task-detail-tag-row />
        }

        @if (readFlags()) {
          <app-task-detail-flags />
        }

        <app-task-detail-description textClass="text-[15px]/[26px]" />

        <app-accordion class="mt-auto">
          <app-accordion-row
            [label]="labels.boards"
            [summary]="boardSummary()"
            [expanded]="isExpanded('boards')"
            (toggled)="toggle('boards')">
            <span class="text-muted shrink-0 pr-1 text-xs">
              {{ task.placements.length }}
            </span>
          </app-accordion-row>
          @if (isExpanded('boards')) {
            <div class="pt-1 pb-3">
              <app-task-detail-boards />
            </div>
          }

          <app-task-links-section
            [count]="relations.count()"
            [canLink]="canUpdate()"
            [expanded]="isExpanded('links')"
            (toggled)="toggle('links')"
            (linkRequested)="relations.openLinkDialog()">
            <app-task-detail-relations #relations />
          </app-task-links-section>

          @if (readFiles()) {
            <app-task-files-section
              last
              [count]="files.count()"
              [expanded]="isExpanded('files')"
              (toggled)="toggle('files')"
              (chooseRequested)="expand('files')">
              <app-task-detail-files #files />
            </app-task-files-section>
          }
        </app-accordion>

        @if (readComments()) {
          <div
            class="border-foreground/8 bg-foreground/[0.02] flex shrink-0 flex-col gap-3 border-t px-7 pt-3 pb-3.5"
            dialogColumnsFooter
            [class.min-h-0]="commentsExpanded()"
            [class.flex-1]="commentsExpanded()">
            <div class="flex items-center gap-2.5">
              <span class="text-[13px] font-semibold">
                <span i18n="Section heading for a task's comments">
                  Comments
                </span>
              </span>
              <span class="text-muted text-xs">
                {{ comments.count() }}
              </span>
              <button
                type="button"
                app-inline-button
                class="ml-auto font-medium"
                (click)="commentsExpanded.set(!commentsExpanded())">
                @if (commentsExpanded()) {
                  <span i18n="Collapses the expanded comment list">
                    Show less
                  </span>
                } @else {
                  <span i18n="Expands the comment list over the task body">
                    Show all
                  </span>
                }
              </button>
            </div>

            @if (commentsExpanded()) {
              <div class="custom-scroll min-h-0 flex-1 overflow-y-auto">
                <app-task-detail-comments />
              </div>
            } @else if (comments.latest(); as latest) {
              <div class="flex gap-2.5">
                <app-avatar
                  size="sm"
                  [tooltip]="false"
                  [name]="latest.userDisplayName"
                  [imageUrl]="latest.userDisplayImage"
                  [isServiceAccount]="latest.userIsServiceAccount ?? false" />
                <div class="min-w-0 text-[13px]/[20px]">
                  <span class="font-semibold">
                    {{ latest.userDisplayName }}
                  </span>
                  <span class="text-muted ml-1.5 text-[11px]">
                    {{ latest.createdAt | fromNow }}
                  </span>
                  <div class="line-clamp-2">{{ latest.body }}</div>
                </div>
              </div>
            }

            <app-task-detail-composer />
          </div>
        }

        <app-dialog-rail>
          <app-dialog-section divider="bottom" class="px-5 pt-4.5 pb-4">
            <app-task-detail-status-segments />
          </app-dialog-section>

          <app-task-detail-field-rows
            class="custom-scroll min-h-0 flex-1 overflow-y-auto px-2 pt-2 pb-4"
            [fields]="railFields"
            [foldEmptyFields]="true" />

          <app-dialog-section
            divider="top"
            class="flex shrink-0 gap-2 px-4 py-3">
            @if (canAskAssistant()) {
              <button
                type="button"
                class="border-foreground/8 hover:bg-hover flex h-8.5 flex-1 cursor-pointer items-center justify-center gap-2 rounded-lg border text-xs font-medium transition-colors"
                (click)="taskDetail.askAssistant()">
                <svg lucideSparkles class="h-3.5 w-3.5"></svg>
                <span i18n="Button that asks the assistant about this task">
                  Ask assistant
                </span>
              </button>
            }
            @if (canDeleteTask()) {
              <button
                type="button"
                class="border-foreground/8 text-muted hover:bg-hover hover:text-foreground flex h-8.5 w-10 shrink-0 cursor-pointer items-center justify-center rounded-lg border transition-colors"
                i18n-aria-label="
                  Accessible label for the button that deletes the task
                "
                aria-label="Delete task"
                (click)="taskDetail.deleteTask()">
                <svg lucideTrash2 class="h-3.5 w-3.5"></svg>
              </button>
            }
          </app-dialog-section>

          <app-dialog-section divider="top" class="shrink-0 px-4 pt-2.5 pb-3.5">
            <app-task-detail-timestamps />
          </app-dialog-section>
        </app-dialog-rail>
      </app-dialog-columns>
    }
  `,
})
export class TaskDetailSummaryRailComponent {
  readonly taskDetail = inject(TaskDetailService);
  readonly comments = inject(TaskDetailCommentsService);

  private readonly expanded = signal<ReadonlySet<Section>>(new Set());

  readonly task = this.taskDetail.task;
  readonly commentsExpanded = signal(false);
  readonly railFields = RAIL_FIELDS;

  readonly labels = {
    boards: $localize`:Section heading for the boards a task appears on:Boards`,
  };

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly canDeleteTask = hasPermission(PERMISSIONS.tasks.delete);
  readonly readTags = hasPermission(PERMISSIONS.tags.read);
  readonly readFiles = hasPermission(PERMISSIONS.files.read);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);
  readonly readComments = hasPermission(PERMISSIONS.comments.read);

  readonly canAskAssistant = this.taskDetail.canAskAssistant;

  readonly boardSummary = computed(() => {
    const placements = this.task()?.placements ?? [];

    if (!placements.length) {
      return $localize`:Accordion summary when a task is not on any board:Not on any board`;
    }

    return placements
      .map(
        (placement) => `${placement.boardName} · ${placement.boardGroupName}`
      )
      .join(', ');
  });

  isExpanded(section: Section) {
    return this.expanded().has(section);
  }

  toggle(section: Section) {
    toggleInSet(this.expanded, section);
  }

  expand(section: Section) {
    toggleInSet(this.expanded, section, true);
  }
}
