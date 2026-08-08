import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PopoverSurfaceComponent } from '@static/components/popover-surface/popover-surface.component';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { EntityType } from '@core/models/entity-type';
import {
  ActivityFeedRequest,
  activityResource,
} from '@core/resources/activity.resource';
import { LucideActivity, LucideHistory } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ActivityTimeRangePipe } from '@static/pipes/activity-time-range.pipe';
import { ActivityPipe } from '@static/pipes/activity.pipe';

@Component({
  selector: 'app-activity-menu',
  imports: [
    FlatButtonComponent,
    EmptyStateComponent,
    PopoverSurfaceComponent,
    TooltipDirective,
    LucideActivity,
    LucideHistory,
    AvatarComponent,
    SpinnerComponent,
    ActivityPipe,
    ActivityTimeRangePipe,
  ],
  template: `
    <button
      app-flat-button
      i18n-appTooltip="Tooltip on the button that opens the activity feed"
      appTooltip="Show activity"
      color="ghost"
      (click)="toggleMenu()">
      <svg
        lucideHistory
        aria-hidden="false"
        i18n-aria-label="Accessible label for the activity feed icon"
        aria-label="Show activity"></svg>
    </button>

    <ng-template #menuTemplate>
      <app-popover-surface class="mt-1 block" enterFrom="top">
        <div class="py-2">
          @if (loaded()) {
            @for (activity of activities(); track $index; let last = $last) {
              <div
                class="flex min-w-80 flex-row items-center gap-1 px-4 py-1 text-sm">
                <app-avatar
                  class="shrink-0 grow-0 basis-8"
                  [imageUrl]="activity.userPictureUrl"
                  [name]="activity.userUsername"
                  [isServiceAccount]="activity.userIsServiceAccount ?? false"
                  size="sm"></app-avatar>
                <span class="font-medium tracking-[0.225px] whitespace-nowrap">
                  {{ activity.userUsername }}
                </span>
                @if (activity.agent) {
                  <span
                    class="text-foreground/60 text-xs whitespace-nowrap"
                    i18n="
                      Precedes the assistant that made a change on the user's
                      behalf
                    ">
                    via {{ activity.agent }}
                  </span>
                }
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
              <app-empty-state
                compact
                i18n-title="Heading of the empty activity feed"
                title="There is no activity"
                i18n-description="Explains why the activity feed is empty"
                description="Activity on the item will appear here">
                <svg emptyStateIcon lucideActivity></svg>
              </app-empty-state>
            }

            @if (canLoadMore()) {
              <div class="flex justify-center px-3 pt-3">
                <button app-ghost-button (click)="loadMore()">
                  <span i18n="Button that loads the next page of activity">
                    Load more
                  </span>
                </button>
              </div>
            }
          } @else {
            <div class="flex justify-center p-4">
              <app-spinner diameter="24" />
            </div>
          }
        </div>
      </app-popover-surface>
    </ng-template>
  `,
})
export class ActivityMenuComponent implements OnDestroy {
  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private el = inject(ElementRef<HTMLElement>);

  readonly entityType = input.required<EntityType>();
  readonly entityId = input<number>();

  private readonly isOpen = signal(false);

  private readonly request = computed<ActivityFeedRequest | null>(() => {
    const entityId = this.entityId();
    const isReady = this.isOpen() && entityId !== undefined;

    if (!isReady) return null;

    return { entityType: this.entityType(), entityId };
  });

  private readonly feed = activityResource(this.request);

  readonly activities = this.feed.items;
  readonly loaded = this.feed.loaded;
  readonly canLoadMore = this.feed.canLoadMore;

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

    this.isOpen.set(true);
  }

  /* Closing idles the resource, which empties the list and forgets the cursor. */
  private closeMenu() {
    this.overlayRef?.detach();

    this.isOpen.set(false);
  }

  loadMore() {
    this.feed.loadMore();
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
