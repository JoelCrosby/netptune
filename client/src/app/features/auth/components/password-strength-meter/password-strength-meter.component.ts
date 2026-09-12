import { Component, computed, input } from '@angular/core';

interface PasswordStrength {
  width: string;
  color: string;
  label: string;
  score: number;
}

const EMPTY_STRENGTH: PasswordStrength = {
  width: '0%',
  color: 'bg-primary',
  label: '',
  score: 0,
};

@Component({
  selector: 'app-password-strength-meter',
  template: `
    <div class="mt-0.5 flex items-center gap-2.5">
      <span
        class="bg-foreground/5 relative h-1 grow overflow-hidden rounded-full"
        role="progressbar"
        aria-valuemin="0"
        aria-valuemax="5"
        [attr.aria-valuenow]="strength().score"
        [attr.aria-valuetext]="strength().label || null"
        [attr.aria-label]="meterLabel">
        <span
          class="absolute inset-y-0 left-0 rounded-full transition-[width] duration-200"
          [class]="strength().color"
          [style.width]="strength().width"></span>
      </span>

      <span
        class="text-foreground/50 min-w-[54px] text-right text-xs font-semibold">
        {{ strength().label }}
      </span>
    </div>
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
      width: '33%',
      color: 'bg-warn',
      label: $localize`:Rating of a typed password:Weak`,
      score,
    };
  }

  if (score === 3) {
    return {
      width: '55%',
      color: 'bg-[#f9a825]',
      label: $localize`:Rating of a typed password:Fair`,
      score,
    };
  }

  if (score === 4) {
    return {
      width: '78%',
      color: 'bg-primary',
      label: $localize`:Rating of a typed password:Good`,
      score,
    };
  }

  return {
    width: '100%',
    color: 'bg-[#2e7d32]',
    label: $localize`:Rating of a typed password:Strong`,
    score,
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
