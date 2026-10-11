import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { PageKind } from '@core/models/pages';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { PageCommandsService } from '@core/services/page-commands.service';
import { LucideSparkles } from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PromptInputComponent } from '@static/components/prompt-input/prompt-input.component';
import { PageTreeComponent } from '../../components/page-tree.component';
import { PageTreeStore } from '../../services/page-tree.store';

@Component({
  selector: 'app-pages-home-view',
  imports: [
    PageHeaderComponent,
    EmptyStateComponent,
    PageTreeComponent,
    PromptInputComponent,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col overflow-y-auto' },
  template: `
    <div class="px-8 pt-4 max-md:px-3">
      <app-page-header
        i18n-title="Title of the pages home"
        title="Pages"
        [count]="store.nodes().length"
        [actionTitle]="canCreate() ? createLabel : null"
        (actionClick)="onCreate()" />
    </div>

    <div class="flex flex-col gap-6 px-8 py-6 max-md:px-3">
      @if (canAsk()) {
        <app-prompt-input
          class="max-w-2xl"
          [icon]="askIcon"
          i18n-placeholder="
            Placeholder of the box on pages home that asks the assistant about
            the workspace's pages
          "
          placeholder="Ask anything about your pages…"
          i18n-ariaLabel="
            Accessible label of the box on pages home that asks the assistant a
            question
          "
          ariaLabel="Ask the assistant about your pages"
          i18n-submitLabel="
            Accessible label of the button that sends a question to the
            assistant
          "
          submitLabel="Ask"
          i18n-hint="
            Hint beside the pages home question box saying the assistant answers
            from pages
          "
          hint="Answers cite the pages they use"
          (submitted)="onAsk($event)" />
      }

      <app-page-tree class="md:hidden" />

      <app-empty-state
        class="max-md:hidden"
        outlined
        i18n-title="Shown on the pages home before a page is opened"
        title="Pick a page from the tree"
        i18n-description="Hint on the pages home"
        description="Or create one to start writing. Pages can be nested inside folders and other pages." />
    </div>
  `,
})
export class PagesHomeViewComponent {
  protected readonly store = inject(PageTreeStore);
  private readonly commands = inject(PageCommandsService);
  private readonly router = inject(Router);

  private readonly assistant = inject(AiAssistantService);
  private readonly canUseAssistant = hasPermission(PERMISSIONS.assistant.use);

  protected readonly canCreate = hasPermission(PERMISSIONS.pages.create);
  protected readonly askIcon = LucideSparkles;
  protected readonly canAsk = computed(() => {
    return this.assistant.isAvailable() && this.canUseAssistant();
  });
  protected readonly createLabel = $localize`:Button that creates a page:Create Page`;

  protected onAsk(question: string) {
    this.assistant.askAcrossPages(question);
  }

  protected onCreate() {
    this.commands.create({ kind: PageKind.document }).subscribe((page) => {
      void this.router.navigate(this.store.link(page.slug));
    });
  }
}
