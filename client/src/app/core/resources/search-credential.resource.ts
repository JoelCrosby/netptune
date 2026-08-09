import { httpResource } from '@angular/common/http';
import { SearchCredential } from '../models/search-credential';

export const searchCredentialUrl = 'api/ai/workspace-search-credential';

export const searchCredentialResource = () => {
  return httpResource<SearchCredential | null>(
    () => ({ url: searchCredentialUrl }),
    { defaultValue: null }
  );
};
