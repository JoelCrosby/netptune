const byteUnits = ['B', 'KiB', 'MiB', 'GiB', 'TiB'];

export function formatBytes(bytes: number): string {
  if (bytes === 0) {
    return '0 B';
  }

  const index = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    byteUnits.length - 1
  );

  return `${(bytes / 1024 ** index).toFixed(index ? 1 : 0)} ${byteUnits[index]}`;
}
