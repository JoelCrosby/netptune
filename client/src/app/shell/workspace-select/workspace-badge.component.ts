import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';

@Component({
  selector: 'app-workspace-badge',
  host: {
    class:
      'h-7 min-w-7 w-7 flex items-center justify-center rounded-[.12rem] transition-opacity duration-200 text-white text-sm',
  },
  template: `{{ letter() }}`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceBadgeComponent {
  readonly color = input<string | null | undefined>(null);
  readonly letter = input.required<string>();

  @HostBinding('style.backgroundColor') get bgColor() {
    return this.color();
  }
}
