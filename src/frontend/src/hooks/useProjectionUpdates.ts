import { useEffect } from 'react';
import { useDispatch } from 'react-redux';
import { useSignalRConnection } from './useSignalRConnection';
import { apiSlice } from '@/store/api/apiSlice';
import toast from 'react-hot-toast';

interface ProjectionsUpdatedPayload {
  scenarioId: string;
  timestamp: string;
}

interface CalculationProgressPayload {
  mainMessage: string;
  subStep: string;
}

export const useProjectionUpdates = () => {
  const connection = useSignalRConnection();
  const dispatch = useDispatch();

  useEffect(() => {
    if (!connection) return;

    const handleProjectionsUpdated = (payload: ProjectionsUpdatedPayload) => {
      console.log('[SignalR] Projections updated:', payload);
      
      dispatch(apiSlice.util.invalidateTags(['FinancialGoals', 'Dashboard', 'Scenarios']));
      
      toast.dismiss('projections-calculating');
      toast.success('Projections updated! 🎉', { duration: 4000 });
    };

    const handleJobFailed = (payload: { jobId: string; error: string }) => {
      console.error('[SignalR] Job failed:', payload);
      toast.dismiss('projections-calculating');
      toast.error(`Failed to update projections: ${payload.error}`);
    };

    const handleCalculationProgress = (payload: CalculationProgressPayload) => {
      console.log('[SignalR] Calculation progress:', payload);
      toast.loading(`${payload.mainMessage} (${payload.subStep})`, {
        id: 'projections-calculating',
        duration: 60000
      });
    };

    connection.on('ProjectionsUpdated', handleProjectionsUpdated);
    connection.on('JobFailed', handleJobFailed);
    connection.on('CalculationProgress', handleCalculationProgress);

    return () => {
      connection.off('ProjectionsUpdated', handleProjectionsUpdated);
      connection.off('JobFailed', handleJobFailed);
      connection.off('CalculationProgress', handleCalculationProgress);
    };
  }, [connection, dispatch]);
};
