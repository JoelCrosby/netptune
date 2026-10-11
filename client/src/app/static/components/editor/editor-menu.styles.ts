const editorMenuPanel =
  'bg-card border-border text-foreground rounded-lg border p-1 shadow-lg';

const editorMenuControl =
  'hover:bg-hover data-[active=true]:bg-primary/15 data-[active=true]:text-primary flex h-7 shrink-0 cursor-pointer items-center rounded transition-colors';

export const editorMenuSurface = `${editorMenuPanel} flex items-center gap-0.5`;

export const editorMenuList = `${editorMenuPanel} flex flex-col gap-0.5`;

export const editorMenuButton = `${editorMenuControl} w-7 justify-center`;

export const editorMenuItem = `${editorMenuControl} gap-2 px-2`;

export const editorMenuSeparator = 'bg-border mx-1 h-5 w-px shrink-0';

export const editorMenuIcon = 'h-4 w-4';
