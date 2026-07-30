import { Component, inject, input, output } from '@angular/core';
import { WorkspaceSetupTemplatesService } from '@core/services/workspace-setup-templates.service';
import {
  LucideCode2,
  LucideDynamicIcon,
  LucideFileText,
  LucideIconInput,
  LucideLayers3,
  LucideListChecks,
} from '@lucide/angular';
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';

@Component({
  selector: 'app-setup-template-picker',
  imports: [LucideDynamicIcon, SelectableCardComponent],
  host: { class: 'block' },
  template: `
    <fieldset class="min-w-0 border-0 p-0">
      <legend class="sr-only">
        <span i18n="Screen-reader legend for the workflow template chooser">
          Workflow template
        </span>
      </legend>

      @if (templates.loading()) {
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          @for (item of loadingItems; track item) {
            <div
              class="border-border bg-card h-32 animate-pulse rounded-xl border"></div>
          }
        </div>
      } @else if (templates.error()) {
        <div class="border-warn/30 bg-warn/5 rounded-xl border p-4">
          <p class="text-warn m-0 text-sm font-medium">
            <span i18n="Warning when workflow template previews fail to load">
              Template previews could not be loaded
            </span>
          </p>
          <p class="text-muted mt-1 mb-0 text-xs">
            <span
              i18n="Advice shown when workflow template previews fail to load">
              You can go back and retry, or continue with the recommended
              template.
            </span>
          </p>
        </div>
      } @else {
        <div
          class="grid min-w-0 grid-cols-1 gap-3 sm:grid-cols-2"
          role="radiogroup"
          i18n-aria-label="Accessible label for the workflow template chooser"
          aria-label="Workflow template">
          @for (template of templates.templates(); track template.key) {
            <app-selectable-card
              class="h-full"
              variant="feature"
              groupName="workflow-template"
              [accessibleLabel]="'Use the ' + template.name + ' template'"
              [heading]="template.name"
              [description]="template.description"
              [badge]="template.isRecommended ? 'Recommended' : ''"
              [selected]="selectedKey() === template.key"
              (selectionChange)="selectedKeyChange.emit(template.key)">
              <svg
                selectableCardIcon
                [lucideIcon]="templateIcon(template.key)"
                class="h-5 w-5"
                aria-hidden="true"></svg>
            </app-selectable-card>
          }
        </div>
      }
    </fieldset>
  `,
})
export class SetupTemplatePickerComponent {
  readonly templates = inject(WorkspaceSetupTemplatesService);
  readonly loadingItems = [1, 2, 3, 4];

  readonly selectedKey = input('basic');
  readonly selectedKeyChange = output<string>();

  private readonly icons: Record<string, LucideIconInput> = {
    basic: LucideLayers3,
    software: LucideCode2,
    content: LucideFileText,
    minimal: LucideListChecks,
  };

  templateIcon(key: string) {
    return this.icons[key] ?? LucideLayers3;
  }
}
