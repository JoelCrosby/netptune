import { visit } from 'unist-util-visit';

type DirectiveNode = {
  type: 'containerDirective';
  name: string;
  attributes?: Record<string, string>;
  data?: {
    hName?: string;
    hProperties?: Record<string, string>;
  };
};

type MarkdownTree = {
  type: string;
  children: unknown[];
};

const admonitions = new Set(['note', 'tip', 'info', 'warning', 'danger']);

export default function remarkDocDirectives() {
  return (tree: MarkdownTree) => {
    visit(tree, 'containerDirective', (node: DirectiveNode) => {
      if (!admonitions.has(node.name) && node.name !== 'cards') return;

      const data = node.data ?? (node.data = {});
      data.hName = 'div';
      data.hProperties = {
        class:
          node.name === 'cards' ? 'docs-cards' : `docs-admonition docs-admonition-${node.name}`,
        ...(node.attributes?.title ? { 'data-title': node.attributes.title } : {}),
      };
    });
  };
}
