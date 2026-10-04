import { type LucideIconInput } from '@lucide/angular';
import { AvatarStackItem } from '../avatar-stack/avatar-stack.component';

export interface EntityListItem {
  id: string | number;
  name: string;
  identifier?: string | null;
  icon: LucideIconInput;
  color?: string | null;
  imageUrl?: string | null;
  people: readonly AvatarStackItem[];
  count: number;
  updatedAt: Date | string;
  link: string | readonly unknown[];
}
