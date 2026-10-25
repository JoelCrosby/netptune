import {
  Component,
  computed,
  inject,
  linkedSignal,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskDetailBoardsComponent } from '../task-detail-boards.component';
import { TaskDetailCommentsComponent } from '../task-detail-comments.component';
import { TaskDetailDescriptionComponent } from '../task-detail-description.component';
import { TaskDetailPagesComponent } from '../task-detail-pages.component';
import { TaskDetailCodeComponent } from '../task-detail-code.component';
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
import { TaskDetailTimestampsComponent } from '../shared/task-detail-timestamps.component';
import { TaskDetailRailService } from '../task-detail-rail';
import { TaskDetailReviewComponent } from '../task-detail-review.component';
import { TaskDetailReviewService } from '../shared/task-detail-review.service';
import { TaskDetailReviewOriginComponent } from '../shared/task-detail-review-origin.component';
import { TaskDetailService } from '../task-detail.service';
import {
  TabGroupComponent,
  type TabItem,
} from '@static/components/tab-group/tab-group.component';
import { DividerComponent } from '@static/components/divider/divider.component';
import { DialogColumnsComponent } from '@static/components/dialog/dialog-columns.component';
import { DialogRailComponent } from '@static/components/dialog/dialog-rail.component';
import { DialogSectionComponent } from '@static/components/dialog/dialog-section.component';

