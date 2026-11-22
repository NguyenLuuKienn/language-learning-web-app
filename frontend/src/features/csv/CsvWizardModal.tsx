import React, { useState } from 'react';
import { message } from 'antd';
import { useTranslation } from '../../i18n';
import { csvApi } from '../../services/apiClient';
import type { CsvPreviewResponse } from '../../types';
import {
  UploadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  CloseOutlined,
  LoadingOutlined
} from '@ant-design/icons';

interface CsvWizardModalProps {
  deckId: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const CsvWizardModal: React.FC<CsvWizardModalProps> = ({
  deckId,
  isOpen,
  onClose,
  onSuccess
}) => {
  const { t } = useTranslation();
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<CsvPreviewResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isImporting, setIsImporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;

    setFile(selected);
    setError(null);
    setIsLoading(true);

    try {
      const result = await csvApi.previewCsv(deckId, selected);
      setPreview(result);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : t('common.error');
      setError(msg);
      message.error(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCommit = async () => {
    if (!preview || preview.validRows === 0) return;

    setIsImporting(true);
    setError(null);

    try {
      const validCards = preview.items
        .filter((item) => item.isValid)
        .map((item) => ({
          front: item.front,
          back: item.back,
          tags: item.tags || []
        }));

      await csvApi.commitCsvImport(deckId, validCards);
      message.success(t('csv.importSuccess'));
      onSuccess();
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : t('common.error');
      setError(msg);
      message.error(msg);
    } finally {
      setIsImporting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
      <div className="w-full max-w-2xl bg-[#FAF6EE] border-notebook-2 shadow-2xl rounded-2xl p-6 relative max-h-[90vh] flex flex-col">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-stone-500 hover:text-stone-900 transition"
        >
          <CloseOutlined className="text-lg" />
        </button>

        <div>
          <h2 className="text-lg font-bold text-stone-900">{t('csv.title')}</h2>
          <p className="text-xs text-stone-600 mb-2">{t('csv.subtitle')}</p>
          {file && (
            <span className="text-[11px] font-mono font-bold text-stone-500 bg-stone-100 px-2 py-0.5 rounded border border-stone-300 inline-block mb-3">
              {file.name}
            </span>
          )}
        </div>

        {error && (
          <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded-lg text-red-700 text-xs font-semibold">
            {error}
          </div>
        )}

        {!preview && (
          <div className="border-2 border-dashed border-stone-300 rounded-xl p-8 text-center hover:bg-stone-50 transition cursor-pointer my-4">
            <UploadOutlined className="text-3xl text-stone-400 mb-2" />
            <p className="text-xs font-bold text-stone-700 mb-1">{t('csv.dropzoneHint')}</p>
            <p className="text-[11px] text-stone-500 mb-3">.csv UTF-8</p>
            <input
              type="file"
              accept=".csv"
              onChange={handleFileChange}
              className="text-xs text-stone-500 file:mr-4 file:py-1.5 file:px-3 file:rounded-lg file:border-notebook file:text-xs file:font-bold file:bg-white file:text-stone-800 hover:file:bg-stone-100 cursor-pointer"
            />
          </div>
        )}

        {isLoading && (
          <div className="py-8 text-center">
            <LoadingOutlined className="text-2xl text-stone-500 animate-spin" />
            <p className="text-xs text-stone-500 mt-2">{t('common.loading')}</p>
          </div>
        )}

        {preview && (
          <div className="flex-1 overflow-hidden flex flex-col space-y-4">
            <div className="grid grid-cols-3 gap-3 text-center">
              <div className="p-2.5 bg-white border-notebook rounded-lg">
                <span className="text-[10px] uppercase font-bold text-stone-400 block">
                  {t('csv.totalRows')}
                </span>
                <span className="font-bold text-sm text-stone-900">{preview.totalRows}</span>
              </div>
              <div className="p-2.5 bg-emerald-50 border border-emerald-300 rounded-lg">
                <span className="text-[10px] uppercase font-bold text-emerald-600 block">
                  {t('csv.validRows')}
                </span>
                <span className="font-bold text-sm text-emerald-900">{preview.validRows}</span>
              </div>
              <div className="p-2.5 bg-rose-50 border border-rose-300 rounded-lg">
                <span className="text-[10px] uppercase font-bold text-rose-600 block">
                  {t('csv.invalidRows')}
                </span>
                <span className="font-bold text-sm text-rose-900">{preview.invalidRows}</span>
              </div>
            </div>

            <div className="flex-1 overflow-y-auto border-notebook bg-white rounded-xl divide-y divide-stone-200">
              <table className="w-full text-left text-xs">
                <thead className="bg-stone-50 text-stone-600 font-bold sticky top-0 border-b border-stone-200">
                  <tr>
                    <th className="p-2.5 w-12">#</th>
                    <th className="p-2.5">{t('csv.frontHeader')}</th>
                    <th className="p-2.5">{t('csv.backHeader')}</th>
                    <th className="p-2.5 w-24">{t('csv.statusHeader')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100">
                  {preview.items.map((row) => (
                    <tr key={row.rowNumber} className={row.isValid ? '' : 'bg-rose-50/50'}>
                      <td className="p-2.5 font-mono text-stone-400">{row.rowNumber}</td>
                      <td className="p-2.5 font-medium text-stone-900">{row.front}</td>
                      <td className="p-2.5 text-stone-600">{row.back}</td>
                      <td className="p-2.5">
                        {row.isValid ? (
                          <span className="text-emerald-600 flex items-center gap-1 font-bold">
                            <CheckCircleOutlined /> Valid
                          </span>
                        ) : (
                          <span
                            className="text-rose-600 flex items-center gap-1 font-bold"
                            title={row.errorMessage}
                          >
                            <CloseCircleOutlined /> Error
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex gap-3 pt-2">
              <button
                onClick={() => setPreview(null)}
                className="py-2.5 px-4 border-notebook rounded-lg bg-white hover:bg-stone-100 text-xs font-bold text-stone-700 transition"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleCommit}
                disabled={isImporting || preview.validRows === 0}
                className="flex-1 py-2.5 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-xs font-bold notebook-btn transition cursor-pointer disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {isImporting ? <LoadingOutlined /> : t('csv.commitImport')}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
