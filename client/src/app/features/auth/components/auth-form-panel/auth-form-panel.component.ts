import { booleanAttribute, Component, input, output } from '@angular/core';
import { type LucideIconInput } from '@lucide/angular';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelFooterComponent } from '@static/components/panel-footer.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';

@Component({
  selector: 'app-auth-form-panel',
  imports: [
    PanelComponent,
    PanelHeaderComponent,
    PanelBodyComponent,
    PanelFooterComponent,
    ProgressBarComponent,
  ],
  host: { class: 'relative z-1 block w-full max-w-115' },
  template: `
    <form
      app-panel
      surface="card"
      elevation="floating"
      class="relative"
      [attr.aria-busy]="loading()"
      (submit)="onSubmit($event)">
      @if (loading()) {
        <app-progress-bar
          class="absolute inset-x-0 top-0"
          mode="indeterminate"
          [rounded]="false" />
      }

      <app-panel-header
        density="comfortable"
        [eyebrow]="eyebrow()"
        [heading]="heading()"
        [icon]="icon()" />

      <app-panel-body class="flex flex-col gap-4">
        <ng-content />
      </app-panel-body>

      <ng-content select="[panelSection]" />

      <app-panel-footer
        tone="muted"
        class="flex items-center justify-between gap-3 empty:hidden">
        <ng-content select="[panelFootnote]" />
      </app-panel-footer>
    </form>
  `,
})
export class AuthFormPanelComponent {
  readonly heading = input.required<string>();
  readonly eyebrow = input('');
  readonly icon = input.required<LucideIconInput>();
  readonly loading = input(false, { transform: booleanAttribute });

  readonly submitted = output();

  onSubmit(event: SubmitEvent) {
    event.preventDefault();
    this.submitted.emit();
  }
}
