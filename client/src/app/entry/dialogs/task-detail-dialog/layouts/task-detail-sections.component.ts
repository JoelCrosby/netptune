import {
  Component,
  computed,
  inject,
  linkedSignal,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  LucideAlignLeft,
  LucideFileText,
  LucideGitBranch,
  LucideHistory,
  LucideKanban,
  LucideLink,
  LucideMessageSquare,
  LucidePaperclip,
  LucideSparkles,
} from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DividerComponent } from '@static/components/divider/divider.component';
import {
  SectionNavComponent,
  type SectionNavGroup,
} from '@static/components/section-nav/section-nav.component';
import { TaskDetailBoardsComponent } from '../task-detail-boards.component';
import { TaskDetailCodeComponent } from '../task-detail-code.component';
import { TaskDetailCommentsComponent } from '../task-detail-comments.component';
import { TaskDetailDescriptionComponent } from '../task-detail-description.component';
import { TaskDetailFilesComponent } from '../task-detail-files.component';
import { TaskDetailFlagsComponent } from '../task-detail-flags.component';
import { TaskDetailHeaderComponent } from '../task-detail-header.component';
import { TaskDetailPagesComponent } from '../task-detail-pages.component';
import { TaskDetailRelationsComponent } from '../task-detail-relations.component';
import { TaskDetailReviewComponent } from '../task-detail-review.component';
import { TaskDetailActivityFeedComponent } from '../shared/task-detail-activity-feed.component';
import { TaskDetailChromeComponent } from '../shared/task-detail-chrome.component';
import { TaskDetailCommentsService } from '../shared/task-detail-comments.service';
import { TaskDetailComposerComponent } from '../shared/task-detail-composer.component';
import { TaskDetailFieldRowsComponent } from '../shared/task-detail-field-rows.component';
import { TaskDetailPropertyChipsComponent } from '../shared/task-detail-property-chips.component';
import { TaskDetailReviewOriginComponent } from '../shared/task-detail-review-origin.component';
import { TaskDetailReviewService } from '../shared/task-detail-review.service';
import { TaskDetailTagRowComponent } from '../shared/task-detail-tag-row.component';
import { TaskDetailTimestampsComponent } from '../shared/task-detail-timestamps.component';
import { TaskDetailService } from '../task-detail.service';

// The fields the persistent chip row above the nav does not already carry.
const OVERVIEW_FIELDS = ['reporter', 'startDate', 'project', 'sprint'] as const;

