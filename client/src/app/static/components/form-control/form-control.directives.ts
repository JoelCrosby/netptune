import { computed, Directive, input } from '@angular/core';

export type FormControlDensity = 'default' | 'compact';

@Directive({
  selector: 'input[appFormInput], textarea[appFormInput], select[appFormInput]',
  host: {
    class:
      'w-full appearance-none border-0 bg-transparent leading-10 text-inherit outline-none [font-family:inherit] [font-size:inherit] [font-weight:inherit] placeholder:opacity-60 disabled:bg-foreground/[0.02] disabled:text-foreground/40',
  },
})
export class FormControlInputDirective {}

@Directive({
  selector: '[appFormLabel]',
  host: {
    '[class]': 'hostClass()',
  },
})
export class FormControlLabelDirective {
  readonly variant = input<FormControlDensity>('default');

  protected readonly hostClass = computed(() => {
    const base = 'block w-[inherit] max-w-[inherit] font-medium';

    if (this.variant() === 'compact') {
      return `${base} mb-1.5 text-xs tracking-[.04em] uppercase text-foreground/45`;
    }

    return `${base} mb-[.4rem] text-[15px] tracking-[.125px] text-foreground/60`;
  });
}

@Directive({
  selector: '[appFormHint]',
  host: {
    class:
      'block mx-[.2rem] my-[.4rem] w-[inherit] max-w-[inherit] text-xs font-medium tracking-[.125px] text-foreground/60',
  },
})
export class FormControlHintDirective {}

@Directive({
  selector: '[appFormPrefix]',
  host: {
    class: 'pl-[.8rem] opacity-60',
  },
})
export class FormControlPrefixDirective {}
