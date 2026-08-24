import {
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';
import {
  LucideChevronDown,
  LucideDynamicIcon,
  type LucideIconInput,
} from '@lucide/angular';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';

@Component({
  selector: 'app-split-button',
  imports: [DropdownMenuComponent, LucideChevronDown, LucideDynamicIcon],
  host: { class: 'inline-flex' },
  template: `
    <span
      class="inline-flex h-8 items-stretch overflow-hidden rounded-lg border"
      [class]="shellClass()">
      <button
        type="button"
        class="hover:bg-foreground/5 inline-flex cursor-pointer items-center gap-1.5 px-2.5 text-[13px] font-medium transition-colors"
        [attr.aria-pressed]="pressed()"
        [title]="label()"
        (click)="activated.emit()">
        <svg
          [lucideIcon]="icon()"
          class="h-3.5 w-3.5"
          [class.fill-current]="iconFilled()"></svg>
        <span>{{ label() }}</span>
      </button>

      <span class="w-px" [class]="separatorClass()" aria-hidden="true"></span>

      <button
        #caret
        type="button"
        class="hover:bg-foreground/5 inline-flex cursor-pointer items-center px-1.75 transition-colors"
        aria-haspopup="menu"
        [attr.aria-label]="menuLabel()"
        [title]="menuLabel()"
        (click)="menu.toggle(caret)">
        <svg lucideChevronDown class="h-3.25 w-3.25"></svg>
      </button>
    </span>

    <app-dropdown-menu #menu xPosition="before">
      <ng-content />
    </app-dropdown-menu>
  `,
})
export class SplitButtonComponent {
  readonly label = input.required<string>();
  readonly icon = input.required<LucideIconInput>();
  readonly menuLabel = input.required<string>();
  readonly iconFilled = input(false);
  readonly pressed = input(false);

  readonly activated = output();

  private readonly menu = viewChild.required(DropdownMenuComponent);
  private readonly caret = viewChild.required<ElementRef<HTMLElement>>('caret');

  protected readonly shellClass = computed(() => {
    if (this.pressed()) {
      return 'border-primary/40 bg-primary/12 text-primary';
    }

    return 'border-border text-foreground/80';
  });

  protected readonly separatorClass = computed(() => {
    return this.pressed() ? 'bg-primary/30' : 'bg-border';
  });

  openMenu() {
    this.menu().open(this.caret().nativeElement);
  }

  closeMenu() {
    this.menu().close();
  }
}
