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
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
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

@Component({
  selector: 'app-activity-menu',
  templateUrl: './activity-menu.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FlatButtonComponent,
    TooltipDirective,
    LucideActivity,
    AvatarComponent,
    SpinnerComponent,
    ActivityPipe,
  ],
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
