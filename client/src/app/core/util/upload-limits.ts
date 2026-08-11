const mebibyte = 1024 * 1024;

export const defaultMaxUploadBytes = 50 * mebibyte;

export const minimumMaxUploadBytes = 1 * mebibyte;

export const maximumMaxUploadBytes = 512 * mebibyte;

export const maxUploadPresets = [5, 10, 25, 50, 100, 256, 512].map(
  (megabytes) => megabytes * mebibyte
);