@Component({
  selector: 'app-task-detail-sections',
  imports: [
    DividerComponent,
    SectionNavComponent,
    StrokedButtonComponent,
    TaskDetailActivityFeedComponent,
    TaskDetailBoardsComponent,
    TaskDetailChromeComponent,
    TaskDetailCodeComponent,
    TaskDetailCommentsComponent,
    TaskDetailComposerComponent,
    TaskDetailDescriptionComponent,
    TaskDetailFieldRowsComponent,
    TaskDetailFilesComponent,
    TaskDetailFlagsComponent,
    TaskDetailHeaderComponent,
    TaskDetailPagesComponent,
    TaskDetailPropertyChipsComponent,
    TaskDetailRelationsComponent,
    TaskDetailReviewComponent,
    TaskDetailReviewOriginComponent,
    TaskDetailTagRowComponent,
    TaskDetailTimestampsComponent,
  ],
  providers: [TaskDetailCommentsService, TaskDetailReviewService],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    @if (task(); as task) {
      <app-task-detail-chrome
        class="border-foreground/8 h-[52px] border-b"
        [showActivity]="false" />

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

      <div class="flex min-h-0 flex-1">
        <app-section-nav
          class="border-foreground/8 custom-scroll w-[206px] shrink-0 overflow-y-auto border-r p-2.5"
          i18n-ariaLabel="
            Accessible name of the list that moves between a task's sections
          "
          ariaLabel="Task sections"
          [groups]="
            navGroups(
              task.placements.length,
              relations.count(),
              filesSection()?.count() ?? null,
              pagesSection()?.count() ?? null,
              codeCount(),
              comments.count(),
              review.openCount()
            )
          "
          [(value)]="section" />

        <div class="flex min-w-0 flex-1 flex-col">
          <div
            class="border-foreground/8 flex h-[46px] shrink-0 items-center gap-3 border-b px-6">
            <h3 class="text-[13px] font-semibold">{{ sectionLabel() }}</h3>

            <div class="ml-auto">
              @if (section() === 'boards' && boards.canAdd()) {
                <button
                  app-stroked-button
                  type="button"
                  size="small"
                  (click)="onAddToBoard(boards, $event)">
                  <span i18n="Button that puts this task on another board">
                    Add to board
                  </span>
                </button>
              }
              @if (section() === 'links' && canUpdate()) {
                <button
                  app-stroked-button
                  type="button"
                  size="small"
                  (click)="relations.openLinkDialog()">
                  <span i18n="Button that links this task to another">
                    Link task
                  </span>
                </button>
              }
            </div>
          </div>

          <div class="custom-scroll min-h-0 flex-1 overflow-y-auto px-6 py-5">
            <div [class.hidden]="section() !== 'overview'">
              <app-task-detail-description
                [label]="descriptionLabel"
                textClass="text-[15px]/[26px]" />

              <div class="border-foreground/8 mt-7 max-w-128 border-t pt-3">
                <app-task-detail-field-rows
                  [fields]="overviewFields"
                  labelWidth="w-24" />
              </div>
            </div>

            @if (readComments()) {
              <div [class.hidden]="section() !== 'discussion'">
                <app-task-detail-comments density="comfortable" />
              </div>
            }

            @if (showReview() && section() === 'review') {
              <app-task-detail-review />
            }

            @if (readActivity()) {
              <div [class.hidden]="section() !== 'history'">
                <app-task-detail-activity-feed
                  [enabled]="section() === 'history'" />
              </div>
            }

            <div [class.hidden]="section() !== 'boards'">
              <app-task-detail-boards #boards />
            </div>

            <div [class.hidden]="section() !== 'links'">
              <app-task-detail-relations #relations />
            </div>

            @if (readFiles()) {
              <div [class.hidden]="section() !== 'files'">
                <app-task-detail-files />
              </div>
            }

            @if (readPages()) {
              <div [class.hidden]="section() !== 'pages'">
                <app-task-detail-pages />
              </div>
            }

            @if (readSourceControl()) {
              <div [class.hidden]="section() !== 'code'">
                <app-task-detail-code />
              </div>
            }
          </div>

          @if (section() === 'discussion' && canCreateComment()) {
            <app-task-detail-composer
              class="border-foreground/8 shrink-0 border-t px-6 py-3"
              shape="pill" />
          }
        </div>
      </div>

      <app-task-detail-timestamps
        class="border-foreground/8 h-10 shrink-0 border-t px-5"
        [showReporter]="true" />
    }
  `,
})
export class TaskDetailSectionsComponent {
  readonly taskDetail = inject(TaskDetailService);
  readonly comments = inject(TaskDetailCommentsService);
  readonly review = inject(TaskDetailReviewService);

  protected readonly filesSection = viewChild(TaskDetailFilesComponent);
  protected readonly pagesSection = viewChild(TaskDetailPagesComponent);
  protected readonly codeSection = viewChild(TaskDetailCodeComponent);

  readonly task = this.taskDetail.task;

  protected readonly codeCount = computed(() => {
    return this.codeSection()?.count() ?? 0;
  });

  readonly section = linkedSignal<number, string>({
    source: this.codeCount,
    computation: (count, previous) => {
      const section = previous?.value ?? 'overview';
      const isHiddenCodeSection = section === 'code' && !count;

      return isHiddenCodeSection ? 'overview' : section;
    },
  });

  readonly overviewFields = [...OVERVIEW_FIELDS];
  readonly descriptionLabel = $localize`:Eyebrow above the task description:Description`;

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readTags = hasPermission(PERMISSIONS.tags.read);
  readonly readFiles = hasPermission(PERMISSIONS.files.read);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);
  readonly readComments = hasPermission(PERMISSIONS.comments.read);
  readonly canCreateComment = hasPermission(PERMISSIONS.comments.create);
  readonly readActivity = hasPermission(PERMISSIONS.activity.read);
  readonly readPages = hasPermission(PERMISSIONS.pages.read);
  readonly readSourceControl = hasPermission(PERMISSIONS.sourceControl.read);

  readonly showReview = computed(() => {
    return this.taskDetail.canAskAssistant() || this.review.hasReview();
  });

  private readonly labels: Record<string, string> = {
    overview: $localize`:Section holding a task's description and remaining fields:Overview`,
    discussion: $localize`:Section holding a task's comments:Discussion`,
    history: $localize`:Tab showing what has happened to a task:History`,
    review: $localize`:Tab showing the assistant's review of a task:Review`,
    boards: $localize`:Section heading for the boards a task appears on:Boards`,
    links: $localize`:Tab listing the tasks this one links to:Links`,
    files: $localize`:Section heading for files attached to a task:Files`,
    pages: $localize`:Tab listing the pages that mention a task:Pages`,
    code: $localize`:Tab listing the branches, commits and pull requests linked to a task:Code`,
  };

  private readonly attachedLabel = $localize`:Heading above the sections listing what is attached to a task:Attached`;

  readonly sectionLabel = computed(() => this.labels[this.section()]);

  protected onAddToBoard(boards: TaskDetailBoardsComponent, event: MouseEvent) {
    boards.openAddMenu(event.currentTarget as HTMLElement);
  }

  navGroups(
    boards: number,
    links: number,
    files: number | null,
    pages: number | null,
    code: number,
    comments: number,
    openFindings: number
  ): SectionNavGroup[] {
    const task: SectionNavGroup = {
      key: 'task',
      items: [
        {
          value: 'overview',
          label: this.labels.overview,
          icon: LucideAlignLeft,
        },
      ],
    };

    if (this.readComments()) {
      task.items.push({
        value: 'discussion',
        label: this.labels.discussion,
        icon: LucideMessageSquare,
        count: comments || null,
      });
    }

    if (this.showReview()) {
      task.items.push({
        value: 'review',
        label: this.labels.review,
        icon: LucideSparkles,
        count: openFindings || null,
      });
    }

    if (this.readActivity()) {
      task.items.push({
        value: 'history',
        label: this.labels.history,
        icon: LucideHistory,
      });
    }

    const attached: SectionNavGroup = {
      key: 'attached',
      label: this.attachedLabel,
      items: [
        {
          value: 'boards',
          label: this.labels.boards,
          icon: LucideKanban,
          count: boards,
        },
        {
          value: 'links',
          label: this.labels.links,
          icon: LucideLink,
          count: links,
        },
      ],
    };

    if (files !== null) {
      attached.items.push({
        value: 'files',
        label: this.labels.files,
        icon: LucidePaperclip,
        count: files,
      });
    }

    if (pages !== null) {
      attached.items.push({
        value: 'pages',
        label: this.labels.pages,
        icon: LucideFileText,
        count: pages,
      });
    }

    if (code) {
      attached.items.push({
        value: 'code',
        label: this.labels.code,
        icon: LucideGitBranch,
        count: code,
      });
    }

    return [task, attached];
  }
}
