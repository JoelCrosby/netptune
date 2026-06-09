export function isNullOrUndefined(value: unknown): value is null | undefined {
  return value === null || value === undefined;
}

export function isNotNullOrUndefined<T>(
  value: T | null | undefined
): value is T {
  return !isNullOrUndefined(value);
}