@Component({
  selector: 'app-task-detail-cockpit',
  imports: [
    TaskDetailPagesComponent,
    TaskDetailCodeComponent,
    DialogColumnsComponent,
    DialogRailComponent,
    DialogSectionComponent,
    DividerComponent,
    TabGroupComponent,
    TaskDetailChromeComponent,
    TaskDetailHeaderComponent,
    TaskDetailPropertyChipsComponent,
    TaskDetailTagRowComponent,
    TaskDetailFlagsComponent,
    TaskDetailDescriptionComponent,
    TaskDetailBoardsComponent,
    TaskDetailRelationsComponent,
    TaskDetailFilesComponent,
    TaskDetailCommentsComponent,
    TaskDetailComposerComponent,
    TaskDetailActivityFeedComponent,
    TaskDetailTimestampsComponent,
    TaskDetailReviewComponent,
    TaskDetailReviewOriginComponent,
  ],
  providers: [TaskDetailCommentsService, TaskDetailReviewService],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    @if (task(); as task) {
      <app-task-detail-chrome
        class="border-foreground/8 h-[52px] border-b"
        [showActivity]="false"
        [showRailToggle]="showRail()"
        [railCollapsed]="rail.collapsed()"
        (railCollapsedChange)="rail.setCollapsed($event)" />

      <div
        class="border-foreground/8 flex shrink-0 flex-col gap-3.5 border-b px-7 pt-5.5 pb-4">
        <app-task-detail-header
          textClass="-mx-2 px-2 py-1 text-[22px]/[30px] font-semibold md:text-[27px]/[34px] tracking-[-0.01em]" />

        <div class="flex flex-wrap items-center gap-2">
          <app-task-detail-property-chips variant="bar" />

          @if (readTags()) {
            <app-divider
              orientation="vertical"
              class="bg-foreground/8 mx-1 h-5" />
            <app-task-detail-tag-row size="md" />
          }
        </div>

        @if (readFlags()) {
          <app-task-detail-flags />
        }

        <app-task-detail-review-origin />
      </div>

      <app-dialog-columns bodyClass="px-7 pt-5.5 pb-6">
        <app-task-detail-description
          [label]="descriptionLabel"
          textClass="text-[15px]/[26px]" />

        <app-dialog-section dialogColumnsFooter divider="top" class="shrink-0">
          <div class="flex h-[52px] items-center gap-1 px-5">
            <app-tab-group
              variant="island"
              [tabs]="
                tabItems(
                  task.placements.length,
                  relations.count(),
                  filesSection()?.count() ?? null,
                  pagesSection()?.count() ?? null,
                  codeCount()
                )
              "
              [(value)]="activeTab" />

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

          <div
            class="custom-scroll border-foreground/8 max-h-56 overflow-y-auto border-t px-5 pt-4 pb-4">
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
            @if (readPages()) {
              <div [class.hidden]="activeTab() !== 'pages'">
                <app-task-detail-pages />
              </div>
            }
            @if (readSourceControl()) {
              <div [class.hidden]="activeTab() !== 'code'">
                <app-task-detail-code />
              </div>
            }
          </div>
        </app-dialog-section>

        @if (showRail()) {
          <app-dialog-rail class="w-[372px]" [collapsed]="rail.collapsed()">
            <div
              class="border-foreground/8 flex h-[52px] shrink-0 items-center gap-2 border-b px-4">
              <app-tab-group
                variant="island"
                [tabs]="railTabItems(comments.count(), review.openCount())"
                [(value)]="railTab" />
            </div>

            <div
              class="custom-scroll min-h-0 flex-1 overflow-y-auto px-4 py-3.5">
              @if (readComments() && railTab() === 'comments') {
                <app-task-detail-comments />
              } @else if (railTab() === 'review') {
                <app-task-detail-review />
              } @else {
                <app-task-detail-activity-feed
                  [enabled]="railTab() === 'history'" />
              }
            </div>

            @if (canCreateComment() && railTab() === 'comments') {
              <app-task-detail-composer
                class="border-foreground/8 shrink-0 border-t px-4 py-3"
                [showAvatar]="false" />
            }
          </app-dialog-rail>
        }
      </app-dialog-columns>

      <app-task-detail-timestamps
        class="border-foreground/8 h-10 shrink-0 border-t px-5"
        [showReporter]="true" />
    }
  `,
})
export class TaskDetailCockpitComponent {
  readonly taskDetail = inject(TaskDetailService);
  readonly comments = inject(TaskDetailCommentsService);
  readonly rail = inject(TaskDetailRailService);
  readonly review = inject(TaskDetailReviewService);

  protected readonly filesSection = viewChild(TaskDetailFilesComponent);
  protected readonly pagesSection = viewChild(TaskDetailPagesComponent);
  protected readonly codeSection = viewChild(TaskDetailCodeComponent);

  readonly task = this.taskDetail.task;
  protected readonly codeCount = computed(
    () => this.codeSection()?.count() ?? 0
  );

  readonly activeTab = linkedSignal<number, string>({
    source: this.codeCount,
    computation: (count, previous) => {
      const tab = previous?.value ?? 'boards';
      const isHiddenCodeTab = tab === 'code' && !count;

      return isHiddenCodeTab ? 'boards' : tab;
    },
  });
  readonly railTab = linkedSignal<string>(() => {
    return this.readComments() ? 'comments' : 'history';
  });

  readonly descriptionLabel = $localize`:Eyebrow above the task description:Description`;
  readonly tabActionClass =
    'border-foreground/8 hover:bg-hover h-[30px] cursor-pointer rounded-[7px] border px-2.5 text-xs font-medium transition-colors';

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readTags = hasPermission(PERMISSIONS.tags.read);
  readonly readFiles = hasPermission(PERMISSIONS.files.read);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);
  readonly readComments = hasPermission(PERMISSIONS.comments.read);
  readonly canCreateComment = hasPermission(PERMISSIONS.comments.create);
  readonly readActivity = hasPermission(PERMISSIONS.activity.read);
  readonly readPages = hasPermission(PERMISSIONS.pages.read);
  readonly readSourceControl = hasPermission(PERMISSIONS.sourceControl.read);
  readonly showRail = computed(
    () => this.readComments() || this.readActivity() || this.showReview()
  );

  private readonly labels = {
    boards: $localize`:Section heading for the boards a task appears on:Boards`,
    links: $localize`:Tab listing the tasks this one links to:Links`,
    files: $localize`:Section heading for files attached to a task:Files`,
    pages: $localize`:Tab listing the pages that mention a task:Pages`,
    code: $localize`:Tab listing the branches, commits and pull requests linked to a task:Code`,
    comments: $localize`:Section heading for a task's comments:Comments`,
    history: $localize`:Tab showing what has happened to a task:History`,
    review: $localize`:Tab showing the assistant's review of a task:Review`,
  };

  tabItems(
    boards: number,
    links: number,
    files: number | null,
    pages: number | null,
    code: number
  ): TabItem[] {
    const tabs: TabItem[] = [
      { value: 'boards', label: this.labels.boards, count: boards },
      { value: 'links', label: this.labels.links, count: links },
    ];

    if (files !== null) {
      tabs.push({ value: 'files', label: this.labels.files, count: files });
    }

    if (pages !== null) {
      tabs.push({ value: 'pages', label: this.labels.pages, count: pages });
    }

    if (code) {
      tabs.push({ value: 'code', label: this.labels.code, count: code });
    }

    return tabs;
  }

  readonly showReview = computed(() => {
    return this.taskDetail.canAskAssistant() || this.review.hasReview();
  });

  railTabItems(comments: number, openFindings: number): TabItem[] {
    const tabs: TabItem[] = [];

    if (this.readComments()) {
      tabs.push({
        value: 'comments',
        label: this.labels.comments,
        count: comments,
      });
    }

    if (this.readActivity()) {
      tabs.push({ value: 'history', label: this.labels.history, count: null });
    }

    if (this.showReview()) {
      tabs.push({
        value: 'review',
        label: this.labels.review,
        count: openFindings,
      });
    }

    return tabs;
  }
}
