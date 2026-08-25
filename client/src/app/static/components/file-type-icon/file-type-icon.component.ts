import { Component, computed, input } from '@angular/core';
import { WorkspaceFileContentTypeGroup } from '@core/models/view-models/workspace-file-view-model';
import {
  LucideFile,
  LucideFileArchive,
  LucideFileImage,
  LucideFileJson,
  LucideFileSpreadsheet,
  LucideFileText,
} from '@lucide/angular';
import { cn } from '../button/button.variants';

export type FileTypeIconSize = 'small' | 'medium';

export type FileTypeIconGroup =
  WorkspaceFileContentTypeGroup | 'spreadsheet' | 'data';

const groupClasses: Record<FileTypeIconGroup, string> = {
  image: 'bg-blue-500/10 text-blue-600 dark:text-blue-400',
  document: 'bg-primary/10 text-primary',
  spreadsheet: 'bg-green-500/10 text-green-600 dark:text-green-400',
  data: 'bg-violet-500/10 text-violet-600 dark:text-violet-400',
  archive: 'bg-amber-500/10 text-amber-600 dark:text-amber-400',
  other: 'bg-foreground/8 text-muted',
};

@Component({
  selector: 'app-file-type-icon',
  imports: [
    LucideFile,
    LucideFileArchive,
    LucideFileImage,
    LucideFileJson,
    LucideFileSpreadsheet,
    LucideFileText,
  ],
  host: { class: 'contents' },
  template: `
    <span [class]="containerClass()" aria-hidden="true">
      @switch (group()) {
        @case ('image') {
          <svg lucideFileImage [class]="iconClass()"></svg>
        }
        @case ('document') {
          <svg lucideFileText [class]="iconClass()"></svg>
        }
        @case ('spreadsheet') {
          <svg lucideFileSpreadsheet [class]="iconClass()"></svg>
        }
        @case ('data') {
          <svg lucideFileJson [class]="iconClass()"></svg>
        }
        @case ('archive') {
          <svg lucideFileArchive [class]="iconClass()"></svg>
        }
        @default {
          <svg lucideFile [class]="iconClass()"></svg>
        }
      }
    </span>
  `,
})
export class FileTypeIconComponent {
  readonly group = input<FileTypeIconGroup>('other');
  readonly size = input<FileTypeIconSize>('medium');
  readonly class = input('');

  protected readonly containerClass = computed(() =>
    cn(
      'flex shrink-0 items-center justify-center rounded-md',
      this.size() === 'small' ? 'h-7 w-7' : 'h-9 w-9',
      groupClasses[this.group()] ?? groupClasses.other,
      this.class()
    )
  );

  protected readonly iconClass = computed(() =>
    this.size() === 'small' ? 'h-3.5 w-3.5' : 'h-4 w-4'
  );
}
