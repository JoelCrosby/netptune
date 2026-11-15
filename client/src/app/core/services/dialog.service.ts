import {
  CdkDialogContainer,
  Dialog,
  DialogConfig,
  DialogRef,
} from '@angular/cdk/dialog';
import { ComponentType } from '@angular/cdk/portal';
import { Service, StaticProvider, Type, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
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

// Marks the overlay pane of every dialog, so the shared sizing in modals.css can
// reach it. It goes first so a dialog's own panel class still overrides it.
const DIALOG_PANEL_CLASS = 'np-dialog';

type DialogPanelClass = string | string[];

// A dialog component declares the pane classes its own layout needs as a static
// `panelClass`, so no caller has to remember to pass them.
interface DialogPanelClassHost {
  panelClass?: DialogPanelClass;
}

function toPanelClasses(panelClass?: DialogPanelClass): string[] {
  if (!panelClass) return [];

  return Array.isArray(panelClass) ? panelClass : [panelClass];
}

function withDialogPanelClass<C>(
  component: ComponentType<C> & DialogPanelClassHost,
  panelClass?: DialogPanelClass
): string[] {
  const classes = [
    DIALOG_PANEL_CLASS,
    ...toPanelClasses(component.panelClass),
    ...toPanelClasses(panelClass),
  ];

  return [...new Set(classes)];
}

@Service()
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

  // Resolves with undefined when the dialog is dismissed, so callers only need `if (!result) return;`.
  openForResult<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config?: DialogConfig<D, DialogRef<R, C>>
  ): Promise<R | undefined> {
    const dialogRef = this.open<R, D, C>(component, config);

    return firstValueFrom(dialogRef.closed, { defaultValue: undefined });
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
    component: ComponentType<C> & DialogPanelClassHost,
    config: DialogConfig<D, DialogRef<R, C>>,
    container: Type<CdkDialogContainer>,
    providers: StaticProvider[] = []
  ): DialogRef<R, C> {
    return this.dialog.open<R, D, C>(component, {
      ...config,
      hasBackdrop: true,
      panelClass: withDialogPanelClass(component, config.panelClass),
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
