
import { apiRequest } from '../../../services/apiClient';

export interface LanguageOption {
  id: string;
  name: string;
}

export const languagesApi = {
  getLanguages: async (): Promise<LanguageOption[]> => {
    return apiRequest<LanguageOption[]>('/languages');
  },
};
