import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import api from '../api/axios';

interface WorkloadSummary {
  currentSemesterSessions: number;
  targetWorkload: number;
  remainingSessions: number;
  workloadPercentage: number;
}

interface SystemDashboard {
  totalTAs: number;
  activeTAs: number;
  totalQuizzes: number;
  upcomingQuizzes: number;
  assignedQuizzes: number;
  unassignedQuizzes: number;
}

export default function Dashboard() {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const [workload, setWorkload] = useState<WorkloadSummary | null>(null);
  const [systemData, setSystemData] = useState<SystemDashboard | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      if (isAdmin) {
        const res = await api.get('/dashboard');
        setSystemData(res.data);
      } else {
        const semesters = await api.get('/semesters');
        const activeSemester = semesters.data.find((s: any) => s.isActive);
        if (activeSemester && user) {
          const res = await api.get(`/dashboard/workload/${user.id}?semesterId=${activeSemester.id}`);
          setWorkload(res.data);
        }
      }
    } catch (err) {
      console.error('Failed to load dashboard', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="text-center py-8">Loading...</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {isAdmin && systemData && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-medium text-gray-900">TAs</h3>
            <p className="mt-2 text-3xl font-bold text-indigo-600">{systemData.activeTAs}</p>
            <p className="text-sm text-gray-500">of {systemData.totalTAs} total</p>
          </div>
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-medium text-gray-900">Upcoming Quizzes</h3>
            <p className="mt-2 text-3xl font-bold text-indigo-600">{systemData.upcomingQuizzes}</p>
            <p className="text-sm text-gray-500">{systemData.unassignedQuizzes} unassigned</p>
          </div>
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-medium text-gray-900">Assignments</h3>
            <p className="mt-2 text-3xl font-bold text-indigo-600">{systemData.assignedQuizzes}</p>
            <p className="text-sm text-gray-500">total assignments</p>
          </div>
        </div>
      )}

      {!isAdmin && workload && (
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Your Workload</h3>
          <div className="grid grid-cols-2 gap-6">
            <div>
              <p className="text-sm text-gray-500">Completed Sessions</p>
              <p className="text-2xl font-bold text-indigo-600">{workload.currentSemesterSessions}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Target</p>
              <p className="text-2xl font-bold text-gray-900">{workload.targetWorkload}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Remaining</p>
              <p className="text-2xl font-bold text-green-600">{workload.remainingSessions}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">Progress</p>
              <div className="mt-2">
                <div className="bg-gray-200 rounded-full h-4">
                  <div
                    className="bg-indigo-600 h-4 rounded-full"
                    style={{ width: `${Math.min(workload.workloadPercentage, 100)}%` }}
                  />
                </div>
                <p className="text-sm text-gray-500 mt-1">{workload.workloadPercentage}%</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
