import { useState, useEffect } from 'react';
import api from '../api/axios';
import toast from 'react-hot-toast';

interface TAReport {
  taName: string;
  taEmail: string;
  totalSessions: number;
  completedSessions: number;
  upcomingSessions: number;
  percentage: number;
}

export default function Reports() {
  const [reports, setReports] = useState<TAReport[]>([]);
  const [loading, setLoading] = useState(false);
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date(); d.setMonth(d.getMonth() - 1); return d.toISOString().split('T')[0];
  });
  const [toDate, setToDate] = useState(() => new Date().toISOString().split('T')[0]);

  const loadReport = async () => {
    setLoading(true);
    try {
      const res = await api.get(`/dashboard/report?fromDate=${fromDate}&toDate=${toDate}`);
      setReports(res.data.taReports);
    } catch (err) {
      toast.error('Failed to load report');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadReport(); }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Reports</h1>

      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex items-end gap-4 mb-6">
          <div>
            <label className="block text-sm font-medium text-gray-700">From</label>
            <input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)}
              className="mt-1 block border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">To</label>
            <input type="date" value={toDate} onChange={e => setToDate(e.target.value)}
              className="mt-1 block border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
          </div>
          <button onClick={loadReport} disabled={loading}
            className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50">
            {loading ? 'Loading...' : 'Generate Report'}
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">TA Name</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Total</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Completed</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Upcoming</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Completion %</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {reports.map((r, i) => (
                <tr key={i}>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">{r.taName}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{r.taEmail}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">{r.totalSessions}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-green-600">{r.completedSessions}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-blue-600">{r.upcomingSessions}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">
                    <div className="flex items-center">
                      <div className="w-16 bg-gray-200 rounded-full h-2 mr-2">
                        <div className="bg-indigo-600 h-2 rounded-full" style={{width: `${r.percentage}%`}} />
                      </div>
                      <span>{r.percentage}%</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
