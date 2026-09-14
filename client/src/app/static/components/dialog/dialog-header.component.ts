import { booleanAttribute, Component, computed, input } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { DialogCloseDirective } from '../../directives/dialog-close.directive';
import { IconButtonComponent } from '../button/icon-button.component';
import { cn } from '../button/button.variants';

// The fixed top bar of a full-height dialog. Project `dialogHeaderActions` for
// buttons that sit before the close button.
@Component({
  selector: 'app-dialog-header, [app-dialog-header]',
  imports: [DialogCloseDirective, IconButtonComponent, LucideX],
  host: { '[class]': 'hostClass()' },
  template: `
    @if (heading(); as heading) {
      <h2 class="truncate text-[13px] font-semibold">{{ heading }}</h2>
    }

    <ng-content />

    <div class="ml-auto flex shrink-0 items-center gap-0.5">
      <ng-content select="[dialogHeaderActions]" />

      @if (showCloseButton()) {
        <button
          app-icon-button
          app-dialog-close
          size="small"
          color="muted"
          i18n-aria-label="Accessible label for the button that closes a dialog"
          aria-label="Close">
          <svg lucideX class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      }
    </div>
  `,
})
export class DialogHeaderComponent {
  readonly heading = input('');
  readonly showCloseButton = input(false, { transform: booleanAttribute });
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'border-foreground/8 flex h-[50px] shrink-0 items-center gap-2.5 border-b pr-3.5 pl-5',
      this.class()
    );
  });
}
