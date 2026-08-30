import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { LucideChevronDown, LucideSparkles } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
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
import { TaskDetailFieldRowsComponent } from '../shared/task-detail-field-rows.component';
import { TaskDetailPropertyChipsComponent } from '../shared/task-detail-property-chips.component';
import { TaskDetailTagRowComponent } from '../shared/task-detail-tag-row.component';
import {
  TaskDetailTab,
  TaskDetailTabsComponent,
} from '../shared/task-detail-tabs.component';
import { EYEBROW, META_CHIP } from '../task-detail-styles';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-document',
  imports: [
    DropdownMenuComponent,
    LucideChevronDown,
    LucideSparkles,
    TaskDetailChromeComponent,
    TaskDetailHeaderComponent,
    TaskDetailPropertyChipsComponent,
    TaskDetailTagRowComponent,
    TaskDetailFlagsComponent,
    TaskDetailDescriptionComponent,
    TaskDetailTabsComponent,
    TaskDetailFieldRowsComponent,
    TaskDetailBoardsComponent,
    TaskDetailRelationsComponent,
    TaskDetailFilesComponent,
    TaskDetailCommentsComponent,
    TaskDetailComposerComponent,
    TaskDetailActivityFeedComponent,
  ],
  providers: [TaskDetailCommentsService],
  host: { class: 'flex h-full min-h-0 flex-col' },
  template: `
    @if (task(); as task) {
      <app-task-detail-chrome class="h-12" [showBreadcrumb]="false" />

      <div
        class="custom-scroll flex min-h-0 flex-1 justify-center overflow-y-auto pt-2">
        <div class="flex w-[680px] max-w-full flex-col gap-[22px] px-4 pb-7">
          <app-task-detail-header
            textClass="-mx-2 px-2 py-1 text-[34px]/[42px] font-semibold tracking-[-0.015em]" />

          <div class="text-muted flex flex-wrap items-center gap-2 text-[13px]">
            <app-task-detail-property-chips variant="meta" />

            <span aria-hidden="true">·</span>

            <button
              #allFields
              type="button"
              [class]="allFieldsClass"
              aria-haspopup="menu"
              [attr.aria-expanded]="fieldsMenu.showing()"
              (click)="fieldsMenu.toggle(allFields)">
              <span i18n="Opens the full list of a task's fields">
                All fields
              </span>
              <svg lucideChevronDown class="h-3 w-3"></svg>
            </button>

            <app-dropdown-menu #fieldsMenu panelClass="py-2" xPosition="before">
              <div class="w-80">
                <div [class]="popoverEyebrowClass">
                  <span i18n="Eyebrow above the full list of a task's fields">
                    All fields
                  </span>
                </div>

                <app-task-detail-field-rows labelWidth="w-27" />

                @if (canAskAssistant()) {
                  <div
                    class="bg-foreground/8 my-2 h-px"
                    aria-hidden="true"></div>
                  <button
                    type="button"
                    class="text-primary hover:bg-hover flex h-8.5 w-full cursor-pointer items-center gap-2 px-3.5 text-left text-[13px] font-medium transition-colors"
                    (click)="askAssistant(); fieldsMenu.close()">
                    <svg lucideSparkles class="h-3.5 w-3.5"></svg>
                    <span i18n="Button that asks the assistant about this task">
                      Ask the assistant
                    </span>
                  </button>
                }
              </div>
            </app-dropdown-menu>
          </div>

          @if (readTags()) {
            <app-task-detail-tag-row />
          }

          @if (readFlags()) {
            <app-task-detail-flags />
          }

          <app-task-detail-description textClass="text-[16px]/[28px]" />

          <div class="bg-foreground/8 h-px" aria-hidden="true"></div>

          <app-task-detail-tabs
            class="gap-4.5"
            role="tablist"
            [tabs]="
              tabItems(
                comments.count(),
                filesSection()?.count() ?? null,
                relations.count(),
                task.placements.length
              )
            "
            [(active)]="activeTab" />

          @if (readComments()) {
            <div [class.hidden]="activeTab() !== 'comments'">
              <app-task-detail-comments density="comfortable" />
            </div>
          }
          @if (readFiles()) {
            <div [class.hidden]="activeTab() !== 'files'">
              <app-task-detail-files />
            </div>
          }
          <div [class.hidden]="activeTab() !== 'links'">
            <app-task-detail-relations #relations />
          </div>
          <div [class.hidden]="activeTab() !== 'boards'">
            <app-task-detail-boards />
          </div>
          @if (readActivity()) {
            <div [class.hidden]="activeTab() !== 'history'">
              <app-task-detail-activity-feed
                [enabled]="activeTab() === 'history'" />
            </div>
          }
        </div>
      </div>

      @if (readComments()) {
        <div
          class="border-foreground/8 flex shrink-0 justify-center border-t py-3">
          <app-task-detail-composer
            class="w-[680px] max-w-full px-4"
            shape="pill" />
        </div>
      }
    }
  `,
})
export class TaskDetailDocumentComponent {
  readonly taskDetail = inject(TaskDetailService);
  readonly comments = inject(TaskDetailCommentsService);

  private readonly assistant = inject(AiAssistantService);

  protected readonly filesSection = viewChild(TaskDetailFilesComponent);

  readonly task = this.taskDetail.task;
  readonly activeTab = signal('comments');

  readonly popoverEyebrowClass = `${EYEBROW} px-3.5 pt-1.5 pb-2`;
  readonly allFieldsClass = `${META_CHIP} border-primary/45 text-primary border font-semibold`;

  readonly readTags = hasPermission(PERMISSIONS.tags.read);
  readonly readFiles = hasPermission(PERMISSIONS.files.read);
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);
  readonly readComments = hasPermission(PERMISSIONS.comments.read);
  readonly readActivity = hasPermission(PERMISSIONS.activity.read);

  readonly canAskAssistant = computed(() => {
    return this.assistant.isAvailable() && this.task() !== null;
  });

  private readonly labels = {
    comments: $localize`:Section heading for a task's comments:Comments`,
    files: $localize`:Section heading for files attached to a task:Files`,
    links: $localize`:Tab listing the tasks this one links to:Links`,
    boards: $localize`:Section heading for the boards a task appears on:Boards`,
    history: $localize`:Tab showing what has happened to a task:History`,
  };

  tabItems(
    comments: number,
    files: number | null,
    links: number,
    boards: number
  ): TaskDetailTab[] {
    const tabs: TaskDetailTab[] = [];

    if (this.readComments()) {
      tabs.push({
        key: 'comments',
        label: this.labels.comments,
        count: comments,
      });
    }

    if (files !== null) {
      tabs.push({ key: 'files', label: this.labels.files, count: files });
    }

    tabs.push({ key: 'links', label: this.labels.links, count: links });
    tabs.push({ key: 'boards', label: this.labels.boards, count: boards });

    if (this.readActivity()) {
      tabs.push({ key: 'history', label: this.labels.history, count: null });
    }

    return tabs;
  }

  askAssistant() {
    const task = this.task();

    if (!task) return;

    this.assistant.askAboutTask(task);
  }
}
