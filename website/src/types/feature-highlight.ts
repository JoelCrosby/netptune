import type { JSX } from 'solid-js';

export interface FeatureHighlight {
  eyebrow: string;
  title: string;
  description: string;
  extra?: () => JSX.Element;
  visual: (() => JSX.Element) | string;
  reversed?: boolean;
}
