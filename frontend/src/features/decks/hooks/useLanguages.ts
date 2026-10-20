
import { useQuery } from '@tanstack/react-query';

import { languagesApi } from '../api/languagesApi';

export function useLanguages() {
  return useQuery({
    queryKey: ['languages'],
    queryFn: languagesApi.getLanguages,
  });
}
