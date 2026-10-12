import { Component, input } from '@angular/core';
import { LucideLink } from '@lucide/angular';
import { BrandLogoComponent } from './brand-logo.component';
import { ProviderMarkComponent } from './provider-mark.component';

@Component({
  selector: 'app-provider-link-diagram',
  imports: [BrandLogoComponent, LucideLink, ProviderMarkComponent],
  host: { class: 'flex items-center gap-3', 'aria-hidden': 'true' },
  template: `
    <span
      class="border-border text-foreground rounded-dialog flex h-11 w-11 items-center justify-center border">
      <app-provider-mark [provider]="provider()" [size]="22" />
    </span>
    <span class="border-foreground/25 w-8 border-t border-dashed"></span>
    <svg lucideLink class="text-primary" size="16"></svg>
    <span class="border-foreground/25 w-8 border-t border-dashed"></span>
    <app-brand-logo size="large" />
  `,
})
export class ProviderLinkDiagramComponent {
  readonly provider = input.required<string>();
}
