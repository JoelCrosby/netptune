import { SKIP, visit } from 'unist-util-visit';
import { slugify } from './docs-content';

type MarkdownNode = {
  type: string;
  value?: string;
  alt?: string;
  name?: string;
  depth?: number;
  ordered?: boolean;
  attributes?: Record<string, string>;
  children?: MarkdownNode[];
  data?: {
    hName?: string;
    hProperties?: Record<string, string>;
  };
};

const admonitions: Record<string, { title: string; icon: string }> = {
  note: { title: 'Note', icon: 'i' },
  info: { title: 'Note', icon: 'i' },
  tip: { title: 'Tip', icon: '✦' },
  warning: { title: 'Warning', icon: '!' },
  danger: { title: 'Warning', icon: '!' },
};

function element(
  tagName: string,
  className: string,
  children: MarkdownNode[],
  properties: Record<string, string> = {},
): MarkdownNode {
  return {
    type: 'docsElement',
    data: { hName: tagName, hProperties: { class: className, ...properties } },
    children,
  };
}

function text(value: string): MarkdownNode {
  return { type: 'text', value };
}

function plainText(node: MarkdownNode): string {
  return node.value ?? node.children?.map(plainText).join('') ?? '';
}

function stripLeadInSeparator(nodes: MarkdownNode[]): MarkdownNode[] {
  const [first, ...rest] = nodes;

  if (first?.type !== 'text' || first.value === undefined) {
    return nodes;
  }

  const value = first.value.replace(/^\s*[—–]?\s*/, '');
  return [text(value.charAt(0).toUpperCase() + value.slice(1)), ...rest];
}

function withoutTrailingPeriod(nodes: MarkdownNode[]): MarkdownNode[] {
  const last = nodes.at(-1);

  if (last?.type !== 'text' || !last.value?.endsWith('.')) {
    return nodes;
  }

  return [...nodes.slice(0, -1), text(last.value.slice(0, -1))];
}

function leadParagraph(item: MarkdownNode): MarkdownNode | undefined {
  const paragraph = item.children?.[0];
  return item.children?.length === 1 && paragraph?.type === 'paragraph' ? paragraph : undefined;
}

function isDefinitionList(list: MarkdownNode): boolean {
  return (list.children ?? []).every((item) => {
    const [lead, after] = leadParagraph(item)?.children ?? [];

    if (lead?.type !== 'strong') {
      return false;
    }

    return plainText(lead).endsWith('.') || /^\s*[—–]\s/.test(after?.value ?? '');
  });
}

function transformAdmonition(node: MarkdownNode) {
  const admonition = admonitions[node.name ?? ''];
  const title = node.attributes?.title ?? admonition.title;

  node.data = {
    hName: 'div',
    hProperties: { class: `docs-admonition docs-admonition-${node.name}` },
  };
  node.children = [
    element('span', 'docs-admonition-icon', [text(admonition.icon)], { 'aria-hidden': 'true' }),
    element('div', 'docs-admonition-body', [
      element('p', 'docs-admonition-title', [text(title)]),
      ...(node.children ?? []),
    ]),
  ];
}

function transformCards(node: MarkdownNode) {
  node.data = { hName: 'div', hProperties: { class: 'docs-cards' } };

  for (const list of node.children ?? []) {
    if (list.type !== 'list') continue;

    list.data = { hProperties: { class: 'docs-cards-list' } };

    for (const item of list.children ?? []) {
      const paragraph = leadParagraph(item);
      const [link, ...rest] = paragraph?.children ?? [];

      if (!paragraph || link?.type !== 'link') continue;

      paragraph.children = [
        link,
        element('span', 'docs-card-description', stripLeadInSeparator(rest)),
      ];
    }
  }
}

function transformDefinitionList(list: MarkdownNode) {
  list.data = { hProperties: { class: 'docs-definitions' } };

  for (const item of list.children ?? []) {
    const paragraph = leadParagraph(item);
    const [lead, ...rest] = paragraph?.children ?? [];

    if (!paragraph || !lead) continue;

    paragraph.children = [
      element('span', 'docs-term', withoutTrailingPeriod(lead.children ?? [])),
      element('span', 'docs-description', stripLeadInSeparator(rest)),
    ];
  }
}

export default function remarkDocDirectives() {
  return (tree: MarkdownNode) => {
    visit(tree, 'containerDirective', (node: MarkdownNode) => {
      if (node.name === 'cards') {
        transformCards(node);
      } else if (node.name && node.name in admonitions) {
        transformAdmonition(node);
      }
    });

    visit(tree, 'list', (node: MarkdownNode) => {
      if (node.ordered || node.data) return;

      const readsAsDefinitions = isDefinitionList(node);

      if (readsAsDefinitions) {
        transformDefinitionList(node);
      }
    });

    visit(tree, 'heading', (node: MarkdownNode) => {
      if (node.depth === 2 || node.depth === 3) {
        node.data = { hProperties: { id: slugify(plainText(node)) } };
      }
    });

    visit(tree, 'table', (node: MarkdownNode, index, parent: MarkdownNode | undefined) => {
      if (index === undefined || !parent?.children) return;

      parent.children[index] = element('div', 'docs-table', [node]);
      return [SKIP, index + 1];
    });

    visit(tree, 'paragraph', (node: MarkdownNode) => {
      const [image] = node.children ?? [];

      if (node.children?.length !== 1 || image.type !== 'image') return;

      node.data = { hName: 'figure', hProperties: { class: 'docs-figure' } };
      node.children = [
        element('span', 'docs-figure-frame', [image]),
        ...(image.alt ? [element('figcaption', 'docs-figure-caption', [text(image.alt)])] : []),
      ];
    });
  };
}
