export const MIN_AI_PANEL_WIDTH = 320;
export const MAX_AI_PANEL_WIDTH = 880;
export const DEFAULT_AI_PANEL_WIDTH = 416;

export function clampAiPanelWidth(width: number): number {
  if (!Number.isFinite(width)) {
    return DEFAULT_AI_PANEL_WIDTH;
  }

  return Math.round(
    Math.min(MAX_AI_PANEL_WIDTH, Math.max(MIN_AI_PANEL_WIDTH, width))
  );
}
