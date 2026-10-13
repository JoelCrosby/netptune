import { formatDate } from '@angular/common';
import {
  booleanAttribute,
  Component,
  LOCALE_ID,
  computed,
  inject,
  input,
} from '@angular/core';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { LucideCalendarClock } from '@lucide/angular';
import {
  EntityIdentityComponent,
  EntityIdentitySize,
  EntityIdentityVariant,
} from '@static/components/entity-identity/entity-identity.component';
import { SprintDaysBadgeComponent } from '@static/components/sprint-days-badge.component';
import { SprintStatusBadgeComponent } from '@static/components/sprint-status-badge.component';

@Component({
  selector: 'app-sprint-identity',
  host: { class: 'contents' },
  imports: [
    EntityIdentityComponent,
    SprintDaysBadgeComponent,
    SprintStatusBadgeComponent,
  ],
  template: `
    <app-entity-identity
      [icon]="sprintIcon"
      [name]="sprint().name"
      [eyebrow]="eyebrow()"
      [metaLead]="sprint().projectName"
      [meta]="meta()"
      [description]="description()"
      [link]="link()"
      [headingLevel]="headingLevel()"
      [size]="size()"
      [variant]="variant()"
      [class]="class()">
      <app-sprint-status-badge identityBadges [status]="sprint().status" />
      <app-sprint-days-badge
        identityBadges
        [status]="sprint().status"
        [endDate]="sprint().endDate" />
    </app-entity-identity>
  `,
})
export class SprintIdentityComponent {
  readonly sprint = input.required<SprintViewModel>();
  readonly size = input<EntityIdentitySize>('medium');
  readonly variant = input<EntityIdentityVariant>('plain');
  readonly eyebrow = input('');
  readonly detail = input('');
  readonly link = input<unknown[] | null>(null);
  readonly headingLevel = input<0 | 1 | 2>(0);
  readonly showGoal = input(false, { transform: booleanAttribute });
  readonly class = input('');

  private readonly locale = inject(LOCALE_ID);

  protected readonly sprintIcon = LucideCalendarClock;

  protected readonly meta = computed(() => {
    const sprint = this.sprint();
    const start = formatDate(sprint.startDate, 'mediumDate', this.locale);
    const end = formatDate(sprint.endDate, 'mediumDate', this.locale);
    const dates = `${start} – ${end}`;
    const detail = this.detail();

    return detail ? `${dates}\u00a0·\u00a0${detail}` : dates;
  });

  protected readonly description = computed(() => {
    return this.showGoal() ? (this.sprint().goal ?? '') : '';
  });
}
