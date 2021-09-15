export const filterStringArray = (
  array: string[] | null,
  term: string
): string[] => {
  if (array === null) return [];

  if (typeof term !== 'string') {
    return array;
  }

  if (!array) {
    return array;
  }

  const filterValue = term.toLowerCase();

  return array.filter((item) => fuzzysearch(filterValue, item));
};

export const filterObjectArray = <T>(
  array: T[],
  prop: keyof T,
  term: string
): T[] => {
  if (typeof term !== 'string' || typeof prop !== 'string') {
    return array;
  }

  const filterValue = term.toLowerCase();

  return array.filter((item) =>
    fuzzysearch(filterValue, item[prop] as unknown as string)
  );
};

export const fuzzysearch = (needle: string, haystack: string) => {
  const haystackLC = haystack.toLowerCase();
  const needleLC = needle.toLowerCase();

  const hlen = haystack.length;
  const nlen = needleLC.length;

  if (nlen > hlen) {
    return false;
  }

  if (nlen === hlen) {
    return needleLC === haystackLC;
  }

  outer: for (let i = 0, j = 0; i < nlen; i++) {
    const nch = needleLC.charCodeAt(i);

    while (j < hlen) {
      if (haystackLC.charCodeAt(j++) === nch) {
        continue outer;
      }
    }

    return false;
  }

  return true;
};
