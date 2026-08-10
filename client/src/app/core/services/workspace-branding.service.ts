import {
  EnvironmentProviders,
  effect,
  inject,
  Service,
  provideAppInitializer,
} from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { ThemeService } from '@core/services/theme.service';
import { workspaceBrandVariables } from '@core/util/colors/workspace-branding';

@Service()
export class WorkspaceBrandingService {
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly theme = inject(ThemeService);

  constructor() {
    effect(() => this.apply());
  }

  private apply() {
    const color = this.currentWorkspace.workspace()?.metaInfo?.color;
    const isDark = this.theme.theme() === 'dark';

    const root = document.documentElement.style;
    const variables = workspaceBrandVariables(color, isDark);

    for (const [property, value] of Object.entries(variables)) {
      if (value) {
        root.setProperty(property, value);
      } else {
        root.removeProperty(property);
      }
    }
  }
}

export function provideWorkspaceBranding(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(WorkspaceBrandingService);
  });
}
