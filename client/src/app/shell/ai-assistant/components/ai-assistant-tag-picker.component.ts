import { Component, computed, input, output } from '@angular/core';
import { tagResource } from '@core/resources/tag.resource';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';

@Component({
  selector: 'app-ai-assistant-tag-picker',
  imports: [FormSelectTagsComponent, FormSelectTagsOptionComponent],
  template: `
    <app-form-select-tags
      [label]="label()"
      [value]="values()"
      (changed)="valuesChange.emit($event)">
      @for (name of names(); track name) {
        <app-form-select-tags-option [value]="name">
          {{ name }}
        </app-form-select-tags-option>
      }
    </app-form-select-tags>
  `,
})
export class AiAssistantTagPickerComponent {
  readonly label = input.required<string>();
  readonly values = input<string[]>([]);
  readonly proposedNames = input<string[]>([]);

  readonly valuesChange = output<string[]>();

  private readonly tags = tagResource();

  protected readonly names = computed(() => {
    const workspaceNames = this.tags.value().map((tag) => tag.name);
    const all = [...workspaceNames, ...this.proposedNames(), ...this.values()];
    const unique = new Map(all.map((name) => [name.toLowerCase(), name]));

    return [...unique.values()].sort((left, right) =>
      left.localeCompare(right)
    );
  });
}
