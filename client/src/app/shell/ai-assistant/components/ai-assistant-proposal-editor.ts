import {
  ApplicationRef,
  createComponent,
  EnvironmentInjector,
} from '@angular/core';
import {
  createPageReferenceExtension,
  PageReferenceDisplay,
  PageReferenceHost,
} from '@app/features/pages/editor/page-reference.extension';
import { PageReferenceAttrs } from '@app/features/pages/editor/page-reference-syntax';
import {
  pageSyntax,
  referenceSyntax,
} from '@app/features/pages/editor/page-syntax';
import {
  createQueryEmbedExtension,
  QueryEmbedMount,
} from '@app/features/pages/editor/query-embed.extension';
import { MarkdownSyntax } from '@static/components/editor/editor-content';
import { EmbedFrameComponent } from '@static/components/embed-frame/embed-frame.component';
import { AnyExtension } from '@tiptap/core';

export interface ProposalEditorConfig {
  syntax: MarkdownSyntax;
  extensions: readonly AnyExtension[];
}

export interface ProposalEditorOptions {
  withQueryEmbeds: boolean;
  injector: EnvironmentInjector;
}

// A proposal is edited away from the page or task it belongs to, so references show the label the assistant
// wrote and an embed shows a placeholder. Without these nodes the editor would escape both on save.
export function proposalEditorConfig(
  options: ProposalEditorOptions
): ProposalEditorConfig {
  const references = createPageReferenceExtension(inertReferenceHost);

  if (!options.withQueryEmbeds) {
    return { syntax: referenceSyntax, extensions: [references] };
  }

  const embeds = createQueryEmbedExtension((slug, element) => {
    return mountEmbedPlaceholder(slug, element, options.injector);
  });

  return { syntax: pageSyntax, extensions: [references, embeds] };
}

const ignore = () => undefined;

const inertReferenceHost: PageReferenceHost = {
  display: describeReference,
  subscribe: () => ignore,
  hovered: ignore,
  unhovered: ignore,
  opened: ignore,
  suggestion: {
    start: ignore,
    update: ignore,
    keyDown: () => false,
    exit: ignore,
  },
};

function describeReference(attrs: PageReferenceAttrs): PageReferenceDisplay {
  if (attrs.kind === 'task') {
    return { key: attrs.key, text: attrs.label ?? '', resolved: true };
  }

  return { key: null, text: attrs.label ?? attrs.key, resolved: true };
}

function mountEmbedPlaceholder(
  slug: string,
  element: HTMLElement,
  injector: EnvironmentInjector
): QueryEmbedMount {
  const appRef = injector.get(ApplicationRef);
  const frame = createComponent(EmbedFrameComponent, {
    environmentInjector: injector,
    hostElement: element,
  });

  frame.setInput(
    'badge',
    $localize`:Placeholder for a query embedded in a page body the assistant proposes, shown while the body is edited:Embedded query`
  );
  frame.setInput('heading', slug);
  appRef.attachView(frame.hostView);

  return {
    update: (updated) => frame.setInput('heading', updated),
    destroy: () => {
      appRef.detachView(frame.hostView);
      frame.destroy();
    },
  };
}
