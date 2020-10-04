import { HttpHeaders } from '@angular/common/http';

const defaultFilename = 'unknown-filename';

export const extractFilenameFromHeaders = (headers: HttpHeaders): string => {
  const disposition = headers.get('Content-Disposition');

  if (!disposition) {
    console.warn(
      '[FileResponse] Content-Disposition did not exist in reponse.'
    );

    return defaultFilename;
  }

  if (disposition && disposition.indexOf('attachment') !== -1) {
    const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
    const matches = filenameRegex.exec(disposition);

    if (matches != null && matches[1]) {
      return matches[1].replace(/['"]/g, '');
    }

    console.warn(
      '[FileResponse] Content-Disposition header did not contain the filename key.'
    );
  }

  return defaultFilename;
};
