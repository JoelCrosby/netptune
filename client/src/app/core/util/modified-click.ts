export function isModifiedClick(event: MouseEvent) {
  return event.ctrlKey || event.metaKey || event.shiftKey || event.altKey;
}
