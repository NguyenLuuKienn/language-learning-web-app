import React, { useState, useEffect } from 'react';
import { App } from 'antd';
import { useTranslation } from '../../i18n';
import { db } from '../../offline/db';
import { syncApi } from '../../services/apiClient';
import { useAuth } from '../auth';
import type { SyncOperation } from '../../types';
import {
  CloudSyncOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  ReloadOutlined,
  LoadingOutlined,
  WifiOutlined,
  DisconnectOutlined
} from '@ant-design/icons';

export const SyncView: React.FC = () => {
  const { message } = App.useApp();
  const { t } = useTranslation();
  const { user } = useAuth();
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [pendingOps, setPendingOps] = useState<SyncOperation[]>([]);
  const [lastSynced, setLastSynced] = useState<string>('—');
  const [isSyncing, setIsSyncing] = useState(false);
  const [syncStatus, setSyncStatus] = useState<'idle' | 'success' | 'error'>('idle');

  const loadPendingQueue = async () => {
    try {
      const ops = await db.syncQueue.toArray();
      setPendingOps(ops);
    } catch {
      setPendingOps([]);
    }
  };

  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    loadPendingQueue();
    if (user?.id) {
      void db.syncMetadata.get(user.id).then((metadata) => setLastSynced(metadata?.lastSyncedAt ?? '—'));
    }

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, [user?.id]);

  const handleSyncNow = async () => {
    if (!isOnline) {
      message.warning(t('sync.statusOffline'));
      return;
    }

    setIsSyncing(true);
    setSyncStatus('idle');

    try {
      const localQueue = await db.syncQueue.where('status').equals('Pending').toArray();
      if (localQueue.length > 0) {
        const pushResult = await syncApi.pushOperations(localQueue);
        const acceptedIds = pushResult.results.filter((result) => result.status === 'Accepted').map((result) => result.operationId);
        await db.syncQueue.bulkDelete(acceptedIds);
        if (pushResult.results.some((result) => result.status !== 'Accepted')) {
          throw new Error('Some sync operations were rejected.');
        }
      }

      const metadata = user?.id ? await db.syncMetadata.get(user.id) : undefined;
      const cursor = metadata?.cursor;
      const pullResult = await syncApi.pullChanges(cursor);
      await db.transaction('rw', db.decks, db.cards, async () => {
        if (pullResult.updatedDecks.length > 0) {
          await db.decks.bulkPut(pullResult.updatedDecks.map((deck) => ({ ...deck, visibility: deck.isPublic ? 'Public' as const : 'Private' as const })));
        }
        if (pullResult.updatedCards.length > 0) await db.cards.bulkPut(pullResult.updatedCards);
      });
      const now = new Date().toISOString();
      if (user?.id) await db.syncMetadata.put({ userId: user.id, cursor: pullResult.newCursor, lastSyncedAt: now });
      setLastSynced(now);
      setSyncStatus('success');
      message.success(t('sync.syncSuccess'));
      await loadPendingQueue();
    } catch {
      setSyncStatus('error');
      message.error(t('sync.syncFailed'));
    } finally {
      setIsSyncing(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <h1 className="text-xl font-bold text-stone-900">{t('sync.title')}</h1>
        <p className="text-xs text-stone-600">{t('sync.subtitle')}</p>
      </div>

      <div
        className={`p-4 rounded-xl border-notebook flex items-center justify-between ${
          isOnline ? 'bg-emerald-50 border-emerald-300' : 'bg-amber-50 border-amber-300'
        }`}
      >
        <div className="flex items-center gap-3">
          {isOnline ? (
            <WifiOutlined className="text-xl text-emerald-600" />
          ) : (
            <DisconnectOutlined className="text-xl text-amber-600" />
          )}
          <div>
            <span
              className={`font-bold text-sm block ${
                isOnline ? 'text-emerald-900' : 'text-amber-900'
              }`}
            >
              {isOnline ? t('sync.statusOnline') : t('sync.statusOffline')}
            </span>
            <span className="text-xs text-stone-600 block">
              {isOnline
                ? 'Connected to local/remote server endpoint'
                : 'Offline changes queued safely in IndexedDB'}
            </span>
          </div>
        </div>

        <button
          onClick={handleSyncNow}
          disabled={!isOnline || isSyncing}
          className="px-4 py-2 bg-stone-900 hover:bg-stone-800 text-white rounded-lg text-xs font-bold notebook-btn flex items-center gap-2 transition cursor-pointer disabled:opacity-40"
        >
          {isSyncing ? (
            <>
              <LoadingOutlined />
              <span>{t('sync.syncing')}</span>
            </>
          ) : (
            <>
              <CloudSyncOutlined />
              <span>{t('sync.syncNow')}</span>
            </>
          )}
        </button>
      </div>

      {syncStatus === 'success' && (
        <div className="p-3 bg-emerald-100 border border-emerald-300 rounded-lg text-emerald-800 text-xs font-semibold flex items-center gap-2">
          <CheckCircleOutlined />
          <span>{t('sync.syncSuccess')}</span>
        </div>
      )}

      {syncStatus === 'error' && (
        <div className="p-3 bg-rose-100 border border-rose-300 rounded-lg text-rose-800 text-xs font-semibold flex items-center gap-2">
          <ExclamationCircleOutlined />
          <span>{t('sync.syncFailed')}</span>
        </div>
      )}

      <div className="grid grid-cols-2 gap-4">
        <div className="bg-white border-notebook rounded-xl p-4 shadow-xs">
          <span className="text-[11px] font-bold uppercase tracking-wider text-stone-400 block mb-1">
            {t('sync.pendingChanges')}
          </span>
          <span className="text-2xl font-black text-stone-900">{pendingOps.length}</span>
        </div>

        <div className="bg-white border-notebook rounded-xl p-4 shadow-xs">
          <span className="text-[11px] font-bold uppercase tracking-wider text-stone-400 block mb-1">
            {t('sync.lastSynced')}
          </span>
          <span className="text-sm font-bold text-stone-700 block mt-1">{lastSynced}</span>
        </div>
      </div>

      <div className="bg-white border-notebook-2 rounded-2xl p-5 shadow-xs space-y-3">
        <div className="flex items-center justify-between pb-2 border-b border-stone-200">
          <h3 className="font-bold text-sm text-stone-900">IndexedDB Sync Queue</h3>
          <button
            onClick={loadPendingQueue}
            className="text-stone-500 hover:text-stone-900 transition text-xs"
          >
            <ReloadOutlined />
          </button>
        </div>

        {pendingOps.length === 0 ? (
          <p className="text-xs text-stone-500 py-4 text-center">
            Queue is clear. All actions are synchronized with backend.
          </p>
        ) : (
          <div className="divide-y divide-stone-100 max-h-60 overflow-y-auto">
            {pendingOps.map((op) => (
              <div key={op.operationId} className="py-2.5 flex items-center justify-between text-xs">
                <div>
                  <span className="font-mono font-bold text-stone-800 block">{op.type}</span>
                  <span className="text-[10px] text-stone-400 font-mono block">
                    ID: {op.entityId}
                  </span>
                </div>
                <span className="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-amber-100 text-amber-800">
                  {op.status}
                </span>
              </div>
            ))}
          </div>
        )}

        <div className="pt-2 border-t border-stone-200 text-[11px] text-stone-500">
          {t('sync.conflictNotice')}
        </div>
      </div>
    </div>
  );
};
