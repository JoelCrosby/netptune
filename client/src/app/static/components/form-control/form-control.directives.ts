import { Directive } from '@angular/core';

@Directive({
  selector: 'input[appFormInput], textarea[appFormInput], select[appFormInput]',
  host: {
    class:
      'w-full appearance-none border-0 bg-transparent leading-10 text-inherit outline-none [font-family:inherit] [font-size:inherit] [font-weight:inherit] placeholder:opacity-60 disabled:bg-foreground/[0.02] disabled:text-foreground/40',
  },
})
export class FormControlInputDirective {}

@Directive({
  selector: 'label[appFormLabel]',
  host: {
    class:
      'block mb-[.4rem] w-[inherit] max-w-[inherit] text-[15px] font-medium tracking-[.125px] text-foreground/60',
  },
})
export class FormControlLabelDirective {}

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
