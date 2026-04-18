import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { AvatarComponent } from '../avatar/avatar.component';

@Component({
  selector: 'app-user-select-option',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent],
  template: `
    <div
      class="my-0.5 flex h-9 cursor-pointer items-center gap-2 rounded-sm px-2 text-sm"
      [class]="
        active() || selected()
          ? 'bg-primary text-primary-foreground'
          : 'hover:bg-accent text-foreground'
      "
      (click)="clicked.emit(option())">
      <app-avatar
        [imageUrl]="option().pictureUrl"
        [name]="option().displayName"
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
}
