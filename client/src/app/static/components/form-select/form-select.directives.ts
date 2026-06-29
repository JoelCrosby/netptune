import { Directive } from '@angular/core';

@Directive({
  selector: '[appFormSelectOption]',
  host: {
    class:
      'cursor-pointer select-none overflow-hidden rounded-[.2rem] p-[.4rem] text-ellipsis whitespace-nowrap hover:bg-hover [&.selected]:bg-primary-selected [&.active]:bg-hover [&.selected:hover]:bg-primary-selected-hover [&.active.selected]:bg-primary-selected-hover',
  },
})
export class FormSelectOptionDirective {}

@Directive({
  selector: '[appFormSelectDropdown]',
  host: {
    class:
      'flex w-full cursor-pointer flex-col gap-[.4rem] rounded-[.4rem] border border-border bg-secondary-background p-[.4rem] shadow-lg',
  },
})
export class FormSelectDropdownStyleDirective {}
