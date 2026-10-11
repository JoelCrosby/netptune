import bash from 'highlight.js/lib/languages/bash';
import csharp from 'highlight.js/lib/languages/csharp';
import css from 'highlight.js/lib/languages/css';
import diff from 'highlight.js/lib/languages/diff';
import javascript from 'highlight.js/lib/languages/javascript';
import json from 'highlight.js/lib/languages/json';
import markdown from 'highlight.js/lib/languages/markdown';
import python from 'highlight.js/lib/languages/python';
import sql from 'highlight.js/lib/languages/sql';
import typescript from 'highlight.js/lib/languages/typescript';
import xml from 'highlight.js/lib/languages/xml';
import yaml from 'highlight.js/lib/languages/yaml';
import { createLowlight } from 'lowlight';
import type { Parser } from 'prosemirror-highlight';
import { createParser } from 'prosemirror-highlight/lowlight';

const lowlight = createLowlight({
  bash,
  csharp,
  css,
  diff,
  javascript,
  json,
  markdown,
  python,
  sql,
  typescript,
  xml,
  yaml,
});

const highlight = createParser(lowlight);

export const codeParser: Parser = (options) => {
  const isKnownLanguage =
    !!options.language && lowlight.registered(options.language);

  return isKnownLanguage ? highlight(options) : [];
};
