import { booleanAttribute, Component, computed, input } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { DialogCloseDirective } from '../../directives/dialog-close.directive';
import { IconButtonComponent } from '../button/icon-button.component';
import { cn } from '../button/button.variants';
import { dialogTitleId } from './dialog-title-id';

export type DialogHeaderDivider = 'none' | 'bottom';

// The fixed top bar of a full-height dialog. Project `dialogHeaderIcon` for a
// tile before the heading and `dialogHeaderActions` for buttons that sit before
// the close button.
@Component({
  selector: 'app-dialog-header, [app-dialog-header]',
  imports: [DialogCloseDirective, IconButtonComponent, LucideX],
  host: { '[class]': 'hostClass()' },
  template: `
    <ng-content select="[dialogHeaderIcon]" />

    @if (heading(); as heading) {
      @if (description(); as description) {
        <div class="flex min-w-0 flex-col">
          <h2 class="truncate text-[17px] font-bold" [id]="titleId">
            {{ heading }}
          </h2>
          <p class="text-muted truncate text-[12.5px]">{{ description }}</p>
        </div>
      } @else {
        <h2 class="truncate text-[13px] font-semibold" [id]="titleId">
          {{ heading }}
        </h2>
      }
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
  readonly description = input('');
  readonly divider = input<DialogHeaderDivider>('bottom');
  readonly showCloseButton = input(false, { transform: booleanAttribute });
  readonly class = input('');

  protected readonly titleId = dialogTitleId(() => !!this.heading());

  protected readonly hostClass = computed(() => {
    return cn(
      'border-foreground/8 flex shrink-0 items-center gap-2.5 pr-3.5 pl-5',
      this.description() ? 'gap-3 pt-4.5 pb-3.5' : 'h-[50px]',
      this.divider() === 'bottom' && 'border-b',
      this.class()
    );
  });
}
