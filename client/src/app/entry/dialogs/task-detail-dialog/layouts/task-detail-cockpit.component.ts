import { Component, inject, signal, viewChild } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { cn } from '@static/components/button/button.variants';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskDetailBoardsComponent } from '../task-detail-boards.component';
import { TaskDetailCommentsComponent } from '../task-detail-comments.component';
import { TaskDetailDescriptionComponent } from '../task-detail-description.component';
import { TaskDetailFilesComponent } from '../task-detail-files.component';
import { TaskDetailFlagsComponent } from '../task-detail-flags.component';
import { TaskDetailHeaderComponent } from '../task-detail-header.component';
import { TaskDetailRelationsComponent } from '../task-detail-relations.component';
import { TaskDetailActivityFeedComponent } from '../shared/task-detail-activity-feed.component';
import { TaskDetailChromeComponent } from '../shared/task-detail-chrome.component';
import { TaskDetailCommentsService } from '../shared/task-detail-comments.service';
import { TaskDetailComposerComponent } from '../shared/task-detail-composer.component';
import { TaskDetailPropertyChipsComponent } from '../shared/task-detail-property-chips.component';
import { TaskDetailTagRowComponent } from '../shared/task-detail-tag-row.component';
import {
  TaskDetailTab,
  TaskDetailTabsComponent,
} from '../shared/task-detail-tabs.component';
import { TaskDetailTimestampsComponent } from '../shared/task-detail-timestamps.component';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-cockpit',
  imports: [
    TaskDetailChromeComponent,
    TaskDetailHeaderComponent,
    TaskDetailPropertyChipsComponent,
    TaskDetailTagRowComponent,
    TaskDetailFlagsComponent,
    TaskDetailDescriptionComponent,
    TaskDetailTabsComponent,
    TaskDetailBoardsComponent,
    TaskDetailRelationsComponent,
    TaskDetailFilesComponent,
    TaskDetailCommentsComponent,
    TaskDetailComposerComponent,
    TaskDetailActivityFeedComponent,
    TaskDetailTimestampsComponent,
  ],
  providers: [TaskDetailCommentsService],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    @if (task(); as task) {
      <app-task-detail-chrome
        class="border-foreground/8 h-[52px] border-b"
        [showActivity]="false" />

      <div
        class="border-foreground/8 flex shrink-0 flex-col gap-3.5 border-b px-7 pt-5.5 pb-4">
        <app-task-detail-header
          textClass="-mx-2 px-2 py-1 text-[27px]/[34px] font-semibold tracking-[-0.01em]" />

        <div class="flex flex-wrap items-center gap-2">
          <app-task-detail-property-chips variant="bar" />

          @if (readTags()) {
            <span
              class="bg-foreground/8 mx-1 h-5 w-px shrink-0"
              aria-hidden="true"></span>
            <app-task-detail-tag-row size="md" />
          }
        </div>

        @if (readFlags()) {
          <app-task-detail-flags />
        }
      </div>

      <div class="flex min-h-0 flex-1 flex-row max-[1200px]:flex-col">
        <div class="flex min-w-0 flex-1 flex-col">
          <div
            class="custom-scroll min-h-0 flex-1 overflow-y-auto px-7 pt-5.5 pb-6">
            <app-task-detail-description
              [label]="descriptionLabel"
              textClass="text-[15px]/[26px]" />
          </div>

          <div class="border-foreground/8 shrink-0 border-t">
            <div class="flex h-[42px] items-center gap-1 px-5">
              <app-task-detail-tabs
                class="gap-1"
                role="tablist"
                [tabs]="
                  tabItems(
                    task.placements.length,
                    relations.count(),
                    filesSection()?.count() ?? null
                  )
                "
                [(active)]="activeTab" />

              <div class="ml-auto">
                @if (activeTab() === 'boards' && boards.canAdd()) {
                  <button
                    #addToBoard
                    type="button"
                    [class]="tabActionClass"
                    (click)="boards.openAddMenu(addToBoard)">
                    <span i18n="Button that puts this task on another board">
                      Add to board
                    </span>
                  </button>
                }
                @if (activeTab() === 'links' && canUpdate()) {
                  <button
                    type="button"
                    [class]="tabActionClass"
                    (click)="relations.openLinkDialog()">
                    <span i18n="Button that links this task to another">
                      Link task
                    </span>
                  </button>
                }
              </div>
            </div>

            <div class="max-h-56 overflow-y-auto px-5 pt-4 pb-4">
              <div [class.hidden]="activeTab() !== 'boards'">
                <app-task-detail-boards #boards />
              </div>
              <div [class.hidden]="activeTab() !== 'links'">
                <app-task-detail-relations #relations />
              </div>
              @if (readFiles()) {
                <div [class.hidden]="activeTab() !== 'files'">
                  <app-task-detail-files />
                </div>
              }
            </div>
          </div>
        </div>

        <div
          class="border-foreground/8 bg-foreground/[0.02] flex w-[372px] shrink-0 flex-col border-l max-[1200px]:w-full max-[1200px]:border-t max-[1200px]:border-l-0">
          <div
            class="border-foreground/8 flex h-11 shrink-0 items-center gap-2 border-b px-4">
            <div class="bg-hover flex gap-0.5 rounded-[7px] p-0.5">
              @if (readComments()) {
                <button
                  type="button"
                  [class]="segmentClass(railTab() === 'comments')"
                  [attr.aria-pressed]="railTab() === 'comments'"
                  (click)="railTab.set('comments')">
                  <span i18n="Section heading for a task's comments">
                    Comments
                  </span>
                </button>
              }
              @if (readActivity()) {
                <button
                  type="button"
                  [class]="segmentClass(railTab() === 'history')"
                  [attr.aria-pressed]="railTab() === 'history'"
                  (click)="railTab.set('history')">
                  <span i18n="Tab showing what has happened to a task">
                    History
                  </span>
                </button>
              }
            </div>
            @if (railTab() === 'comments') {
              <span class="text-foreground/40 ml-auto text-xs">
                {{ comments.count() }}
              </span>
            }
          </div>

          <div class="custom-scroll min-h-0 flex-1 overflow-y-auto px-4 py-3.5">
            @if (railTab() === 'comments') {
              <app-task-detail-comments />
            } @else {
              <app-task-detail-activity-feed
                [enabled]="railTab() === 'history'" />
            }
          </div>

          @if (readComments() && railTab() === 'comments') {
            <app-task-detail-composer
              class="border-foreground/8 shrink-0 border-t px-4 py-3"
              [showAvatar]="false" />
          }
        </div>
      </div>

      <app-task-detail-timestamps
        class="border-foreground/8 h-10 shrink-0 border-t px-5"
        [showReporter]="true" />
    }
  `,
})
export class TaskDetailCockpitComponent {
  readonly taskDetail = inject(TaskDetailService);
  readonly comments = inject(TaskDetailCommentsService);

  protected readonly filesSection = viewChild(TaskDetailFilesComponent);

  readonly task = this.taskDetail.task;
  readonly activeTab = signal('boards');
  readonly railTab = signal<'comments' | 'history'>('comments');

  readonly descriptionLabel = $localize`:Eyebrow above the task description:Description`;
  readonly tabActionClass =
    'border-foreground/8 hover:bg-hover h-[30px] cursor-pointer rounded-[7px] border px-2.5 text-xs font-medium transition-colors';

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readTags = hasPermission(PERMISSIONS.tags.read);
  readonly readFiles = hasPermission(PERMISSIONS.files.read);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);
  readonly readComments = hasPermission(PERMISSIONS.comments.read);
  readonly readActivity = hasPermission(PERMISSIONS.activity.read);

  private readonly labels = {
    boards: $localize`:Section heading for the boards a task appears on:Boards`,
    links: $localize`:Tab listing the tasks this one links to:Links`,
    files: $localize`:Section heading for files attached to a task:Files`,
  };

  tabItems(
    boards: number,
    links: number,
    files: number | null
  ): TaskDetailTab[] {
    const tabs: TaskDetailTab[] = [
      { key: 'boards', label: this.labels.boards, count: boards },
      { key: 'links', label: this.labels.links, count: links },
    ];

    if (files !== null) {
      tabs.push({ key: 'files', label: this.labels.files, count: files });
    }

    return tabs;
  }

  segmentClass(active: boolean) {
    return cn(
      'h-6 cursor-pointer rounded-[5px] px-2.5 text-xs transition-colors',
      active
        ? 'bg-dialog-background text-foreground font-semibold'
        : 'text-muted hover:text-foreground font-medium'
    );
  }
}
