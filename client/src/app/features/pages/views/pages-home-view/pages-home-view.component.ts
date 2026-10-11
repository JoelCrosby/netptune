import {
  Component,
  computed,
  ElementRef,
  inject,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  CreatePageRequest,
  PageKind,
  PageReviewReason,
} from '@core/models/pages';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { DialogService } from '@core/services/dialog.service';
import { PageCommandsService } from '@core/services/page-commands.service';
import { fromNow } from '@core/util/dates';
import {
  LucideDynamicIcon,
  LucideFileClock,
  LucideHistory,
  LucideLayoutTemplate,
  LucideSparkles,
  LucideUpload,
} from '@lucide/angular';
import { HeaderAction } from '@core/types/header-action';
import { ActionCardComponent } from '@static/components/action-card/action-card.component';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PromptInputComponent } from '@static/components/prompt-input/prompt-input.component';
import {
  RowListComponent,
  RowListItem,
} from '@static/components/row-list/row-list.component';
import { PageTreeComponent } from '../../components/page-tree.component';
import { SprintPickerDialogComponent } from '../../dialogs/sprint-picker-dialog.component';
import {
  pagesNeedingReviewResource,
  recentPagesResource,
} from '../../resources/pages.resource';
import { PageTreeStore } from '../../services/page-tree.store';
import { PageTemplateOption, pageTemplates } from '../../util/page-templates';

const REVIEW_REASONS: Record<PageReviewReason, string> = {
  [PageReviewReason.stale]: $localize`:Why a page is listed under needs review:Not reviewed in 6 months`,
  [PageReviewReason.unvisited]: $localize`:Why a page is listed under needs review:Nobody has opened it in 90 days`,
  [PageReviewReason.brokenLinks]: $localize`:Why a page is listed under needs review:Links to a deleted task or page`,
};

