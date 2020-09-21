export type Selected<T> = {
  [K in keyof T]: T[K];
} & {
  selected: boolean;
};
