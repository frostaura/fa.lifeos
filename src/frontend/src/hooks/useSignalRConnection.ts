import { useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import type { RootState } from '@/store';
import { createHubConnection } from '@/services/signalr';
import type { HubConnection } from '@microsoft/signalr';

export const useSignalRConnection = (): HubConnection | null => {
  const token = useSelector((state: RootState) => state.auth.token);
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    if (!token) {
      console.log('[SignalR] No token available, skipping connection');
      return;
    }

    const { connection: hubConnection, start, stop } = createHubConnection(token);

    start().then(() => setConnection(hubConnection));

    return () => {
      stop();
      setConnection(null);
    };
  }, [token]);

  return connection;
};
