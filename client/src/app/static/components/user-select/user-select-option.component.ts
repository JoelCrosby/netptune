import { Component, computed, input, output } from '@angular/core';
import { UserSelectOption } from '@core/models/view-models/user-select-option';
import { LucideCheck } from '@lucide/angular';
import { AvatarComponent } from '../avatar/avatar.component';

@Component({
  selector: 'app-user-select-option',
  imports: [AvatarComponent, LucideCheck],
  template: `
    <button
      type="button"
      role="option"
      class="flex w-full cursor-pointer items-center gap-3 rounded-sm px-3 py-2 text-left text-sm transition-colors select-none hover:bg-neutral-100 focus-visible:outline-none dark:hover:bg-neutral-800"
      [id]="optionId()"
      [attr.aria-selected]="selected()"
      [class]="
        active() ? 'bg-neutral-100 dark:bg-neutral-800' : 'bg-transparent'
      "
      (click)="clicked.emit(option())">
      <app-avatar
        [imageUrl]="option().pictureUrl"
        [name]="option().displayName"
        [isServiceAccount]="option().isServiceAccount ?? false"
        size="sm" />

      <span class="flex min-w-0 flex-1 flex-col">
        <span class="truncate font-medium">{{ option().displayName }}</span>
        @if (option().email; as email) {
          <span class="text-muted truncate text-xs">{{ email }}</span>
        }
      </span>

      @if (selected()) {
        <svg lucideCheck class="text-primary h-4 w-4 shrink-0"></svg>
      }
    </button>
  `,
  host: { class: 'block' },
})
export class UserSelectOptionComponent {
  readonly option = input.required<UserSelectOption>();
  readonly active = input(false);
  readonly selected = input(false);
  readonly clicked = output<UserSelectOption>();

  readonly optionId = computed(() => userSelectOptionId(this.option().id));
}

export function userSelectOptionId(userId: string): string {
  return `user-select-option-${userId}`;
}