@Component({
  selector: 'app-pages-home-view',
  imports: [
    PageHeaderComponent,
    PageTreeComponent,
    PromptInputComponent,
    ActionCardComponent,
    ChartCardComponent,
    PageBodyComponent,
    RowListComponent,
    LucideDynamicIcon,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <app-page-header
      toolbar
      i18n-title="Title of the pages home"
      title="Pages"
      [count]="store.spaceNodes().length"
      [actionTitle]="canCreate() ? createLabel : null"
      [secondaryActions]="headerActions()"
      (actionClick)="onTemplate(blankTemplate)" />

    <input
      #importInput
      type="file"
      class="hidden"
      multiple
      accept=".md,.markdown,.txt,.zip"
      (change)="onImport(importInput)" />

    <app-page-body scroll>
      <div class="flex flex-col gap-8 pb-12">
        <app-page-tree class="md:hidden" />

        @if (canAsk()) {
          <app-chart-card
            [icon]="askIcon"
            i18n-title="Heading of the pages home card that asks the assistant"
            title="Ask your pages"
            i18n-description="
              Hint beside the pages home question box saying the assistant
              answers from pages
            "
            description="Answers cite the pages they use">
            <app-prompt-input
              i18n-placeholder="
                Placeholder of the box on pages home that asks the assistant
                about the workspace's pages
              "
              placeholder="Ask anything about your pages…"
              i18n-ariaLabel="
                Accessible label of the box on pages home that asks the
                assistant a question
              "
              ariaLabel="Ask the assistant about your pages"
              i18n-submitLabel="
                Accessible label of the button that sends a question to the
                assistant
              "
              submitLabel="Ask"
              (submitted)="onAsk($event)" />
          </app-chart-card>
        }

        @if (canCreate()) {
          <app-chart-card
            [icon]="templatesIcon"
            i18n-title="Heading over the templates on pages home"
            title="Start a page"
            i18n-description="Description of the pages home templates card"
            description="A template, release notes from a sprint, or a draft from the assistant">
            <div
              class="grid grid-cols-4 gap-2.5 max-lg:grid-cols-2 max-sm:grid-cols-1">
              @for (template of templates(); track template.key) {
                <app-action-card
                  [variant]="
                    template.key === 'assistant' ? 'accent' : 'outlined'
                  "
                  [heading]="template.title"
                  [showChevron]="false"
                  (activated)="onTemplate(template)">
                  <svg
                    actionCardIcon
                    [lucideIcon]="template.icon"
                    class="h-4 w-4"></svg>
                  {{ template.description }}
                </app-action-card>
              }
            </div>
          </app-chart-card>
        }

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <app-chart-card
            [flush]="true"
            [icon]="recentIcon"
            i18n-title="Heading over the pages the user opened recently"
            title="Recently viewed"
            i18n-description="
              Description of the pages home recently viewed card
            "
            description="Pages you opened lately">
            <app-row-list
              flush
              i18n-ariaLabel="
                Accessible label for the list of recently viewed pages
              "
              ariaLabel="Recently viewed pages"
              i18n-emptyLabel="
                Shown on pages home before the user has opened any page
              "
              emptyLabel="Pages you open appear here."
              [items]="recentItems()" />
          </app-chart-card>

          <app-chart-card
            [flush]="true"
            [icon]="reviewIcon"
            i18n-title="Heading over published pages that may be out of date"
            title="Needs review"
            i18n-description="Description of the pages home needs review card"
            description="Published pages that may be out of date">
            <app-row-list
              flush
              i18n-ariaLabel="
                Accessible label for the list of pages that need review
              "
              ariaLabel="Pages that need review"
              i18n-emptyLabel="Shown when no published page needs review"
              emptyLabel="Every published page is up to date."
              [items]="reviewItems()"
              (actioned)="onReviewed($event)" />
          </app-chart-card>
        </div>
      </div>
    </app-page-body>
  `,
})
export class PagesHomeViewComponent {
  protected readonly store = inject(PageTreeStore);
  private readonly commands = inject(PageCommandsService);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly assistant = inject(AiAssistantService);
  private readonly canUseAssistant = hasPermission(PERMISSIONS.assistant.use);
  private readonly canUpdate = hasPermission(PERMISSIONS.pages.update);
  private readonly recent = recentPagesResource();
  private readonly needingReview = pagesNeedingReviewResource();

  protected readonly canCreate = hasPermission(PERMISSIONS.pages.create);
  protected readonly createLabel = $localize`:Button that creates a page:Create Page`;
  protected readonly askIcon = LucideSparkles;
  protected readonly templatesIcon = LucideLayoutTemplate;
  protected readonly recentIcon = LucideHistory;
  protected readonly reviewIcon = LucideFileClock;
  protected readonly blankTemplate = pageTemplates[0];

  private readonly importInput =
    viewChild.required<ElementRef<HTMLInputElement>>('importInput');

  protected readonly headerActions = computed<HeaderAction[]>(() => {
    if (!this.canCreate()) return [];

    return [
      {
        label: importLabel,
        icon: LucideUpload,
        click: () => this.importInput().nativeElement.click(),
      },
    ];
  });

  protected readonly canAsk = computed(() => {
    return this.assistant.isAvailable() && this.canUseAssistant();
  });

  protected readonly templates = computed(() => {
    const canDraft = this.canAsk();

    return pageTemplates.filter(
      (template) => template.key !== 'assistant' || canDraft
    );
  });

  protected readonly recentItems = computed<RowListItem[]>(() => {
    return this.recent.value().map((page) => {
      return {
        id: page.id,
        title: page.title,
        subtitle: this.pathOf(page.id),
        meta: [page.updatedByDisplayName ?? '', fromNow(page.visitedAt)],
        link: this.store.link(page.slug),
      };
    });
  });

  protected readonly reviewItems = computed<RowListItem[]>(() => {
    const actionLabel = this.canUpdate() ? markReviewedLabel : null;

    return this.needingReview.value().map((page) => {
      return {
        id: page.id,
        title: page.title,
        subtitle: REVIEW_REASONS[page.reason],
        link: this.store.link(page.slug),
        actionLabel,
      };
    });
  });

  protected onAsk(question: string) {
    this.assistant.askAcrossPages(question);
  }

  protected async onTemplate(template: PageTemplateOption) {
    if (template.key === 'assistant') {
      this.assistant.draftPage();
      return;
    }

    const request = await this.requestFor(template);

    if (!request) return;

    this.commands.create(request).subscribe((page) => {
      void this.router.navigate(this.store.link(page.slug));
    });
  }

  protected onImport(input: HTMLInputElement) {
    const files = Array.from(input.files ?? []);

    input.value = '';

    if (!files.length) return;

    this.commands
      .importPages(files, this.store.activeSpaceId())
      .subscribe((result) => {
        if (result.firstSlug) {
          void this.router.navigate(this.store.link(result.firstSlug));
        }
      });
  }

  protected onReviewed(id: string | number) {
    this.commands.markReviewed(Number(id));
  }

  private async requestFor(
    template: PageTemplateOption
  ): Promise<CreatePageRequest | null> {
    const isFromSprint = template.key === 'from-sprint';

    const projectId = this.store.activeSpaceId();

    if (!isFromSprint) {
      return { kind: PageKind.document, templateKey: template.key, projectId };
    }

    const sprintId = await this.dialogs.openForResult<number>(
      SprintPickerDialogComponent,
      {
        width: SprintPickerDialogComponent.width,
      }
    );

    if (sprintId === undefined) return null;

    return {
      kind: PageKind.document,
      templateKey: template.key,
      sprintId,
      projectId,
    };
  }

  private pathOf(id: number): string | null {
    const ancestors = this.store.ancestorsOf(id);

    if (!ancestors.length) return null;

    return ancestors.map((node) => node.title).join(' / ');
  }
}

const markReviewedLabel = $localize`:Button that marks a page as still accurate so it leaves needs review:Mark reviewed`;

const importLabel = $localize`:Button on pages home that imports markdown files as pages:Import`;
