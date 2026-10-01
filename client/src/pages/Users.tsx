import { useState, useEffect } from 'react';
import api from '../api/axios';
import toast from 'react-hot-toast';

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  dayOff: string | null;
  targetWorkload: number;
  maxProctoringSessionsPerSemester: number;
  isActive: boolean;
  assignmentCount: number;
}

export default function Users() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [roleFilter, setRoleFilter] = useState('');
  const [search, setSearch] = useState('');

  useEffect(() => { loadUsers(); }, [roleFilter, search]);

  const loadUsers = async () => {
    try {
      const params = new URLSearchParams();
      if (roleFilter) params.append('role', roleFilter);
      if (search) params.append('search', search);
      const res = await api.get(`/users?${params}`);
      setUsers(res.data);
    } catch (err) {
      toast.error('Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="text-center py-8">Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700">
          Add User
        </button>
      </div>

      <div className="flex gap-4">
        <select value={roleFilter} onChange={e => setRoleFilter(e.target.value)}
          className="border-gray-300 rounded-md shadow-sm px-3 py-2 border">
          <option value="">All Roles</option>
          <option value="Admin">Admin</option>
          <option value="TA">TA</option>
        </select>
        <input type="text" placeholder="Search..." value={search} onChange={e => setSearch(e.target.value)}
          className="flex-1 border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Role</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Day Off</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Target</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Max/Sem</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Assignments</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {users.map(u => (
              <tr key={u.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">{u.fullName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{u.email}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <span className={`px-2 py-1 text-xs rounded-full ${u.role === 'Admin' ? 'bg-purple-100 text-purple-800' : 'bg-blue-100 text-blue-800'}`}>
                    {u.role}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{u.dayOff || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{u.targetWorkload}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{u.maxProctoringSessionsPerSemester}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{u.assignmentCount}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <span className={`px-2 py-1 text-xs rounded-full ${u.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showCreate && <CreateUserModal onClose={() => setShowCreate(false)} onCreated={() => { setShowCreate(false); loadUsers(); }} />}
    </div>
  );
}

function CreateUserModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [form, setForm] = useState({
    email: '', password: '', firstName: '', lastName: '', role: 'TA',
    dayOff: '', targetWorkload: 14, maxProctoringSessionsPerSemester: 10
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const payload = { ...form, dayOff: form.dayOff || undefined };
      await api.post('/users', payload);
      toast.success('User created');
      onCreated();
    } catch (err: any) {
      toast.error(err.response?.data?.errors?.[0] || 'Failed to create user');
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
        <h2 className="text-xl font-bold mb-4">Create User</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">First Name</label>
              <input value={form.firstName} onChange={e => setForm({...form, firstName: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" required />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Last Name</label>
              <input value={form.lastName} onChange={e => setForm({...form, lastName: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" required />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Email</label>
            <input type="email" value={form.email} onChange={e => setForm({...form, email: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" required />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Password</label>
            <input type="password" value={form.password} onChange={e => setForm({...form, password: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" required minLength={6} />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Role</label>
            <select value={form.role} onChange={e => setForm({...form, role: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border">
              <option value="Admin">Admin</option>
              <option value="TA">TA</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Day Off</label>
            <select value={form.dayOff} onChange={e => setForm({...form, dayOff: e.target.value})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border">
              <option value="">None</option>
              {['Friday','Saturday','Sunday','Monday','Tuesday','Wednesday','Thursday'].map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">Target Workload</label>
              <input type="number" value={form.targetWorkload} onChange={e => setForm({...form, targetWorkload: parseInt(e.target.value) || 14})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Max Sessions/Semester</label>
              <input type="number" value={form.maxProctoringSessionsPerSemester} onChange={e => setForm({...form, maxProctoringSessionsPerSemester: parseInt(e.target.value) || 10})} className="mt-1 block w-full border-gray-300 rounded-md shadow-sm px-3 py-2 border" />
            </div>
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
