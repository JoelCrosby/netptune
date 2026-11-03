import { inject, Service } from '@angular/core';
import { Router } from '@angular/router';
import { ScrollAnchorService } from '@core/router/scroll-anchor.service';
import { CommandPlace } from '@core/services/command-registry.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { pathOf } from '@core/util/url';

@Service()
export class PlaceNavigationService {
  private readonly router = inject(Router);
  private readonly workspace = inject(WorkspaceService);
  private readonly anchors = inject(ScrollAnchorService);

  urlFor({ path, fragment }: CommandPlace): string | null {
    const workspace = this.workspace.getWorkspaceRoute();

    if (!workspace) return null;

    return `/${workspace}/${path}${fragment ? `#${fragment}` : ''}`;
  }

  open(url: string) {
    const target = this.router.parseUrl(url);
    const current = this.router.parseUrl(this.router.url);
    const isSamePage = pathOf(url) === pathOf(this.router.url);

    if (isSamePage) target.queryParams = current.queryParams;

    const fragment = target.fragment;

    if (isSamePage && fragment && fragment === current.fragment) {
      this.anchors.request(fragment, true);

      return;
    }

    void this.router.navigateByUrl(target);
  }
}
