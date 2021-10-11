import {
  query,
  trigger,
  transition,
  style,
  stagger,
  animate,
} from '@angular/animations';

export const dropIn = trigger('dropIn', [
  transition('* <=> *', [
    query(
      ':enter',
      [
        style({ opacity: 0, transform: 'translateY(-18px)' }),
        stagger(
          '50ms',
          animate(
            '320ms ease-out',
            style({ opacity: 1, transform: 'translateY(0px)' })
          )
        ),
      ],
      { optional: true }
    ),
    query(
      ':leave',
      animate(
        '320ms ease-out',
        style({ opacity: 0, transform: 'translateY(18px)' })
      ),
      {
        optional: true,
      }
    ),
  ]),
]);

export const pullIn = trigger('pullIn', [
  transition('* <=> *', [
    query(
      ':enter',
      [
        style({ opacity: 0, transform: 'translateX(-18px)' }),
        animate('320ms ease-out'),
        style({ opacity: 1, transform: 'translateX(0px)' }),
      ],
      { optional: true }
    ),
    query(
      ':leave',
      animate(
        '320ms ease-out',
        style({ opacity: 0, transform: 'translateX(18px)' })
      ),
      {
        optional: true,
      }
    ),
  ]),
]);

export const insertRemoveSidebar = trigger('insertRemoveSidebar', [
  transition(':enter', [
    style({ opacity: 0, transform: 'translateX(-320px)', width: '0px' }),
    animate(
      '320ms ease-out',
      style({ opacity: 1, transform: 'translateX(0px)', width: '320px' })
    ),
  ]),
  transition(':leave', [
    animate(
      '320ms ease-out',
      style({ opacity: 0, transform: 'translateX(-320px)', width: '0px' })
    ),
  ]),
]);

export const toggleChip = trigger('toggleChip', [
  transition(':enter', [
    style({ opacity: 0, width: 0, height: '*' }),
    animate('320ms ease-out', style({ opacity: 1, width: '*', height: '*' })),
  ]),
  transition(':leave', [
    animate('320ms ease-out', style({ opacity: 0, width: 0, height: '*' })),
  ]),
]);

export const fadeIn = trigger('fadeIn', [
  transition(':enter', [
    style({ opacity: 0 }),
    animate('320ms ease-out', style({ opacity: 1 })),
  ]),
  transition(':leave', [animate('320ms ease-out', style({ opacity: 0 }))]),
]);
