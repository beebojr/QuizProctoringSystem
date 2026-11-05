import { Suspense, lazy } from 'react'
import type { ReactNode } from 'react'
import { Toaster } from 'react-hot-toast'
import { BrowserRouter, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ConfirmProvider, Loading } from './components/ui'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { TermProvider, useTerm } from './contexts/TermContext'
import Login from './pages/Login'

const Overview = lazy(() => import('./pages/admin/Overview'))
const Quizzes = lazy(() => import('./pages/admin/Quizzes'))
const QuizEditor = lazy(() => import('./pages/admin/QuizEditor'))
const ImportCalendar = lazy(() => import('./pages/admin/ImportCalendar'))
const AssignmentRun = lazy(() => import('./pages/admin/AssignmentRun'))
const Schedules = lazy(() => import('./pages/admin/Schedules'))
const Proctors = lazy(() => import('./pages/admin/Proctors'))
const Reviewing = lazy(() => import('./pages/admin/Reviewing'))
const SettingsPage = lazy(() => import('./pages/admin/Settings'))
const TaHome = lazy(() => import('./pages/ta/TaHome'))
const MyAssignments = lazy(() => import('./pages/ta/MyAssignments'))
const MySchedule = lazy(() => import('./pages/ta/MySchedule'))
const MyExcuses = lazy(() => import('./pages/ta/MyExcuses'))
const Account = lazy(() => import('./pages/Account'))

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, ready } = useAuth()
  const location = useLocation()
  if (!ready) return <Loading />
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  return <TermProvider>{children}</TermProvider>
}

function TermGate({ children }: { children: ReactNode }) {
  const { loaded } = useTerm()
  return loaded ? <>{children}</> : <Loading />
}

function AppRoutes() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        {isAdmin ? (
          <>
            <Route index element={<Page><Overview /></Page>} />
            <Route path="quizzes" element={<Page><Quizzes /></Page>} />
            <Route path="quizzes/new" element={<Page><QuizEditor key="new" /></Page>} />
            <Route path="quizzes/:id/edit" element={<Page><QuizEditor key="edit" /></Page>} />
            <Route path="import" element={<Page><ImportCalendar /></Page>} />
            <Route path="assign" element={<Page><AssignmentRun /></Page>} />
            <Route path="schedules" element={<Page><Schedules /></Page>} />
            <Route path="proctors" element={<Page><Proctors /></Page>} />
            <Route path="reviewing" element={<Page><Reviewing /></Page>} />
            <Route path="settings" element={<Page><SettingsPage /></Page>} />
          </>
        ) : (
          <>
            <Route index element={<Page><TaHome /></Page>} />
            <Route path="my-assignments" element={<Page><MyAssignments /></Page>} />
            <Route path="my-schedule" element={<Page><MySchedule /></Page>} />
            <Route path="my-excuses" element={<Page><MyExcuses /></Page>} />
          </>
        )}
        <Route path="account" element={<Page><Account /></Page>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

function Page({ children }: { children: ReactNode }) {
  return (
    <TermGate>
      <Suspense fallback={<Loading />}>{children}</Suspense>
    </TermGate>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ConfirmProvider>
          <AppRoutes />
          <Toaster position="bottom-right" toastOptions={{ className: 'toast', duration: 4000 }} />
        </ConfirmProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}
