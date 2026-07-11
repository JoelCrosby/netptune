import {
  Component,
  ElementRef,
  OnInit,
  effect,
  inject,
  input,
} from '@angular/core';
import { Router } from '@angular/router';
import { CardListItemComponent } from '@app/static/components/card/card-list-item.component';
import { Workspace } from '@core/models/workspace';
import { DialogService } from '@core/services/dialog.service';
import { selectEffectiveTheme } from '@core/store/settings/settings.selectors';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { HeaderAction } from '@core/types/header-action';
import { workspaceBrandVariables } from '@core/util/colors/workspace-branding';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { LucidePanelsTopLeft } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { WorkspaceService } from '../../../core/services/workspace.service';

@Component({
  selector: 'app-workspace-list-item',
  imports: [CardListItemComponent, FromNowPipe],
  // The derived variables are declared on :root, where they resolve against the
  // root --primary-rgb and inherit as finished colours. Re-declaring them here
  // makes them resolve against the --primary-rgb set on this card instead.
  styles: `
    :host {
      --primary: rgba(var(--primary-rgb));
      --primary-selected: rgba(var(--primary-rgb), 0.6);
      --primary-selected-hover: rgba(var(--primary-rgb), 0.8);
    }
  `,
  template: `
    <app-card-list-item
      [title]="workspace().name"
      [description]="workspace().description"
      [subText]="'Last updated ' + (workspace().updatedAt | fromNow)"
      [actions]="actions"
      (delete)="deleteClicked(workspace())">
      @if (workspace().isLastVisited) {
        <span
          class="bg-primary/10 text-primary mt-2 w-fit rounded-sm px-2 py-0.5 text-xs font-medium">
          Last visited
        </span>
      }
    </app-card-list-item>
  `,
})
export class WorkspaceListItemComponent implements OnInit {
  private store = inject(Store);
  private dialog = inject(DialogService);

  private workspaceService = inject(WorkspaceService);
  private router = inject(Router);
  private elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly workspace = input.required<Workspace>();

  private theme = this.store.selectSignal(selectEffectiveTheme);

  actions!: HeaderAction[];

  constructor() {
    // Scope the brand variables to the card, so each workspace's controls are
    // tinted with its own colour rather than the current workspace's.
    effect(() => {
      const variables = workspaceBrandVariables(
        this.workspace().metaInfo?.color,
        this.theme() === 'dark'
      );

      const style = this.elementRef.nativeElement.style;

      for (const [property, value] of Object.entries(variables)) {
        if (value) {
          style.setProperty(property, value);
        } else {
          style.removeProperty(property);
        }
      }
    });
  }

  ngOnInit() {
    this.actions = [
      {
        label: 'Go To Projects',
        click: () => this.onGotToClicked(),
        icon: LucidePanelsTopLeft,
      },
      {
        label: 'Manage Users',
        isLink: true,
        routerLink: ['/', this.workspace().slug, 'users'],
      },
      {
        label: 'Edit Workspace',
        click: () => this.onEditClicked(),
      },
    ];
  }

  onGotToClicked() {
    this.workspaceService.setWorkspace(this.workspace().slug);
    this.router.navigate(['/', this.workspace().slug, 'projects']);
  }

  onEditClicked() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: this.workspace(),
      width: '720px',
    });
  }

  deleteClicked(workspace: Workspace) {
    this.store.dispatch(WorkspaceActions.deleteWorkspace.init({ workspace }));
  }
}
