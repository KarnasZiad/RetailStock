import { useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { API_BASE } from './api';
import type { StockAlertMessage, StockChangedMessage, TransferUpdatedMessage } from './types';

export type HubStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

export interface StockHubHandlers {
  onStockChanged: (message: StockChangedMessage) => void;
  onStockAlert: (message: StockAlertMessage) => void;
  onTransferUpdated: (message: TransferUpdatedMessage) => void;
  /** Called after a reconnection: events may have been missed, reload the state. */
  onResync: () => void;
}

/** Connects to the SignalR hub and subscribes to every store (head-office dashboard). */
export function useStockHub(handlers: StockHubHandlers): HubStatus {
  const [status, setStatus] = useState<HubStatus>('connecting');
  const handlersRef = useRef(handlers);
  handlersRef.current = handlers;

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/stock`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('StockChanged', (m: StockChangedMessage) => handlersRef.current.onStockChanged(m));
    connection.on('StockAlert', (m: StockAlertMessage) => handlersRef.current.onStockAlert(m));
    connection.on('TransferUpdated', (m: TransferUpdatedMessage) => handlersRef.current.onTransferUpdated(m));

    connection.onreconnecting(() => setStatus('reconnecting'));
    connection.onreconnected(async () => {
      await connection.invoke('SubscribeAll');
      setStatus('connected');
      handlersRef.current.onResync();
    });
    connection.onclose(() => setStatus('disconnected'));

    let disposed = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    const start = async () => {
      try {
        await connection.start();
        if (disposed) return;
        await connection.invoke('SubscribeAll');
        setStatus('connected');
      } catch {
        if (disposed) return;
        setStatus('disconnected');
        retry = setTimeout(start, 5000);
      }
    };
    void start();

    return () => {
      disposed = true;
      clearTimeout(retry);
      void connection.stop();
    };
  }, []);

  return status;
}
