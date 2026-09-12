import { Component, computed, inject, input } from '@angular/core';
import { BuildInfoService } from '@core/services/build-info.service';

export type BuildNumberAppearance = 'fixed' | 'inline';

const appearanceClasses: Record<BuildNumberAppearance, string> = {
  fixed: 'fixed right-8 bottom-4 text-xs font-medium tracking-[0.125px]',
  inline: 'font-avatar text-[11px] tracking-[.04em]',
};

@Component({
  selector: 'app-build-number',
  template: `
    @if (buildInfo(); as buildInfo) {
      <div [class]="wrapperClass()">
        <a
          class="gitHashShort"
          [href]="
            'https://github.com/JoelCrosby/Netptune/commit/' + buildInfo.gitHash
          "
          target="_blank"
          rel="noopener noreferrer">
          {{ buildInfo.gitHashShort }}
        </a>
        <span aria-hidden="true">{{ separator() }}</span>
        <span class="buildNumber">
          <ng-container
            i18n="
              Build identifier in the footer. NUMBER is the CI build number
            ">
            BUILD
            {{
              buildInfo.buildNumber // i18n(ph="NUMBER")
            }}
          </ng-container>
        </span>
        <span aria-hidden="true">{{ separator() }}</span>
        <a
          class="runId"
          [href]="
            'https://github.com/JoelCrosby/Netptune/actions/runs/' +
            buildInfo.runId
          "
          target="_blank"
          rel="noopener noreferrer">
          <span i18n="Footer link to the CI run that produced this build">
            Github Action
          </span>
        </a>
      </div>
    }
  `,
})
export class BuildNumberComponent {
  readonly buildInfo = inject(BuildInfoService).buildInfo;

  readonly appearance = input<BuildNumberAppearance>('fixed');

  protected readonly wrapperClass = computed(() => {
    return `${appearanceClasses[this.appearance()]} opacity-60`;
  });

  protected readonly separator = computed(() => {
    return this.appearance() === 'inline' ? '·' : '|';
  });
}
