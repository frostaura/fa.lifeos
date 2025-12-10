import toast from 'react-hot-toast';

interface ConfirmToastOptions {
  message: string;
  confirmText?: string;
  cancelText?: string;
}

/**
 * Display a toast-based confirmation dialog
 * @returns Promise that resolves to true if confirmed, false if cancelled
 */
export function confirmToast({
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
}: ConfirmToastOptions): Promise<boolean> {
  return new Promise((resolve) => {
    toast(
      (t) => (
        <div className="flex flex-col gap-3">
          <p className="text-sm text-white">{message}</p>
          <div className="flex gap-2 justify-end">
            <button
              onClick={() => {
                toast.dismiss(t.id);
                resolve(false);
              }}
              className="px-3 py-1.5 text-sm rounded-md bg-white/10 hover:bg-white/20 text-white transition-colors"
            >
              {cancelText}
            </button>
            <button
              onClick={() => {
                toast.dismiss(t.id);
                resolve(true);
              }}
              className="px-3 py-1.5 text-sm rounded-md bg-red-500 hover:bg-red-600 text-white transition-colors"
            >
              {confirmText}
            </button>
          </div>
        </div>
      ),
      {
        duration: Infinity,
        style: {
          background: '#1a1a2e',
          color: '#fff',
          border: '1px solid rgba(255, 255, 255, 0.1)',
          maxWidth: '400px',
        },
      }
    );
  });
}
