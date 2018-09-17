import { query, trigger, transition, style, stagger, animate } from '@angular/animations';

export const dropIn = trigger('dropIn', [
  transition('* <=> *', [
    query(':enter', [
      style({ opacity: 0, transform: 'translateY(-18px)' }),
      stagger('50ms',
        animate('320ms ease-out',
          style({ opacity: 1, transform: 'translateY(0px)' }))),
    ], { optional: true }),
    query(':leave', animate('320ms ease-out', style({ opacity: 0, transform: 'translateY(18px)' })), {
      optional: true
    })
  ])
]);

export const pullIn = trigger('pullIn', [
  transition('* <=> *', [
    query(':enter', [
      style({ opacity: 0, transform: 'translateX(-18px)' }),
      animate('320ms ease-out'),
      style({ opacity: 1, transform: 'translateX(0px)' }),
    ], { optional: true }),
    query(':leave', animate('320ms ease-out', style({ opacity: 0, transform: 'translateX(18px)' })), {
      optional: true
    })
  ])
]);
