import { Component, computed, input, output } from '@angular/core';
import { boardGroupOptionsResource } from '@core/resources/board-group.resource';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-ai-assistant-board-group-picker',
  imports: [FormSelectComponent, FormSelectOptionComponent],
  template: `
    <app-form-select
      [label]="label()"
      [noMargin]="true"
      [value]="value()"
      (changed)="valueChange.emit($event)">
      @if (isClearable()) {
        <app-form-select-option [value]="null">
          <span i18n="Option that uses the project default">
            Project default
          </span>
        </app-form-select-option>
      }
      @for (option of options(); track option.id) {
        <app-form-select-option [value]="option.id">
          {{ option.name }}
        </app-form-select-option>
      }
    </app-form-select>
  `,
})
export class AiAssistantBoardGroupPickerComponent {
  readonly label = input.required<string>();
  readonly value = input<string | null>(null);
  readonly projectId = input<number | null>(null);
  readonly isClearable = input(false);

  readonly valueChange = output<string | null>();

  private readonly boardGroups = boardGroupOptionsResource();

  protected readonly options = computed(() => {
    const projectId = this.projectId();
    const isAcrossProjects = projectId === null;
    const groups = this.boardGroups.value().filter((group) => {
      return isAcrossProjects || group.projectId === projectId;
    });

    return groups.map((group) => {
      const name = `${group.boardName} · ${group.name}`;

      return {
        id: String(group.id),
        name: isAcrossProjects ? `${group.projectName} · ${name}` : name,
      };
    });
  });
}
