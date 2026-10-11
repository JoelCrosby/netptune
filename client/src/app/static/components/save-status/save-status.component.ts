import { Component, computed, input } from '@angular/core';
import {
  LucideCheck,
  LucideCircleAlert,
  LucideLoaderCircle,
  LucideTriangleAlert,
} from '@lucide/angular';
import { cn } from '../button/button.variants';

export type SaveState = 'idle' | 'saving' | 'saved' | 'conflict' | 'error';

// Where an autosaving document stands, as a small inline label.
@Component({
  selector: 'app-save-status',
  imports: [
    LucideCheck,
    LucideCircleAlert,
    LucideLoaderCircle,
    LucideTriangleAlert,
  ],
  host: {
    '[class]': 'hostClass()',
    role: 'status',
  },
  template: `
    @switch (state()) {
      @case ('saving') {
        <svg lucideLoaderCircle class="h-3 w-3 animate-spin"></svg>
      }
      @case ('saved') {
        <svg lucideCheck class="text-change-added h-3 w-3"></svg>
      }
      @case ('conflict') {
        <svg lucideTriangleAlert class="h-3 w-3"></svg>
      }
      @case ('error') {
        <svg lucideCircleAlert class="h-3 w-3"></svg>
      }
    }
    <span>{{ label() }}</span>
  `,
})
export class SaveStatusComponent {
  readonly state = input.required<SaveState>();
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    const state = this.state();
    const isProblem = state === 'conflict' || state === 'error';

    return cn(
      'inline-flex items-center gap-1.5 text-xs whitespace-nowrap',
      isProblem ? 'text-warn' : 'text-foreground/45',
      state === 'idle' && 'invisible',
      this.class()
    );
  });

  protected readonly label = computed(() => {
    switch (this.state()) {
      case 'saving':
        return $localize`:Shown while a document is being saved:Saving…`;
      case 'saved':
        return $localize`:Shown once a document's latest changes are saved:Saved`;
      case 'conflict':
        return $localize`:Shown when a document was changed elsewhere before this save:Not saved`;
      case 'error':
        return $localize`:Shown when saving a document failed:Couldn't save`;
      default:
        return '';
    }
  });
}
