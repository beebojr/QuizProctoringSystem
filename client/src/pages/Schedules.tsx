import { useState, useEffect } from 'react';
import api from '../api/axios';
import toast from 'react-hot-toast';

interface ScheduleSlot {
  id: string;
  userId: string;
  taName: string;
  dayOfWeek: string;
  slotNumber: number;
  courseName: string;
  slotType: string;
  room: string;
  startTime: string;
  endTime: string;
}

export default function Schedules() {
  const [slots, setSlots] = useState<ScheduleSlot[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [selectedDay, setSelectedDay] = useState<string>('');

  const days = ['Friday', 'Saturday', 'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday'];

  useEffect(() => { loadSlots(); }, [selectedDay]);

  const loadSlots = async () => {
    try {
      const res = await api.get('/schedules');
      setSlots(res.data);
    } catch (err) {
      toast.error('Failed to load schedules');
    } finally {
      setLoading(false);
    }
  };

  const deleteSlot = async (id: string) => {
    if (!confirm('Delete this slot?')) return;
    try {
      await api.delete(`/schedules/${id}`);
      toast.success('Slot deleted');
      loadSlots();
    } catch (err) {
      toast.error('Failed to delete slot');
    }
  };

  const filteredSlots = selectedDay
    ? slots.filter(s => s.dayOfWeek === selectedDay)
    : slots;

  if (loading) return <div className="text-center py-8">Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900">TA Schedules</h1>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700">
          Add Slot
        </button>
      </div>

      <div className="flex space-x-2 overflow-x-auto pb-2">
        <button onClick={() => setSelectedDay('')}
          className={`px-3 py-1 rounded-full text-sm ${!selectedDay ? 'bg-indigo-600 text-white' : 'bg-gray-200'}`}>
          All
        </button>
        {days.map(day => (
          <button key={day} onClick={() => setSelectedDay(day)}
            className={`px-3 py-1 rounded-full text-sm ${selectedDay === day ? 'bg-indigo-600 text-white' : 'bg-gray-200'}`}>
            {day}
          </button>
        ))}
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">TA</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Day</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Slot</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Time</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Course</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Room</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filteredSlots.map(slot => (
              <tr key={slot.id}>
                <td className="px-4 py-3 whitespace-nowrap text-sm">{slot.taName}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">{slot.dayOfWeek}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">{slot.slotNumber}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">{slot.startTime} - {slot.endTime}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">{slot.courseName}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">{slot.room}</td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">
                  <span className={`px-2 py-1 text-xs rounded-full ${
                    slot.slotType === 'Teaching' ? 'bg-blue-100 text-blue-800' :
                    slot.slotType === 'Office' ? 'bg-green-100 text-green-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>{slot.slotType}</span>
                </td>
                <td className="px-4 py-3 whitespace-nowrap text-sm">
                  <button onClick={() => deleteSlot(slot.id)} className="text-red-600 hover:text-red-900">Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && <CreateSlotModal onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); loadSlots(); }} />}
    </div>
  );
}

function CreateSlotModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [users, setUsers] = useState<any[]>([]);
  const [form, setForm] = useState({ userId: '', semesterId: '', dayOfWeek: 'Friday', slotNumber: 1, courseName: '', slotType: 'Teaching', room: '' });

  useEffect(() => {
    api.get('/users?role=TA').then(res => setUsers(res.data));
    api.get('/semesters').then(res => { const a = res.data.find((s: any) => s.isActive); if (a) setForm(f => ({...f, semesterId: a.id})); });
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.post('/schedules', form);
      toast.success('Slot created');
      onCreated();
    } catch (err) { toast.error('Failed to create slot'); }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-md">
        <h2 className="text-xl font-bold mb-4">Add Schedule Slot</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">TA</label>
            <select value={form.userId} onChange={e => setForm({...form, userId: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border">
              <option value="">Select TA</option>
              {users.map((u: any) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Day</label>
            <select value={form.dayOfWeek} onChange={e => setForm({...form, dayOfWeek: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border">
              {['Friday','Saturday','Sunday','Monday','Tuesday','Wednesday','Thursday'].map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Slot</label>
            <select value={form.slotNumber} onChange={e => setForm({...form, slotNumber: parseInt(e.target.value)})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border">
              {[1,2,3,4,5].map(s => <option key={s} value={s}>Slot {s}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Course Name</label>
            <input value={form.courseName} onChange={e => setForm({...form, courseName: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" required />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Room</label>
            <input value={form.room} onChange={e => setForm({...form, room: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" required />
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
