import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { AvatarComponent } from '../avatar/avatar.component';

@Component({
  selector: 'app-user-select-option',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent],
  template: `
    <div
      class="h-9 flex items-center gap-2 px-2 cursor-pointer text-sm rounded-sm my-0.5"
      [class]="
        active() || selected()
          ? 'bg-primary text-primary-foreground'
          : 'hover:bg-accent text-foreground'
      "
      (click)="clicked.emit(option())">
      <app-avatar [imageUrl]="option().pictureUrl" [name]="option().displayName" size="24" />
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
