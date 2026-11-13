export function summariseFilterValues(values: string[]): string | null {
  const [first, ...rest] = values;

  if (!first) return null;

  if (!rest.length) return first;

  return $localize`:Summary shown on a filter control. VALUE is the first selected option and COUNT is how many more are selected, as in "Urgent +2":${first}:VALUE: +${rest.length}:COUNT:`;
}
