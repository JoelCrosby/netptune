import { Component, input } from '@angular/core';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FieldRowComponent } from '@static/components/field-row/field-row.component';
import { TaskFieldBase } from './field-label';

@Component({
  selector: 'app-task-reporter-field',
  imports: [AvatarComponent, FieldRowComponent],
  host: { class: 'block' },
  template: `
    <div app-field-row [label]="label" [labelWidth]="labelWidth()">
      <span class="flex min-w-0 items-center gap-1.5 font-medium">
        <app-avatar
          size="sm"
          [tooltip]="false"
          [name]="name()"
          [imageUrl]="pictureUrl()"
          [isServiceAccount]="isServiceAccount()" />
        <span class="truncate">{{ name() }}</span>
      </span>
    </div>
  `,
})
export class TaskReporterFieldComponent extends TaskFieldBase {
  readonly name = input.required<string>();
  readonly pictureUrl = input<string | null | undefined>(null);
  readonly isServiceAccount = input(false);

  protected readonly label = $localize`:Field heading for the person who raised the task:Reporter`;
}
