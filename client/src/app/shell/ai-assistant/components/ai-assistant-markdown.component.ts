import { NgTemplateOutlet } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiEntityReference } from '@core/models/ai-conversation';
import { AiMarkdownBlock, AiMarkdownInline } from '@core/util/ai-markdown';
import { referenceKey, referenceRoute } from '@core/util/ai-references';

@Component({
  selector: 'app-ai-assistant-markdown',
  host: { class: 'flex flex-col gap-2 text-sm leading-relaxed' },
  imports: [NgTemplateOutlet, RouterLink],
  template: `
    <ng-container
      *ngTemplateOutlet="blockList; context: { $implicit: blocks() }" />

    <ng-template #blockList let-list>
      @for (block of list; track $index) {
        @switch (block.kind) {
          @case ('heading') {
            <p class="font-overpass font-medium" [class]="headingSize(block)">
              <ng-container
                *ngTemplateOutlet="
                  inlineList;
                  context: { $implicit: block.inline }
                " />
            </p>
          }
          @case ('code') {
            <pre
              class="bg-hover overflow-x-auto rounded-lg p-3 text-xs"><code>{{ block.value }}</code></pre>
          }
          @case ('list') {
            @if (block.ordered) {
              <ol class="list-decimal space-y-1 pl-5" [start]="block.start">
                <ng-container
                  *ngTemplateOutlet="
                    listItems;
                    context: { $implicit: block.items }
                  " />
              </ol>
            } @else {
              <ul class="list-disc space-y-1 pl-5">
                <ng-container
                  *ngTemplateOutlet="
                    listItems;
                    context: { $implicit: block.items }
                  " />
              </ul>
            }
          }
          @case ('quote') {
            <blockquote class="border-border text-muted border-l-2 pl-3">
              <ng-container
                *ngTemplateOutlet="
                  blockList;
                  context: { $implicit: block.blocks }
                " />
            </blockquote>
          }
          @case ('table') {
            <div class="border-border my-4 overflow-hidden rounded border">
              <div class="overflow-x-auto">
                <table class="w-full text-left text-xs">
                  <thead class="text-muted border-border border-b">
                    <tr>
                      @for (cell of block.head; track $index) {
                        <th class="px-3 py-2 font-medium">
                          <ng-container
                            *ngTemplateOutlet="
                              inlineList;
                              context: { $implicit: cell }
                            " />
                        </th>
                      }
                    </tr>
                  </thead>
                  <tbody>
                    @for (row of block.rows; track $index) {
                      <tr
                        class="border-border/60 bg-background border-b last:border-0">
                        @for (cell of row; track $index) {
                          <td class="px-3 py-2">
                            <ng-container
                              *ngTemplateOutlet="
                                inlineList;
                                context: { $implicit: cell }
                              " />
                          </td>
                        }
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
          @case ('rule') {
            <hr class="border-border" />
          }
          @default {
            <p>
              <ng-container
                *ngTemplateOutlet="
                  inlineList;
                  context: { $implicit: block.inline }
                " />
            </p>
          }
        }
      }
    </ng-template>

    <ng-template #listItems let-items>
      @for (item of items; track $index) {
        <li class="flex flex-col gap-1">
          <ng-container
            *ngTemplateOutlet="blockList; context: { $implicit: item }" />
        </li>
      }
    </ng-template>

    <ng-template #inlineList let-parts>
      @for (part of parts; track $index) {
        @switch (part.kind) {
          @case ('strong') {
            <strong class="font-semibold">
              <ng-container
                *ngTemplateOutlet="
                  inlineList;
                  context: { $implicit: part.children }
                " />
            </strong>
          }
          @case ('em') {
            <em class="italic">
              <ng-container
                *ngTemplateOutlet="
                  inlineList;
                  context: { $implicit: part.children }
                " />
            </em>
          }
          @case ('strike') {
            <s class="line-through">
              <ng-container
                *ngTemplateOutlet="
                  inlineList;
                  context: { $implicit: part.children }
                " />
            </s>
          }
          @case ('code') {
            <code class="bg-hover rounded px-1 py-0.5 font-mono text-[0.8em]">{{
              part.value
            }}</code>
          }
          @case ('link') {
            <a
              class="text-primary hover:underline"
              target="_blank"
              rel="noreferrer noopener"
              [href]="part.href">
              <ng-container
                *ngTemplateOutlet="
                  inlineList;
                  context: { $implicit: part.children }
                " />
            </a>
          }
          @case ('reference') {
            @if (routeFor(part); as route) {
              <a
                class="bg-primary/10 text-primary rounded px-1 py-0.5 font-medium hover:underline"
                [routerLink]="route"
                >{{ part.label }}</a
              >
            } @else {
              <span
                class="bg-primary/10 text-primary rounded px-1 py-0.5 font-medium"
                >{{ part.label }}</span
              >
            }
          }
          @case ('break') {
            <br />
          }
          @default {
            <span>{{ part.value }}</span>
          }
        }
      }
    </ng-template>
  `,
})
export class AiAssistantMarkdownComponent {
  readonly blocks = input.required<AiMarkdownBlock[]>();
  readonly references = input<Map<string, AiEntityReference>>(new Map());
  readonly workspace = input<string | null>(null);

  protected headingSize(block: AiMarkdownBlock): string {
    const isTopLevel = block.kind === 'heading' && block.level <= 2;

    return isTopLevel ? 'text-base' : 'text-sm';
  }

  protected routeFor(part: AiMarkdownInline): string[] | null {
    const isReference = part.kind === 'reference';

    if (!isReference) {
      return null;
    }

    const workspace = this.workspace();
    const isKnown = this.references().has(referenceKey(part.type, part.id));

    if (!workspace || !isKnown) {
      return null;
    }

    return referenceRoute(workspace, part.type, part.id);
  }
}
