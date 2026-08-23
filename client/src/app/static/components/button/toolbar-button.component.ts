import { Component, HostBinding, input } from '@angular/core';
import { cn, toolbarButtonVariants, type ButtonColor } from './button.variants';

// Compact icon-and-label button for the action bars that sit above or over a
// view, where every action stays visible rather than folding into a menu.
@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-toolbar-button]',
  template: '<ng-content />',
  host: { type: 'button' },
})
export class ToolbarButtonComponent {
  readonly color = input<ButtonColor>('neutral');
  readonly class = input('');

  @HostBinding('class') get className(): string {
    return cn(toolbarButtonVariants({ color: this.color() }), this.class());
  }
}
