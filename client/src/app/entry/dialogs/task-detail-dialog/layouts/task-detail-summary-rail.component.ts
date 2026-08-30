import { Component, computed, inject, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiAssistantService } from '@core/services/ai-assistant.service';
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
import { TaskDetailAccordionRowComponent } from '../shared/task-detail-accordion-row.component';
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
    LucideSparkles,
    LucideTrash2,
    TaskDetailAccordionRowComponent,
    TaskDetailChromeComponent,
    TaskDetailHeaderComponent,
    TaskDetailTagRowComponent,
    TaskDetailFlagsComponent,
    TaskDetailDescriptionComponent,
    TaskDetailBoardsComponent,
    TaskDetailRelationsComponent,
    TaskDetailFilesComponent,
    TaskDetailCommentsComponent,
    TaskDetailComposerComponent,
    TaskDetailFieldRowsComponent,
    TaskDetailStatusSegmentsComponent,
    TaskDetailTimestampsComponent,
  ],
  providers: [TaskDetailCommentsService],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    @if (task(); as task) {
      <app-task-detail-chrome
        class="border-foreground/8 h-[50px] border-b"
        [showOverflow]="false" />

      <div class="flex min-h-0 flex-1 flex-row max-[1200px]:flex-col">
        <div class="flex min-w-0 flex-1 flex-col">
          <div
            class="custom-scroll flex min-h-0 flex-1 flex-col gap-[18px] overflow-y-auto px-7 pt-6 pb-5">
            <app-task-detail-header
              textClass="-mx-2 px-2 py-1 text-[28px]/[36px] font-semibold tracking-[-0.012em]" />

            @if (readTags()) {
              <app-task-detail-tag-row />
            }

            @if (readFlags()) {
              <app-task-detail-flags />
            }

            <app-task-detail-description textClass="text-[15px]/[26px]" />

            <div class="border-foreground/8 mt-auto flex flex-col border-t">
              <app-task-detail-accordion-row
                [label]="labels.boards"
                [summary]="boardSummary()"
                [tone]="task.placements.length ? 'muted' : 'faint'"
                [expanded]="isExpanded('boards')"
                (toggled)="toggle('boards')">
                <span class="text-foreground/40 shrink-0 pr-1 text-xs">
                  {{ task.placements.length }}
                </span>
              </app-task-detail-accordion-row>
              @if (isExpanded('boards')) {
                <div class="pt-1 pb-3">
                  <app-task-detail-boards />
                </div>
              }

              <app-task-detail-accordion-row
                [label]="labels.links"
                [summary]="linkSummary(relations.count())"
                [tone]="relations.count() ? 'muted' : 'faint'"
                [expanded]="isExpanded('links')"
                (toggled)="toggle('links')">
                @if (canUpdate()) {
                  <button
                    type="button"
                    class="text-primary hover:bg-hover shrink-0 cursor-pointer rounded px-2 py-1 text-xs font-medium transition-colors"
                    (click)="relations.openLinkDialog()">
                    <span i18n="Button that links this task to another">
                      Link task
                    </span>
                  </button>
                }
              </app-task-detail-accordion-row>
              <div class="pt-1 pb-3" [class.hidden]="!isExpanded('links')">
                <app-task-detail-relations #relations />
              </div>

              @if (readFiles()) {
                <app-task-detail-accordion-row
                  [label]="labels.files"
                  [summary]="fileSummary(files.count())"
                  [tone]="files.count() ? 'muted' : 'faint'"
                  [last]="true"
                  [expanded]="isExpanded('files')"
                  (toggled)="toggle('files')">
                  <button
                    type="button"
                    class="text-primary hover:bg-hover shrink-0 cursor-pointer rounded px-2 py-1 text-xs font-medium transition-colors"
                    (click)="expand('files')">
                    <span i18n="Button that opens the file picker">
                      Choose files
                    </span>
                  </button>
                </app-task-detail-accordion-row>
                <div class="pt-1 pb-3" [class.hidden]="!isExpanded('files')">
                  <app-task-detail-files #files />
                </div>
              }
            </div>
          </div>

          @if (readComments()) {
            <div
              class="border-foreground/8 bg-foreground/[0.02] flex shrink-0 flex-col gap-3 border-t px-7 pt-3 pb-3.5"
              [class.min-h-0]="commentsExpanded()"
              [class.flex-1]="commentsExpanded()">
              <div class="flex items-center gap-2.5">
                <span class="text-[13px] font-semibold">
                  <span i18n="Section heading for a task's comments">
                    Comments
                  </span>
                </span>
                <span class="text-foreground/40 text-xs">
                  {{ comments.count() }}
                </span>
                <button
                  type="button"
                  class="text-primary ml-auto cursor-pointer text-xs font-medium"
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
                    <span class="text-foreground/40 ml-1.5 text-[11px]">
                      {{ latest.createdAt | fromNow }}
                    </span>
                    <div class="line-clamp-2">{{ latest.body }}</div>
                  </div>
                </div>
              }

              <app-task-detail-composer />
            </div>
          }
        </div>

        <div
          class="border-foreground/8 bg-foreground/[0.02] flex w-[340px] shrink-0 flex-col border-l max-[1200px]:w-full max-[1200px]:border-t max-[1200px]:border-l-0">
          <app-task-detail-status-segments
            class="border-foreground/8 border-b px-5 pt-4.5 pb-4" />

          <app-task-detail-field-rows
            class="custom-scroll min-h-0 flex-1 overflow-y-auto px-2 pt-2 pb-4"
            [fields]="railFields"
            [foldEmptyFields]="true" />

          <div
            class="border-foreground/8 flex shrink-0 gap-2 border-t px-4 py-3">
            @if (canAskAssistant()) {
              <button
                type="button"
                class="border-foreground/8 hover:bg-hover flex h-8.5 flex-1 cursor-pointer items-center justify-center gap-2 rounded-lg border text-xs font-medium transition-colors"
                (click)="askAssistant()">
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
          </div>

          <app-task-detail-timestamps
            class="border-foreground/8 shrink-0 border-t px-4 pt-2.5 pb-3.5" />
        </div>
      </div>
    }
  `,
})
export class TaskDetailSummaryRailComponent {
  readonly taskDetail = inject(TaskDetailService);
  readonly comments = inject(TaskDetailCommentsService);

  private readonly assistant = inject(AiAssistantService);
  private readonly expanded = signal(new Set<Section>());

  readonly task = this.taskDetail.task;
  readonly commentsExpanded = signal(false);
  readonly railFields = RAIL_FIELDS;

  readonly labels = {
    boards: $localize`:Section heading for the boards a task appears on:Boards`,
    links: $localize`:Section heading for links between tasks:Linked tasks`,
    files: $localize`:Section heading for files attached to a task:Files`,
  };

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly canDeleteTask = hasPermission(PERMISSIONS.tasks.delete);
  readonly readTags = hasPermission(PERMISSIONS.tags.read);
  readonly readFiles = hasPermission(PERMISSIONS.files.read);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);
  readonly readComments = hasPermission(PERMISSIONS.comments.read);

  readonly canAskAssistant = computed(() => {
    return this.assistant.isAvailable() && this.task() !== null;
  });

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
    this.expanded.update((sections) => {
      const next = new Set(sections);

      if (!next.delete(section)) next.add(section);

      return next;
    });
  }

  expand(section: Section) {
    this.expanded.update((sections) => new Set(sections).add(section));
  }

  linkSummary(count: number) {
    if (!count) {
      return $localize`:Accordion summary when a task links to nothing:None yet`;
    }

    if (count === 1) {
      return $localize`:Accordion summary when a task links to one other:1 linked task`;
    }

    return $localize`:Accordion summary counting the tasks this one links to. COUNT is how many:${count}:COUNT: linked tasks`;
  }

  fileSummary(count: number) {
    if (!count) {
      return $localize`:Prompt on the collapsed files section:Drop a file to attach`;
    }

    if (count === 1) {
      return $localize`:Accordion summary when a task has one attachment:1 file`;
    }

    return $localize`:Accordion summary counting a task's attachments. COUNT is how many:${count}:COUNT: files`;
  }

  askAssistant() {
    const task = this.task();

    if (!task) return;

    this.assistant.askAboutTask(task);
  }
}
