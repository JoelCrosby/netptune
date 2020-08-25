export const downloadFile = (file: Blob, filename: string) => {
  if (window.navigator && window.navigator.msSaveOrOpenBlob) {
    console.log('window.navigator.msSaveOrOpenBlob');

    return window.navigator.msSaveOrOpenBlob(file);
  }

  const a = document.createElement('a');
  a.setAttribute('style', 'display: none');
  document.body.appendChild(a);

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
  }, 0);
};
