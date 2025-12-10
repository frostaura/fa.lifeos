import * as signalR from '@microsoft/signalr';

export interface SignalRConnection {
  connection: signalR.HubConnection;
  start: () => Promise<void>;
  stop: () => Promise<void>;
}

export const createHubConnection = (accessToken: string): SignalRConnection => {
  const backendUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001';
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${backendUrl}/notifications`, {
      accessTokenFactory: () => accessToken,
      transport: signalR.HttpTransportType.WebSockets,
      skipNegotiation: false,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(
      import.meta.env.DEV ? signalR.LogLevel.Information : signalR.LogLevel.Warning
    )
    .build();

  const start = async () => {
    try {
      await connection.start();
      console.log('[SignalR] Connected successfully');
    } catch (err) {
      console.error('[SignalR] Connection failed:', err);
    }
  };

  const stop = async () => {
    try {
      await connection.stop();
      console.log('[SignalR] Disconnected');
    } catch (err) {
      console.error('[SignalR] Disconnect error:', err);
    }
  };

  return { connection, start, stop };
};
