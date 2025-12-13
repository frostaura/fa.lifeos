import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Edit2, Trash2, ChevronLeft, TrendingUp } from 'lucide-react';
import { GlassCard } from '@components/atoms/GlassCard';
import { Button } from '@components/atoms/Button';
import { Spinner } from '@components/atoms/Spinner';
import { confirmToast } from '@utils/confirmToast';
import toast from 'react-hot-toast';
import {
  useGetMetricRecordsQuery,
  useUpdateMetricRecordMutation,
  useDeleteMetricRecordMutation,
  useGetMetricDefinitionsQuery,
  type MetricRecord,
  type UpdateMetricRecordRequest,
} from '@/services';

// Record Modal Component (same as before, but extracted)
interface RecordModalProps {
  isOpen: boolean;
  onClose: () => void;
  record: MetricRecord | null;
  onSubmit: (data: UpdateMetricRecordRequest & { id: string }) => Promise<void>;
  isLoading: boolean;
}

function RecordModal({ isOpen, onClose, record, onSubmit, isLoading }: RecordModalProps) {
  const [value, setValue] = useState(record?.value?.toString() || '');
  const [notes, setNotes] = useState(record?.notes || '');

  if (!isOpen || !record) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit({
      id: record.id,
      valueNumber: parseFloat(value),
      notes: notes || undefined,
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-background-secondary border border-glass-border rounded-2xl p-6 w-full max-w-md">
        <h3 className="text-lg font-semibold text-text-primary mb-4">Edit Record</h3>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-text-secondary mb-1">Value</label>
            <input
              type="number"
              step="any"
              value={value}
              onChange={(e) => setValue(e.target.value)}
              className="w-full px-3 py-2 bg-background-primary border border-glass-border rounded-lg text-text-primary"
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-text-secondary mb-1">Notes</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full px-3 py-2 bg-background-primary border border-glass-border rounded-lg text-text-primary"
              rows={3}
            />
          </div>
          <div className="flex gap-2 justify-end">
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Saving...' : 'Save'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function MetricRecords() {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [editingRecord, setEditingRecord] = useState<MetricRecord | null>(null);

  // Get the metric definition
  const { data: definitions } = useGetMetricDefinitionsQuery();
  const definition = definitions?.find((d: any) => d.code === code);

  // Get records
  const { data: recordsData, isLoading, error, refetch } = useGetMetricRecordsQuery(
    {
      code: code!,
      page,
      pageSize,
    },
    { skip: !code }
  );

  const [updateRecord, { isLoading: isUpdating }] = useUpdateMetricRecordMutation();
  const [deleteRecord] = useDeleteMetricRecordMutation();

  const handleUpdateRecord = async (data: UpdateMetricRecordRequest & { id: string }) => {
    try {
      await updateRecord(data).unwrap();
      toast.success('Record updated successfully');
      refetch();
    } catch (error: any) {
      toast.error(error?.data?.message || 'Failed to update record');
    }
  };

  const handleDeleteRecord = async (id: string) => {
    const confirmed = await confirmToast({
      message: 'Are you sure you want to delete this record?',
    });
    if (confirmed) {
      try {
        await deleteRecord(id).unwrap();
        toast.success('Record deleted successfully');
        refetch();
      } catch (error: any) {
        toast.error(error?.data?.message || 'Failed to delete record');
      }
    }
  };

  const totalPages = recordsData?.meta?.totalPages || 1;

  if (!definition) {
    return (
      <div className="flex flex-col h-full items-center justify-center">
        <Spinner size="lg" />
        <p className="mt-4 text-text-secondary">Loading metric definition...</p>
      </div>
    );
  }

  // Calculate stats
  const records = recordsData?.data || [];
  const avgValue = records.length > 0
    ? records.reduce((sum: number, r: any) => sum + r.value, 0) / records.length
    : 0;
  const minValue = records.length > 0 ? Math.min(...records.map((r: any) => r.value)) : 0;
  const maxValue = records.length > 0 ? Math.max(...records.map((r: any) => r.value)) : 0;

  return (
    <div className="flex flex-col h-full">
      {/* Sticky Header */}
      <div className="flex-shrink-0 sticky top-0 z-20 bg-background-primary/95 backdrop-blur-md border-b border-glass-border rounded-b-xl mb-6">
        <div className="py-4">
          <div className="flex items-center gap-3 mb-2">
            <button
              onClick={() => navigate('/metrics')}
              className="p-2 rounded-lg hover:bg-background-hover transition-colors text-text-secondary"
            >
              <ChevronLeft className="w-5 h-5" />
            </button>
            <div>
              <h1 className="text-xl md:text-2xl lg:text-3xl font-bold text-text-primary">{definition.name}</h1>
              <p className="text-text-secondary mt-1 text-xs md:text-sm">
                {definition.code} • {definition.unit} • {recordsData?.meta?.total || 0} records
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="flex-1 flex flex-col gap-6 px-4 md:px-6 pb-8 min-h-0">
        {/* Stats Cards */}
        {records.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <GlassCard variant="default" className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-accent-purple/10">
                  <TrendingUp className="w-5 h-5 text-accent-purple" />
                </div>
                <div>
                  <p className="text-xs text-text-tertiary">Average</p>
                  <p className="text-lg font-semibold text-accent-purple">
                    {avgValue.toFixed(2)} {definition.unit}
                  </p>
                </div>
              </div>
            </GlassCard>
            <GlassCard variant="default" className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-semantic-success/10">
                  <TrendingUp className="w-5 h-5 text-semantic-success" />
                </div>
                <div>
                  <p className="text-xs text-text-tertiary">Maximum</p>
                  <p className="text-lg font-semibold text-semantic-success">
                    {maxValue} {definition.unit}
                  </p>
                </div>
              </div>
            </GlassCard>
            <GlassCard variant="default" className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-accent-cyan/10">
                  <TrendingUp className="w-5 h-5 text-accent-cyan" />
                </div>
                <div>
                  <p className="text-xs text-text-tertiary">Minimum</p>
                  <p className="text-lg font-semibold text-accent-cyan">
                    {minValue} {definition.unit}
                  </p>
                </div>
              </div>
            </GlassCard>
          </div>
        )}

        {/* Records Table */}
        <GlassCard variant="default" className="p-6 flex-1 flex flex-col min-h-0">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-text-primary">Record History</h2>
          </div>

          {isLoading ? (
            <div className="flex items-center justify-center flex-1">
              <Spinner size="lg" />
            </div>
          ) : error ? (
            <div className="text-center py-8 text-semantic-error">Failed to load records</div>
          ) : recordsData?.data.length === 0 ? (
            <div className="text-center py-8 text-text-tertiary flex-1 flex items-center justify-center">
              <div>
                <p className="text-lg">No records yet</p>
                <p className="text-sm mt-2">Record values from the Metrics playground to see them here</p>
              </div>
            </div>
          ) : (
            <>
              <div className="overflow-x-auto flex-1">
                <table className="w-full">
                  <thead className="sticky top-0 z-10">
                    <tr className="border-b border-glass-border">
                      <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary">Recorded At</th>
                      <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary">Value</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary">Source</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-text-secondary">Notes</th>
                      <th className="text-right py-3 px-4 text-sm font-medium text-text-secondary">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-glass-border">
                    {recordsData?.data.map((record: any, index: number) => {
                      // Alternate colors for visual appeal
                      const colorClass = index % 2 === 0 ? 'text-accent-purple' : 'text-accent-cyan';
                      
                      return (
                        <tr key={record.id} className="hover:bg-background-hover/50">
                          <td className="py-2 px-4 text-sm text-text-primary">
                            {new Date(record.recordedAt).toLocaleString(undefined, {
                              month: 'short',
                              day: 'numeric',
                              year: 'numeric',
                              hour: '2-digit',
                              minute: '2-digit'
                            })}
                          </td>
                          <td className={`py-2 px-4 text-sm text-right font-mono font-semibold ${colorClass}`}>
                            {record.value} {definition.unit}
                          </td>
                          <td className="py-2 px-4 text-sm">
                            <span className="px-2 py-1 rounded-full bg-accent-cyan/10 border border-accent-cyan/20 text-xs text-accent-cyan">
                              {record.source || 'unknown'}
                            </span>
                          </td>
                          <td className="py-2 px-4 text-sm text-text-tertiary max-w-xs truncate">
                            {record.notes || '—'}
                          </td>
                          <td className="py-2 px-4 text-right">
                            <div className="flex items-center justify-end gap-2">
                              <button
                                onClick={() => setEditingRecord(record)}
                                className="p-1.5 rounded-lg hover:bg-background-hover transition-colors text-text-secondary hover:text-accent-purple"
                              >
                                <Edit2 className="w-4 h-4" />
                              </button>
                              <button
                                onClick={() => handleDeleteRecord(record.id)}
                                className="p-1.5 rounded-lg hover:bg-background-hover transition-colors text-text-secondary hover:text-semantic-error"
                              >
                                <Trash2 className="w-4 h-4" />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-center gap-2 mt-4">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1}
                  >
                    Previous
                  </Button>
                  <span className="text-sm text-text-secondary">
                    Page {page} of {totalPages}
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages}
                  >
                    Next
                  </Button>
                </div>
              )}
            </>
          )}
        </GlassCard>
      </div>

      {/* Record Edit Modal */}
      <RecordModal
        isOpen={!!editingRecord}
        onClose={() => setEditingRecord(null)}
        record={editingRecord}
        onSubmit={handleUpdateRecord}
        isLoading={isUpdating}
      />
    </div>
  );
}
