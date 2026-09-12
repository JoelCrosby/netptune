import {
  booleanAttribute,
  Component,
  computed,
  input,
  signal,
} from '@angular/core';
import { LucideDynamicIcon, LucideEye, LucideEyeOff } from '@lucide/angular';
import { AbstractFormValueControl } from '@static/components/abstract-form-value-control';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import {
  FormControlInputDirective,
  FormControlLabelDirective,
} from '@static/components/form-control/form-control.directives';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';

@Component({
  selector: 'app-auth-field',
  imports: [
    LucideDynamicIcon,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormControlLabelDirective,
    FormErrorComponent,
  ],
  template: `
    <div class="flex w-full flex-col gap-1.5">
      <div class="flex items-baseline justify-between gap-3">
        <label
          class="text-foreground/70 mb-0 text-[13px] font-semibold"
          appFormLabel
          [for]="name()">
          {{ label() }}
        </label>

        <ng-content select="[fieldAction]" />
      </div>

      <app-form-control-field
        class="h-11 rounded-lg"
        density="compact"
        [disabled]="disabled()"
        [invalid]="touched() && invalid()">
        <input
          class="px-3.5 leading-none"
          appFormInput
          [id]="name()"
          [value]="value()"
          [disabled]="disabled()"
          [required]="required()"
          [attr.maxLength]="maxLength()"
          [attr.type]="inputType()"
          [attr.autocomplete]="autocomplete()"
          [attr.placeholder]="placeholder()"
          [attr.aria-invalid]="ariaInvalid()"
          [attr.aria-describedby]="describedBy(false)"
          (input)="onInputChange($event)"
          (blur)="touched.set(true)" />

        @if (revealable()) {
          <button
            class="text-foreground/50 hover:bg-hover hover:text-foreground mr-1.5 flex h-8 w-8 shrink-0 cursor-pointer items-center justify-center rounded-lg transition-colors"
            type="button"
            [attr.aria-label]="revealLabel()"
            [attr.title]="revealLabel()"
            (click)="toggleReveal()">
            <svg
              [lucideIcon]="revealed() ? lucideEyeOff : lucideEye"
              size="16"
              aria-hidden="true"></svg>
          </button>
        }
      </app-form-control-field>

      @if (showErrors()) {
        <div [id]="errorId()">
          @for (error of errors(); track error.kind) {
            <app-form-error>
              {{ error.message }}
            </app-form-error>
          }
        </div>
      }

      <ng-content />
    </div>
  `,
})
export class AuthFieldComponent extends AbstractFormValueControl {
  readonly label = input<string>();
  readonly autocomplete = input('off');
  readonly placeholder = input<string | null>();
  readonly maxLength = input<string | number | null>();
  readonly revealable = input(false, { transform: booleanAttribute });
  readonly type = input<'text' | 'email' | 'password'>('text');

  protected readonly lucideEye = LucideEye;
  protected readonly lucideEyeOff = LucideEyeOff;

  protected readonly revealed = signal(false);

  protected readonly inputType = computed(() => {
    return this.revealed() ? 'text' : this.type();
  });

  protected readonly revealLabel = computed(() => {
    if (this.revealed()) {
      return $localize`:Label of the button that hides a typed password again:Hide password`;
    }

    return $localize`:Label of the button that shows a typed password:Show password`;
  });

  toggleReveal() {
    this.revealed.update((revealed) => !revealed);
  }

  onInputChange(event: Event) {
    const target = event.target as HTMLInputElement;

    this.value.set(target.value);
  }
}
