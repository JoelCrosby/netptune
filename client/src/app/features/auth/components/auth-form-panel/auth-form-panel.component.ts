import { booleanAttribute, Component, input, output } from '@angular/core';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';

@Component({
  selector: 'app-auth-form-panel',
  imports: [ProgressBarComponent],
  host: { class: 'z-1 block w-full max-w-[27.5rem]' },
  template: `
    <form
      class="bg-card border-border relative w-full overflow-hidden rounded-xl border p-8 shadow-xs"
      [attr.aria-busy]="loading()"
      (submit)="onSubmit($event)">
      @if (loading()) {
        <app-progress-bar
          class="absolute inset-x-0 top-0"
          mode="indeterminate"
          [rounded]="false" />
      }

      @if (showLogo()) {
        <img
          class="mb-4.5 block h-11 w-11 rounded-[10px]"
          src="assets/android-chrome-192x192.png"
          i18n-alt="Alt text for the Netptune logo above auth forms"
          alt="Netptune logo"
          width="44"
          height="44" />
      }

      @if (eyebrow(); as eyebrow) {
        <p
          class="text-foreground/45 mb-0.5 text-xs font-semibold tracking-[.08em] uppercase">
          {{ eyebrow }}
        </p>
      }

      <ng-content select="[panelBadge]" />

      <h1 class="text-[1.625rem] font-bold tracking-[-.3px]">
        {{ heading() }}
      </h1>

      <ng-content select="[panelSubtitle]" />
      <ng-content />
    </form>
  `,
})
export class AuthFormPanelComponent {
  readonly heading = input.required<string>();
  readonly eyebrow = input<string | null>(null);
  readonly loading = input(false, { transform: booleanAttribute });
  readonly showLogo = input(false, { transform: booleanAttribute });

  readonly submitted = output();

  onSubmit(event: SubmitEvent) {
    event.preventDefault();
    this.submitted.emit();
  }
}
