import { Component, computed, input } from '@angular/core';
import {
  ProgressBarComponent,
  type ProgressBarColor,
} from '@static/components/progress-bar/progress-bar.component';

interface PasswordStrength {
  value: number;
  color: ProgressBarColor;
  label: string;
}

const EMPTY_STRENGTH: PasswordStrength = {
  value: 0,
  color: 'primary',
  label: '',
};

@Component({
  selector: 'app-password-strength-meter',
  imports: [ProgressBarComponent],
  host: { class: 'mt-0.5 flex items-center gap-2.5' },
  template: `
    <app-progress-bar
      class="grow"
      mode="determinate"
      [ariaLabel]="meterLabel"
      [valueText]="strength().label || null"
      [value]="strength().value"
      [color]="strength().color" />

    <span
      class="text-foreground/50 min-w-13.5 text-right text-xs font-semibold">
      {{ strength().label }}
    </span>
  `,
})
export class PasswordStrengthMeterComponent {
  readonly password = input('');

  protected readonly meterLabel = $localize`:Accessible label of the meter that rates a typed password:Password strength`;

  protected readonly strength = computed(() => {
    return rate(this.password());
  });
}

function rate(password: string): PasswordStrength {
  if (!password) return EMPTY_STRENGTH;

  const score = scorePassword(password);

  if (score <= 2) {
    return {
      value: 33,
      color: 'warn',
      label: $localize`:Rating of a typed password:Weak`,
    };
  }

  if (score === 3) {
    return {
      value: 55,
      color: 'caution',
      label: $localize`:Rating of a typed password:Fair`,
    };
  }

  if (score === 4) {
    return {
      value: 78,
      color: 'primary',
      label: $localize`:Rating of a typed password:Good`,
    };
  }

  return {
    value: 100,
    color: 'success',
    label: $localize`:Rating of a typed password:Strong`,
  };
}

function scorePassword(password: string): number {
  let score = 0;

  if (password.length >= 8) score++;
  if (password.length >= 12) score++;
  if (/[A-Z]/.test(password) && /[a-z]/.test(password)) score++;
  if (/\d/.test(password)) score++;
  if (/[^A-Za-z0-9]/.test(password)) score++;

  return score;
}
