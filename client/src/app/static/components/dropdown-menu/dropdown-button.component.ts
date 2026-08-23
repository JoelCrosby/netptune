import { Component, computed, input, viewChild } from '@angular/core';
import { LucideChevronDown } from '@lucide/angular';
import {
  cn,
  flatButtonVariants,
  type FlatButtonColor,
} from '../button/button.variants';
import {
  DropdownMenuComponent,
  type DropdownMenuXPosition,
} from './dropdown-menu.component';

@Component({
  selector: 'app-dropdown-button',
  imports: [DropdownMenuComponent, LucideChevronDown],
  template: `
    <button
      #trigger
      type="button"
      [class]="className()"
      [disabled]="disabled()"
      aria-haspopup="menu"
      [attr.aria-label]="ariaLabel() || label()"
      (click)="menu.toggle(trigger)">
      <ng-content select="[buttonPrefix]" />
      <span class="truncate">{{ label() }}</span>
      <svg lucideChevronDown class="h-4 w-4 shrink-0 opacity-70"></svg>
    </button>

    <app-dropdown-menu
      #menu
      [xPosition]="xPosition()"
      [panelRole]="panelRole()">
      @if (menuLabel(); as menuLabel) {
        <p
          class="text-foreground/40 mx-2 mt-1.5 mb-2 text-[11px] font-semibold tracking-[0.06em] uppercase">
          {{ menuLabel }}
        </p>
      }

      <ng-content />
    </app-dropdown-menu>
  `,
})
export class DropdownButtonComponent {
  readonly label = input.required<string>();
  readonly ariaLabel = input<string>();
  readonly color = input<FlatButtonColor>('neutral');
  readonly disabled = input(false);
  readonly buttonClass = input('');
  readonly xPosition = input<DropdownMenuXPosition>('after');
  readonly panelRole = input('menu');
  /** Heading shown above the menu contents, for a panel whose trigger shows a value rather than a name. */
  readonly menuLabel = input<string>();

  private readonly menu = viewChild.required(DropdownMenuComponent);

  protected readonly className = computed(() =>
    cn(flatButtonVariants({ color: this.color() }), this.buttonClass())
  );

  close() {
    this.menu().close();
  }
}
