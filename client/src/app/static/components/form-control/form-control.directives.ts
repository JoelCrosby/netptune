import { computed, Directive, input } from '@angular/core';

export type FormControlDensity = 'default' | 'compact';

export type FormControlShape = 'default' | 'rounded';

export type FormControlAppearance = 'default' | 'quiet';

// Set on any ancestor — usually the form itself — so every field under it picks the shape up
// through the element injector. A form's fields are spread across nested editor components, which
// would otherwise each need to thread an input down to the controls they render.
@Directive({
  selector: '[appFormShape]',
})
export class FormControlShapeDirective {
  readonly appFormShape = input<FormControlShape>('default');
}

@Directive({
  selector: '[appFormAppearance]',
})
export class FormControlAppearanceDirective {
  readonly appFormAppearance = input<FormControlAppearance>('default');
}

@Directive({
  selector: 'input[appFormInput], textarea[appFormInput], select[appFormInput]',
  host: {
    class:
      'w-full appearance-none border-0 bg-transparent leading-10 text-inherit outline-none [font-family:inherit] [font-size:inherit] [font-weight:inherit] placeholder:text-muted disabled:text-foreground/40',
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
      return `${base} mb-1.5 text-xs tracking-[.04em] uppercase text-muted`;
    }

    return `${base} mb-[.4rem] text-[15px] tracking-[.125px] text-muted`;
  });
}

@Directive({
  selector: '[appFormHint]',
  host: {
    class:
      'block mx-[.2rem] my-[.4rem] w-[inherit] max-w-[inherit] text-xs font-medium tracking-[.125px] text-muted',
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
