import { RouterProvider } from 'react-router-dom';
import { Provider } from 'react-redux';
import { store } from '@store/index';
import { router } from './router';
import { Toaster } from 'react-hot-toast';
import { useProjectionUpdates } from '@/hooks/useProjectionUpdates';

function AppContent() {
  useProjectionUpdates();
  
  return (
    <>
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 3000,
          style: {
            background: '#1a1a2e',
            color: '#fff',
            border: '1px solid rgba(255, 255, 255, 0.1)',
          },
          success: {
            iconTheme: {
              primary: '#10b981',
              secondary: '#fff',
            },
          },
          error: {
            iconTheme: {
              primary: '#ef4444',
              secondary: '#fff',
            },
          },
        }}
      />
      <RouterProvider router={router} />
    </>
  );
}

function App() {
  return (
    <Provider store={store}>
      <AppContent />
    </Provider>
  );
}

export default App;
