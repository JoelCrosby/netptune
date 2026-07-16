import { Component, computed, input } from '@angular/core';
import { WorkspaceSetupTemplate } from '@core/models/workspace-setup-template';
import { LucideCheck } from '@lucide/angular';
import {
  SummaryListComponent,
  SummaryListItem,
} from '@static/components/summary-list/summary-list.component';

@Component({
  selector: 'app-setup-creation-summary',
  imports: [LucideCheck, SummaryListComponent],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card min-w-0 overflow-hidden rounded-xl border">
      <header class="flex items-center gap-4 p-5">
        <span
          class="bg-primary/10 text-primary flex h-10 w-10 shrink-0 items-center justify-center rounded-full">
          <svg lucideCheck class="h-5 w-5" aria-hidden="true"></svg>
        </span>
        <div class="min-w-0">
          <p
            class="text-primary m-0 text-xs font-semibold tracking-wider uppercase">
            Ready to create
          </p>
          <h4 class="text-foreground mt-1 mb-0 truncate text-lg font-semibold">
            {{ name() || 'Untitled ' + entityType().toLowerCase() }}
          </h4>
          <p class="text-muted mt-1 mb-0 text-sm">
            {{ entityType() }} using the {{ templateName() }} workflow
          </p>
        </div>
      </header>

      <app-summary-list [items]="summaryItems()" />
    </section>
  `,
})
export class SetupCreationSummaryComponent {
  readonly entityType = input.required<'Workspace' | 'Project'>();
  readonly name = input('');
  readonly secondaryLabel = input('Identifier');
  readonly secondaryValue = input('');
  readonly templateKey = input('basic');
  readonly template = input<WorkspaceSetupTemplate>();
  readonly showWorkspaceDefaults = input(false);

  readonly templateName = computed(
    () => this.template()?.name ?? this.templateKey()
  );
  readonly boardFlow = computed(() =>
    (this.template()?.boardGroups ?? []).join(' → ')
  );
  readonly summaryItems = computed<readonly SummaryListItem[]>(() => {
    const selectedTemplate = this.template();
    const secondaryValue = this.secondaryValue();
    const workspaceDefaults = selectedTemplate
      ? `${selectedTemplate.statuses.length} statuses · ${selectedTemplate.tags.length} tags · ${selectedTemplate.relationTypes.length} relations`
      : '';

    return [
      ...(secondaryValue
        ? [
            {
              label: this.secondaryLabel(),
              value: secondaryValue,
              truncate: true,
            },
          ]
        : []),
      {
        label: 'Board flow',
        value: this.boardFlow() || 'Default board',
      },
      ...(this.showWorkspaceDefaults() && workspaceDefaults
        ? [
            {
              label: 'Workspace defaults',
              value: workspaceDefaults,
              muted: true,
            },
          ]
        : []),
    ];
  });
}
