import { Component, computed, input } from '@angular/core';

export type BrandLogoSize = 'small' | 'large';

const sizeClasses: Record<BrandLogoSize, string> = {
  small: 'h-8 w-8 rounded-lg',
  large: 'h-11 w-11 rounded-[10px]',
};

const sizePixels: Record<BrandLogoSize, string> = {
  small: '32',
  large: '44',
};

@Component({
  selector: 'app-brand-logo',
  host: { class: 'block w-fit' },
  template: `
    <img
      class="block shrink-0"
      [class]="imageClass()"
      src="assets/android-chrome-192x192.png"
      i18n-alt="Alt text for the Netptune logo above auth forms"
      alt="Netptune logo"
      [width]="pixels()"
      [height]="pixels()" />
  `,
})
export class BrandLogoComponent {
  readonly size = input<BrandLogoSize>('large');

  protected readonly imageClass = computed(() => sizeClasses[this.size()]);
  protected readonly pixels = computed(() => sizePixels[this.size()]);
}
