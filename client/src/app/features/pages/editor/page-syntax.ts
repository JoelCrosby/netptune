import { MarkdownSyntax } from '@static/components/editor/editor-content';
import { pageReferenceSyntax } from './page-reference-syntax';
import { queryEmbedSyntax } from './query-embed-syntax';

// Shared instances, so the editor's cached lexer for each is built once for the whole app.
export const pageSyntax: MarkdownSyntax = {
  inline: [pageReferenceSyntax],
  block: [queryEmbedSyntax],
};

export const referenceSyntax: MarkdownSyntax = {
  inline: [pageReferenceSyntax],
};
