import { Component, input, output } from '@angular/core';
import { SwitchComponent } from '@static/components/switch/switch.component';

@Component({
  selector: 'app-assistant-privacy-card',
  imports: [SwitchComponent],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <header class="border-border border-b px-6 py-5">
        <h2
          class="font-overpass text-base font-semibold"
          i18n="Heading of the assistant access and privacy card">
          Access &amp; privacy
        </h2>
      </header>

      <div
        class="border-border flex flex-wrap items-center justify-between gap-x-6 gap-y-3 border-b px-6 py-5">
        <div class="min-w-0">
          <h3
            class="text-sm font-medium"
            i18n="Heading of the assistant access setting">
            Assistant access
          </h3>
          <p
            class="text-muted mt-1 text-sm"
            i18n="Explains what turning the assistant off does">
            Turning this off stops new assistant messages and blocks pending
            changes from being applied.
          </p>
        </div>

        <app-switch
          class="shrink-0"
          [checked]="enabled()"
          i18n-ariaLabel="Toggle that enables the assistant for a workspace"
          ariaLabel="Allow members to use the assistant"
          (changed)="enabledChanged.emit($event)" />
      </div>

      <div
        class="flex flex-wrap items-center justify-between gap-x-6 gap-y-3 px-6 py-5">
        <div class="min-w-0">
          <h3
            class="text-sm font-medium"
            i18n="Heading of the assistant data sampling setting">
            Share example values with the assistant
          </h3>
          <p
            class="text-muted mt-1 text-sm"
            i18n="Explains what turning off assistant data sampling does">
            When an import mapping is improved by the assistant, a few real cell
            values are sent with the column names. Turn this off to send column
            names and types only.
          </p>
        </div>

        <app-switch
          class="shrink-0"
          [checked]="dataSampling()"
          i18n-ariaLabel="Toggle that shares example values with the assistant"
          ariaLabel="Share example values with the assistant"
          (changed)="dataSamplingChanged.emit($event)" />
      </div>
    </section>
  `,
})
export class AssistantPrivacyCardComponent {
  readonly enabled = input.required<boolean>();
  readonly dataSampling = input.required<boolean>();

  readonly enabledChanged = output<boolean>();
  readonly dataSamplingChanged = output<boolean>();
}
