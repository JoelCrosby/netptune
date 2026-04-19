import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { EntityType } from '@core/models/entity-type';
import * as ActivityActions from '@core/store/activity/activity.actions';
import {
  selectActivities,
  selectActivitiesLoaded,
} from '@core/store/activity/activity.selectors';
import { LucideActivity } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ActivityPipe } from '@static/pipes/activity.pipe';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';

@Component({
  selector: 'app-activity-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    StrokedButtonComponent,
    TooltipDirective,
    LucideActivity,
    AvatarComponent,
    SpinnerComponent,
    ActivityPipe,
  ],
  template: `<button
      app-stroked-button
      appTooltip="Show activity"
      (click)="toggleMenu()">
      <svg lucideActivity aria-hidden="false" aria-label="Show activity"></svg>
    </button>

    <ng-template #menuTemplate>
      <div
        class="custom-scroll border-border bg-background mt-[0.4rem] max-h-[80vh] max-w-120 min-w-100 overflow-y-auto rounded-[0.4rem] border p-1 py-3 shadow-lg">
        @if (loaded()) {
          @for (activity of activities(); track $index; let last = $last) {
            <div
              class="flex min-w-80 flex-row items-center px-[1.2rem] py-[0.6rem] text-sm">
              <app-avatar
                class="shrink-0 grow-0 basis-8"
                [imageUrl]="activity.userPictureUrl"
                [name]="activity.userUsername"
                size="sm">
              </app-avatar>
              <span class="font-medium tracking-[0.225px] whitespace-nowrap">
                {{ activity.userUsername }}
              </span>
              <span class="text-foreground/90 ml-[0.3rem] whitespace-nowrap">
                {{ activity | activity }}
              </span>
            </div>

            @if (!last) {
              <div class="border-border/50 my-1 w-full border-t"></div>
            }
          } @empty {
            <div
              class="flex min-w-80 flex-col items-center gap-4 px-[0.8rem] py-[0.4rem] text-sm">
              <svg lucideActivity></svg>
              <span> There is no activity </span>
              <p class="text-foreground/60">
                Activity on the item will appear here
              </p>
            </div>
          }
        } @else {
          <div class="flex justify-center p-4">
            <app-spinner diameter="24" />
          </div>
        }
      </div>
    </ng-template> `,
})
export class ActivityMenuComponent implements OnDestroy {
  private store = inject(Store);
  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private el = inject(ElementRef<HTMLElement>);

  readonly entityType = input.required<EntityType>();
  readonly entityId = input<number>();

  readonly activities = this.store.selectSignal(selectActivities);
  readonly loaded = this.store.selectSignal(selectActivitiesLoaded);

  private readonly menuTemplate =
    viewChild.required<TemplateRef<unknown>>('menuTemplate');

  private overlayRef?: OverlayRef;

  toggleMenu() {
    if (this.overlayRef?.hasAttached()) {
      this.closeMenu();
    } else {
      this.openMenu();
    }
  }

  private openMenu() {
    const el = this.el.nativeElement.querySelector('button') as HTMLElement;

    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(el)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
        },
        {
          originX: 'start',
          originY: 'top',
          overlayX: 'start',
          overlayY: 'bottom',
        },
      ]);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
    });

    this.overlayRef.attach(new TemplatePortal(this.menuTemplate(), this.vcr));
    this.overlayRef.backdropClick().subscribe(() => this.closeMenu());

    const entityType = this.entityType();
    const entityId = this.entityId();

    if (entityId !== undefined) {
      this.store.dispatch(
        ActivityActions.loadActivity({ entityType, entityId })
      );
    }
  }

  private closeMenu() {
    this.overlayRef?.detach();
    this.store.dispatch(ActivityActions.clearState());
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
