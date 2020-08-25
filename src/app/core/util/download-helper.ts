export const downloadFile = async (
  file: Blob,
  filename: string
): Promise<boolean> => {
  if (window.navigator && window.navigator.msSaveOrOpenBlob) {
    console.log('window.navigator.msSaveOrOpenBlob');

    return window.navigator.msSaveOrOpenBlob(file);
  }

  const a = document.createElement('a');
  a.setAttribute('style', 'display: none');
  document.body.appendChild(a);

  return new Promise((resovle) => {
    setTimeout(() => {
      const url = URL.createObjectURL(file);
      a.href = url;
      a.download = filename;
      a.dispatchEvent(
        new MouseEvent('click', {
          bubbles: true,
          cancelable: true,
          view: window,
        })
      );
      document.body.removeChild(a);

      resovle();
    }, 0);
  });
};
