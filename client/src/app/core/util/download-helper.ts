export const downloadFile = async (
  file: Blob,
  filename: string
): Promise<void> => {
  const a = document.createElement('a');
  a.setAttribute('style', 'display: none');
  document.body.appendChild(a);

  return new Promise((resolve, reject) => {
    setTimeout(() => {
      try {
        const url = URL.createObjectURL(file);
        a.href = url;
        a.rel = 'noopener noreferrer';
        a.download = filename;
        a.dispatchEvent(
          new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
            view: window,
          })
        );
        setTimeout(() => URL.revokeObjectURL(url), revokeDelayMs);

        resolve();
      } catch (error: unknown) {
        reject(error);
      } finally {
        document.body.removeChild(a);
      }
    }, 0);
  });
};

// Safari cancels a download whose object URL is revoked in the same task as the click.
const revokeDelayMs = 1000;
