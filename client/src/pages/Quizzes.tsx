import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import api from '../api/axios';
import toast from 'react-hot-toast';

interface Quiz {
  id: string;
  courseName: string;
  quizDate: string;
  startTime: string;
  endTime: string;
  weekNumber: number;
  slotNumber: number;
  group: string;
  status: string;
  autoAssign: boolean;
  addBackup: boolean;
  locations: any[];
}

export default function Quizzes() {
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const [quizzes, setQuizzes] = useState<Quiz[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [showImport, setShowImport] = useState(false);

  useEffect(() => { loadQuizzes(); }, []);

  const loadQuizzes = async () => {
    try {
      const res = await api.get('/quizzes');
      setQuizzes(res.data);
    } catch (err) {
      toast.error('Failed to load quizzes');
    } finally {
      setLoading(false);
    }
  };

  const autoAssign = async (quizId: string) => {
    try {
      await api.post(`/assignments/auto-assign`, { quizId });
      toast.success('Proctors assigned successfully');
      loadQuizzes();
    } catch (err) {
      toast.error('Failed to assign proctors');
    }
  };

  if (loading) return <div className="text-center py-8">Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900">Quizzes</h1>
        {isAdmin && (
          <div className="space-x-3">
            <button
              onClick={() => setShowCreate(true)}
              className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700"
            >
              Create Quiz
            </button>
            <button
              onClick={() => setShowImport(true)}
              className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
            >
              Import Excel
            </button>
          </div>
        )}
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Week</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Course</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Slot</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Group</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Locations</th>
              {isAdmin && <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {quizzes.map((quiz) => (
              <tr key={quiz.id}>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className="px-2 py-1 text-xs rounded-full bg-gray-100 text-gray-800">
                    Week {quiz.weekNumber}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">{quiz.courseName}</td>
                <td className="px-6 py-4 whitespace-nowrap">{quiz.quizDate}</td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {quiz.slotNumber === 0 ? 'Gap' : `${quiz.slotNumber}${quiz.slotNumber === 1 ? 'ST' : quiz.slotNumber === 2 ? 'ND' : quiz.slotNumber === 3 ? 'RD' : 'TH'}`}
                  <br />
                  <span className="text-xs text-gray-500">{quiz.startTime} - {quiz.endTime}</span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className="px-2 py-1 text-xs rounded-full bg-purple-100 text-purple-800">
                    {quiz.group}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 py-1 text-xs rounded-full ${
                    quiz.status === 'Upcoming' ? 'bg-blue-100 text-blue-800' :
                    quiz.status === 'Completed' ? 'bg-green-100 text-green-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>
                    {quiz.status}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {quiz.locations.map((l: any) => l.roomName).join(', ')}
                </td>
                {isAdmin && (
                  <td className="px-6 py-4 whitespace-nowrap space-x-2">
                    <button
                      onClick={() => autoAssign(quiz.id)}
                      className="text-indigo-600 hover:text-indigo-900 text-sm"
                    >
                      Auto-Assign
                    </button>
                    <button className="text-gray-600 hover:text-gray-900 text-sm">
                      View
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && (
        <CreateQuizModal onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); loadQuizzes(); }} />
      )}
      {showImport && (
        <ImportQuizModal onClose={() => setShowImport(false)} onImported={() => { setShowImport(false); loadQuizzes(); }} />
      )}
    </div>
  );
}

function CreateQuizModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [courses, setCourses] = useState<any[]>([]);
  const [form, setForm] = useState({
    courseId: '', semesterId: '', quizDate: '', startTime: '', endTime: '',
    autoAssign: false, addBackup: false,
    locations: [{ roomName: '', proctorsNeeded: 1 }]
  });

  useEffect(() => {
    api.get('/courses').then(res => setCourses(res.data));
    api.get('/semesters').then(res => {
      const active = res.data.find((s: any) => s.isActive);
      if (active) setForm(f => ({ ...f, semesterId: active.id }));
    });
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.post('/quizzes', form);
      toast.success('Quiz created');
      onCreated();
    } catch (err) {
      toast.error('Failed to create quiz');
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h2 className="text-xl font-bold mb-4">Create Quiz</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Course</label>
            <select value={form.courseId} onChange={e => setForm({...form, courseId: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border">
              <option value="">Select course</option>
              {courses.map((c: any) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Date</label>
            <input type="date" value={form.quizDate} onChange={e => setForm({...form, quizDate: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">Start Time</label>
              <input type="time" value={form.startTime} onChange={e => setForm({...form, startTime: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">End Time</label>
              <input type="time" value={form.endTime} onChange={e => setForm({...form, endTime: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Locations</label>
            {form.locations.map((loc, i) => (
              <div key={i} className="flex gap-2 mt-2">
                <input placeholder="Room" value={loc.roomName} onChange={e => {
                  const locs = [...form.locations]; locs[i].roomName = e.target.value; setForm({...form, locations: locs});
                }} className="flex-1 border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
                <input type="number" placeholder="Proctors" value={loc.proctorsNeeded} onChange={e => {
                  const locs = [...form.locations]; locs[i].proctorsNeeded = parseInt(e.target.value) || 1; setForm({...form, locations: locs});
                }} className="w-20 border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
              </div>
            ))}
            <button type="button" onClick={() => setForm({...form, locations: [...form.locations, {roomName: '', proctorsNeeded: 1}]})}
              className="mt-2 text-sm text-indigo-600 hover:text-indigo-800">
              + Add Location
            </button>
          </div>
          <div className="flex items-center space-x-4">
            <label className="flex items-center">
              <input type="checkbox" checked={form.autoAssign} onChange={e => setForm({...form, autoAssign: e.target.checked})} className="rounded" />
              <span className="ml-2 text-sm">Auto-assign</span>
            </label>
            <label className="flex items-center">
              <input type="checkbox" checked={form.addBackup} onChange={e => setForm({...form, addBackup: e.target.checked})} className="rounded" />
              <span className="ml-2 text-sm">Add backup</span>
            </label>
          </div>
          <div className="flex justify-end space-x-3 pt-4">
            <button type="button" onClick={onClose} className="px-4 py-2 text-gray-700">Cancel</button>
            <button type="submit" className="px-4 py-2 bg-indigo-600 text-white rounded-md">Create</button>
          </div>
        </form>
      </div>
    </div>
  );
}

function ImportQuizModal({ onClose, onImported }: { onClose: () => void; onImported: () => void }) {
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  const handleImport = async () => {
    if (!file) return;
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const semesters = await api.get('/semesters');
      const active = semesters.data.find((s: any) => s.isActive);
      if (active) formData.append('semesterId', active.id);
      await api.post('/quizzes/import', formData);
      toast.success('Quizzes imported');
      onImported();
    } catch (err) {
      toast.error('Import failed');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-md">
        <h2 className="text-xl font-bold mb-4">Import Quizzes</h2>
        <p className="text-sm text-gray-600 mb-4">Upload an Excel file with columns: Course, Date, StartTime, EndTime, Room, ProctorsNeeded</p>
        <input type="file" accept=".xlsx,.xls" onChange={e => setFile(e.target.files?.[0] || null)} className="w-full" />
        <div className="flex justify-end space-x-3 pt-4">
          <button onClick={onClose} className="px-4 py-2 text-gray-700">Cancel</button>
          <button onClick={handleImport} disabled={!file || uploading}
            className="px-4 py-2 bg-indigo-600 text-white rounded-md disabled:opacity-50">
            {uploading ? 'Importing...' : 'Import'}
          </button>
        </div>
      </div>
    </div>
  );
}
