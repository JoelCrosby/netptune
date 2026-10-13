import { Component } from '@angular/core';
import { LucideUserRound } from '@lucide/angular';

@Component({
  selector: 'app-avatar-placeholder',
  imports: [LucideUserRound],
  host: {
    class:
      'border-foreground/25 text-muted flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-[1.5px] border-dashed',
  },
  template: `<svg
    lucideUserRound
    class="h-3.5 w-3.5"
    aria-hidden="true"></svg>`,
})
export class AvatarPlaceholderComponent {}
