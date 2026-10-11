import { type LucideIconInput } from '@lucide/angular';
import { type BadgeColor } from '../badge/badge.component';
import { type Tone } from '../tones';

export interface DocumentListItem {
  id: number | string;
  title: string;
  excerpt: string | null;
  icon: LucideIconInput;
  tone: Tone;
  statusLabel: string;
  statusTone: BadgeColor;
  tags: readonly string[];
  editorName: string | null;
  editorPictureUrl: string | null;
  editedLabel: string;
  readLabel: string | null;
  link: string | readonly unknown[];
}
