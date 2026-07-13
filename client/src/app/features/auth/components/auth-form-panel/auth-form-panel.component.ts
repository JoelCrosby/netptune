import { booleanAttribute, Component, input, output } from '@angular/core';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';

@Component({
  selector: 'app-auth-form-panel',
  imports: [ProgressBarComponent],
  host: { class: 'z-1 block' },
  template: `<form
    class="bg-background border-border flex w-md flex-col gap-4 rounded border p-8 shadow-lg"
    [attr.aria-busy]="loading()"
    (submit)="onSubmit($event)">
    <div class="h-1">
      @if (loading()) {
        <app-progress-bar mode="indeterminate" />
      }
    </div>

    @if (showLogo()) {
      <img
        class="from-brand/40 mx-auto my-2 rounded-lg bg-linear-to-tl via-fuchsia-300/30 to-sky-300/30 p-2"
        src="assets/apple-touch-icon.png"
        alt="Netptune logo"
        width="72"
        height="72" />
    }

    <h1 class="mb-6 w-full text-center text-xl font-normal tracking-normal">
      {{ heading() }}
    </h1>

    <ng-content />
  </form>`,
})
export class AuthFormPanelComponent {
  readonly heading = input.required<string>();
  readonly loading = input(false, { transform: booleanAttribute });
  readonly showLogo = input(false, { transform: booleanAttribute });

  readonly submitted = output();

  onSubmit(event: SubmitEvent) {
    event.preventDefault();
    this.submitted.emit();
  }
}
