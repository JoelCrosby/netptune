import {
  CdkDialogContainer,
  Dialog,
  DialogConfig,
  DialogRef,
} from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import { Injectable, StaticProvider, Type, inject } from '@angular/core';
import { DialogContainerComponent } from '@static/components/dialog/dialog-container.component';
import {
  DIALOG_WIZARD_TITLE,
  DialogWizardComponent,
} from '@static/components/dialog-wizard/dialog-wizard.component';

export interface DialogWizardConfig<D, R, C> extends DialogConfig<
  D,
  DialogRef<R, C>
> {
  title: string;
}

@Injectable({ providedIn: 'root' })
export class DialogService {
  private dialog = inject(Dialog);

  open<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config?: DialogConfig<D, DialogRef<R, C>>
  ): DialogRef<R, C> {
    const dialogConfig = config ?? new DialogConfig<D, DialogRef<R, C>>();

    return this.openWithContainer(
      component,
      dialogConfig,
      DialogContainerComponent
    );
  }

  openWizard<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config: DialogWizardConfig<D, R, C>
  ): DialogRef<R, C> {
    return this.openWithContainer(
      component,
      {
        ...config,
        ariaLabel: config.ariaLabel ?? config.title,
      },
      DialogWizardComponent,
      [{ provide: DIALOG_WIZARD_TITLE, useValue: config.title }]
    );
  }

  private openWithContainer<R, D, C>(
    component: ComponentType<C>,
    config: DialogConfig<D, DialogRef<R, C>>,
    container: Type<CdkDialogContainer>,
    providers: StaticProvider[] = []
  ): DialogRef<R, C> {
    return this.dialog.open<R, D, C>(component, {
      ...config,
      hasBackdrop: true,
      container: {
        type: container,
        providers: () => [
          { provide: DialogConfig, useValue: config },
          ...providers,
        ],
      },
    });
  }
}
