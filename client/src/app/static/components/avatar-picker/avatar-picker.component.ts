import { Component, input, output } from '@angular/core';
import { LucideCamera } from '@lucide/angular';
import { AvatarComponent } from '../avatar/avatar.component';
import { IconButtonComponent } from '../button/icon-button.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { SpinnerComponent } from '../spinner/spinner.component';

@Component({
  selector: 'app-avatar-picker',
  imports: [
    AvatarComponent,
    DropdownMenuComponent,
    IconButtonComponent,
    LucideCamera,
    SpinnerComponent,
  ],
  host: { class: 'relative block h-24 w-24' },
  template: `
    <button
      type="button"
      class="group focus-visible:ring-primary relative block cursor-pointer rounded-full focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-default"
      [attr.aria-label]="pickLabel()"
      [disabled]="busy()"
      (click)="pick.emit()">
      <app-avatar
        size="xl"
        [name]="name()"
        [imageUrl]="imageUrl()"
        [tooltip]="false" />

      @if (busy()) {
        <span [class]="overlayClass">
          <app-spinner diameter="24px" />
        </span>
      } @else {
        <span
          class="opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100"
          [class]="overlayClass">
          <svg lucideCamera class="h-4.5 w-4.5" aria-hidden="true"></svg>
          <span class="text-[10px] font-bold tracking-widest uppercase">
            {{ overlayLabel() }}
          </span>
        </span>
      }
    </button>

    <button
      app-icon-button
      color="solid"
      size="xs"
      class="border-card absolute -right-1 -bottom-1 border-2"
      aria-haspopup="menu"
      [ariaLabel]="actionsLabel()"
      [attr.aria-expanded]="menu.showing()"
      [disabled]="busy()"
      (click)="menu.toggle($any($event.currentTarget))">
      <svg lucideCamera class="h-3.5 w-3.5" aria-hidden="true"></svg>
    </button>

    <app-dropdown-menu #menu>
      <div class="min-w-52" (click)="menu.close()">
        <ng-content select="[avatarPickerActions]" />
      </div>
    </app-dropdown-menu>
  `,
})
export class AvatarPickerComponent {
  readonly imageUrl = input<string | null | undefined>(null);
  readonly name = input<string | null | undefined>(null);
  readonly busy = input(false);
  readonly pickLabel = input.required<string>();
  readonly actionsLabel = input.required<string>();
  readonly overlayLabel = input.required<string>();

  readonly pick = output();

  protected readonly overlayClass =
    'absolute inset-0 flex flex-col items-center justify-center gap-1 rounded-full bg-black/60 text-white';
}
