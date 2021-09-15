export class TextHelpers {
  static readonly tail = ' ...';
  static truncate(input: string, maxLength = 48) {
    if (input.length <= maxLength) {
      return input;
    }
    return input.substring(0, maxLength - this.tail.length) + this.tail;
  }
}

export const toWordCase = (value: string): string | undefined => {
  if (value === null || value === undefined) {
    return undefined;
  }

  const match = value.match(
    /[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+/g
  );

  if (!match) return undefined;

  return match
    .map((char) => char.toLowerCase())
    .map((word) =>
      word.replace(
        /\w\S*/g,
        (wordMatch) =>
          wordMatch.charAt(0).toUpperCase() +
          wordMatch.substring(1).toLowerCase()
      )
    )
    .join(' ');
};
