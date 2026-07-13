import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { EntityType } from '@core/models/entity-type';
import * as ActivityActions from '@core/store/activity/activity.actions';
import {
  selectActivities,
  selectActivitiesLoaded,
  selectActivityCanLoadMore,
} from '@core/store/activity/activity.selectors';
import { LucideHistory } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ActivityTimeRangePipe } from '@static/pipes/activity-time-range.pipe';
import { ActivityPipe } from '@static/pipes/activity.pipe';

@Component({
  selector: 'app-activity-menu',
  imports: [
    FlatButtonComponent,
    TooltipDirective,
    LucideHistory,
    AvatarComponent,
    SpinnerComponent,
    ActivityPipe,
    ActivityTimeRangePipe,
  ],
  styles: [
    `
      @keyframes dropdown-in {
        from {
          opacity: 0;
          transform: scale(0.95) translateY(-4px);
        }
        to {
          opacity: 1;
          transform: scale(1) translateY(0);
        }
      }

      .dropdown-menu {
        animation: dropdown-in 120ms ease-out;
        transform-origin: top;
      }
    `,
  ],
  template: `<button
      app-flat-button
      appTooltip="Show activity"
      color="ghost"
      (click)="toggleMenu()">
      <svg lucideHistory aria-hidden="false" aria-label="Show activity"></svg>
    </button>

    <ng-template #menuTemplate>
      <div
        class="dropdown-menu custom-scroll bg-background border-border mt-1 max-h-[80vh] max-w-120 min-w-100 overflow-y-auto rounded border py-2 shadow-lg dark:bg-neutral-900">
        @if (loaded()) {
          @for (activity of activities(); track $index; let last = $last) {
            <div
              class="flex min-w-80 flex-row items-center gap-1 px-4 py-1 text-sm">
              <app-avatar
                class="shrink-0 grow-0 basis-8"
                [imageUrl]="activity.userPictureUrl"
                [name]="activity.userUsername"
                size="sm">
              </app-avatar>
              <span class="font-medium tracking-[0.225px] whitespace-nowrap">
                {{ activity.userUsername }}
              </span>
              <span
                class="text-foreground/90 ml-[0.3rem] text-xs whitespace-nowrap"
                [appTooltip]="activity | activityTimeRange">
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

          @if (canLoadMore()) {
            <div class="flex justify-center px-3 pt-3">
              <button app-ghost-button (click)="loadMore()">
                <span>Load more</span>
              </button>
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
  readonly canLoadMore = this.store.selectSignal(selectActivityCanLoadMore);

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
        ActivityActions.loadActivity.init({ entityType, entityId })
      );
    }
  }

  private closeMenu() {
    this.overlayRef?.detach();
    this.store.dispatch(ActivityActions.clearState());
  }

  loadMore() {
    const entityType = this.entityType();
    const entityId = this.entityId();

    if (entityId !== undefined) {
      this.store.dispatch(
        ActivityActions.loadMoreActivity.init({ entityType, entityId })
      );
    }
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
