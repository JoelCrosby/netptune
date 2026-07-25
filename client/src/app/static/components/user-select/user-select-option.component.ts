import { Component, computed, input, output } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { AvatarComponent } from '../avatar/avatar.component';

@Component({
  selector: 'app-user-select-option',
  imports: [AvatarComponent],
  template: `
    <div
      role="option"
      class="my-0.5 flex h-9 cursor-pointer items-center gap-2 rounded-sm px-2 text-sm"
      [id]="optionId()"
      [attr.aria-selected]="selected()"
      [class]="
        active() || selected()
          ? 'bg-primary text-primary-foreground'
          : 'hover:bg-accent text-foreground'
      "
      (click)="clicked.emit(option())">
      <app-avatar
        [imageUrl]="option().pictureUrl"
        [name]="option().displayName"
        [isServiceAccount]="option().isServiceAccount ?? false"
        size="sm" />
      <span>{{ option().displayName }}</span>
    </div>
  `,
})
export class UserSelectOptionComponent {
  readonly option = input.required<AppUser>();
  readonly active = input(false);
  readonly selected = input(false);
  readonly clicked = output<AppUser>();

  readonly optionId = computed(() => userSelectOptionId(this.option().id));
}

export function userSelectOptionId(userId: string): string {
  return `user-select-option-${userId}`;
}
