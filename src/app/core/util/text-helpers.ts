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
  if (!value) {
    return undefined;
  }

  return value
    .match(/[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+/g)
    .map((char) => char.toLowerCase())
    .map((word) =>
      word.replace(
        /\w\S*/g,
        (match) =>
          match.charAt(0).toUpperCase() + match.substring(1).toLowerCase()
      )
    )
    .join(' ');
};
