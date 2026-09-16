import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import api from '../api/axios';
import toast from 'react-hot-toast';

interface Assignment {
  id: string;
  quizCourseName: string;
  quizDate: string;
  quizTime: string;
  roomName: string;
  taName: string;
  taEmail: string;
  isBackup: boolean;
  status: string;
}

export default function Assignments() {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => { loadAssignments(); }, []);

  const loadAssignments = async () => {
    try {
      const res = isAdmin
        ? await api.get('/assignments')
        : await api.get('/assignments/my');
      setAssignments(res.data);
    } catch (err) {
      toast.error('Failed to load assignments');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="text-center py-8">Loading...</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">{isAdmin ? 'All Assignments' : 'My Assignments'}</h1>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Course</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Time</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Room</th>
              {isAdmin && <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">TA</th>}
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {assignments.map(a => (
              <tr key={a.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{a.quizCourseName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{a.quizDate}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{a.quizTime}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{a.roomName}</td>
                {isAdmin && <td className="px-6 py-4 whitespace-nowrap text-sm">{a.taName}</td>}
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <span className={`px-2 py-1 text-xs rounded-full ${a.isBackup ? 'bg-yellow-100 text-yellow-800' : 'bg-green-100 text-green-800'}`}>
                    {a.isBackup ? 'Backup' : 'Primary'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <span className={`px-2 py-1 text-xs rounded-full ${
                    a.status === 'Assigned' ? 'bg-blue-100 text-blue-800' :
                    a.status === 'Completed' ? 'bg-green-100 text-green-800' :
                    a.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>{a.status}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
