import { Component, input } from '@angular/core';
import { AvatarComponent } from '../avatar/avatar.component';

export interface AvatarStackItem {
  id: string;
  displayName: string;
  pictureUrl?: string | null;
}

@Component({
  selector: 'app-avatar-stack',
  imports: [AvatarComponent],
  host: {
    class: 'flex max-w-32 items-center -space-x-2 overflow-hidden',
  },
  template: `
    @for (avatar of avatars(); track avatar.id) {
      <app-avatar
        class="ring-card rounded-full ring-2"
        size="sm"
        [name]="avatar.displayName"
        [imageUrl]="avatar.pictureUrl" />
    }
  `,
})
export class AvatarStackComponent {
  readonly avatars = input.required<readonly AvatarStackItem[]>();
}
