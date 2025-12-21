'use client'

import { useState, useEffect, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { 
  Phone, Globe, User, Mail, MessageCircle, Users, MoreHorizontal,
  Clock, CheckCircle, XCircle, AlertCircle, Plus, Search,
  Car, Calendar, Trash2, Eye, RefreshCw, LogIn
} from 'lucide-react'
import { api, getToken, CallbackRequest, RequestSource, RequestStatus, CallbackStats } from '@/lib/api'

const SOURCE_LABELS: Record<RequestSource, { label: string; icon: React.ReactNode; color: string }> = {
  Website: { label: 'Сайт', icon: <Globe size={14} />, color: 'bg-blue-500/20 text-blue-400' },
  Phone: { label: 'Звонок', icon: <Phone size={14} />, color: 'bg-green-500/20 text-green-400' },
  WalkIn: { label: 'Живой приход', icon: <User size={14} />, color: 'bg-purple-500/20 text-purple-400' },
  Email: { label: 'Почта', icon: <Mail size={14} />, color: 'bg-yellow-500/20 text-yellow-400' },
  Messenger: { label: 'Мессенджер', icon: <MessageCircle size={14} />, color: 'bg-cyan-500/20 text-cyan-400' },
  Referral: { label: 'Рекомендация', icon: <Users size={14} />, color: 'bg-pink-500/20 text-pink-400' },
  Other: { label: 'Другое', icon: <MoreHorizontal size={14} />, color: 'bg-gray-500/20 text-gray-400' },
}

const STATUS_LABELS: Record<RequestStatus, { label: string; icon: React.ReactNode; color: string }> = {
  New: { label: 'Новая', icon: <AlertCircle size={14} />, color: 'bg-yellow-500/20 text-yellow-400' },
  Processing: { label: 'В работе', icon: <Clock size={14} />, color: 'bg-blue-500/20 text-blue-400' },
  Completed: { label: 'Завершена', icon: <CheckCircle size={14} />, color: 'bg-green-500/20 text-green-400' },
  Cancelled: { label: 'Отменена', icon: <XCircle size={14} />, color: 'bg-red-500/20 text-red-400' },
}

interface CallbacksPanelProps {
  onClose?: () => void
}

export default function CallbacksPanel({ onClose }: CallbacksPanelProps) {
  const [callbacks, setCallbacks] = useState<CallbackRequest[]>([])
  const [stats, setStats] = useState<CallbackStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [filter, setFilter] = useState<{ status?: string; source?: string }>({})
  const [search, setSearch] = useState('')
  const [showAddModal, setShowAddModal] = useState(false)
  const [selectedCallback, setSelectedCallback] = useState<CallbackRequest | null>(null)
  const [authError, setAuthError] = useState(false)

  const loadData = useCallback(async (showRefresh = false) => {
    // Проверяем наличие токена перед запросом (из localStorage)
    const token = getToken()
    if (!token) {
      console.log('CallbacksPanel: No token, skipping load')
      setAuthError(true)
      setLoading(false)
      setRefreshing(false)
      return
    }
    
    if (showRefresh) setRefreshing(true)
    else setLoading(true)
    
    setAuthError(false)
    
    try {
      const [callbacksRes, statsRes] = await Promise.all([
        api.callbacks.getAll(filter),
        api.callbacks.getStats()
      ])
      
      if (callbacksRes.unauthorized || statsRes.unauthorized) {
        console.log('CallbacksPanel: Unauthorized response')
        setAuthError(true)
      } else {
        if (callbacksRes.data) {
          setCallbacks(callbacksRes.data)
        }
        if (statsRes.data) setStats(statsRes.data)
      }
    } catch (e) {
      console.error('CallbacksPanel: Error loading data', e)
    }
    
    setLoading(false)
    setRefreshing(false)
  }, [filter])

  // Загружаем данные только если токен уже есть
  useEffect(() => {
    const token = getToken()
    if (token) {
      loadData()
    }
  }, [loadData])

  // Слушаем событие авторизации
  useEffect(() => {
    const handleAuthChange = () => {
      // Даём время на сохранение токена в localStorage
      setTimeout(() => {
        const token = getToken()
        if (token) {
          setAuthError(false)
          loadData()
        }
      }, 50)
    }
    window.addEventListener('auth-changed', handleAuthChange)
    
    const handleStorage = (e: StorageEvent) => {
      if (e.key === 'hq_token' && e.newValue) {
        loadData()
      }
    }
    window.addEventListener('storage', handleStorage)
    
    return () => {
      window.removeEventListener('auth-changed', handleAuthChange)
      window.removeEventListener('storage', handleStorage)
    }
  }, [loadData])

  // Автообновление каждые 30 секунд
  useEffect(() => {
    const token = localStorage.getItem('hq_token')
    if (!token) return
    const interval = setInterval(() => {
      loadData(true)
    }, 30000)
    return () => clearInterval(interval)
  }, [loadData])

  // Слушаем событие добавления новой заявки
  useEffect(() => {
    const handleCallbacksUpdated = () => {
      loadData(true)
    }
    window.addEventListener('callbacks-updated', handleCallbacksUpdated)
    return () => window.removeEventListener('callbacks-updated', handleCallbacksUpdated)
  }, [loadData])

  const handleRefresh = () => {
    loadData(true)
  }

  const handleStatusChange = async (id: number, status: RequestStatus) => {
    await api.callbacks.updateStatus(id, status)
    loadData(true)
  }

  const handleDelete = async (id: number) => {
    if (confirm('Удалить заявку?')) {
      await api.callbacks.delete(id)
      loadData(true)
    }
  }

  const filteredCallbacks = callbacks.filter(c => 
    search === '' || 
    c.name.toLowerCase().includes(search.toLowerCase()) ||
    c.phone.includes(search) ||
    c.carModel?.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="bg-neutral-900 rounded-2xl p-6 max-h-[80vh] overflow-hidden flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <h2 className="text-2xl font-bold">Заявки</h2>
          <button
            onClick={handleRefresh}
            disabled={refreshing}
            className="p-2 hover:bg-neutral-800 rounded-lg transition disabled:opacity-50"
            title="Обновить"
          >
            <RefreshCw size={18} className={refreshing ? 'animate-spin' : ''} />
          </button>
        </div>
        <button
          onClick={() => setShowAddModal(true)}
          className="flex items-center gap-2 bg-white text-black px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-200 transition"
        >
          <Plus size={16} /> Добавить
        </button>
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
          <StatCard label="Новые" value={stats.totalNew} color="yellow" />
          <StatCard label="В работе" value={stats.totalProcessing} color="blue" />
          <StatCard label="Сегодня" value={stats.todayCount} color="green" />
          <StatCard label="За месяц" value={stats.monthCount} color="purple" />
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-4">
        <div className="relative flex-1 min-w-[200px]">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-500" />
          <input
            type="text"
            placeholder="Поиск по имени, телефону, авто..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full bg-neutral-800 border border-neutral-700 rounded-lg pl-10 pr-4 py-2 text-sm outline-none focus:border-white/30"
          />
        </div>
        
        <select
          value={filter.status || ''}
          onChange={(e) => setFilter({ ...filter, status: e.target.value || undefined })}
          className="bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none"
        >
          <option value="">Все статусы</option>
          <option value="New">Новые</option>
          <option value="Processing">В работе</option>
          <option value="Completed">Завершенные</option>
          <option value="Cancelled">Отмененные</option>
        </select>

        <select
          value={filter.source || ''}
          onChange={(e) => setFilter({ ...filter, source: e.target.value || undefined })}
          className="bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none"
        >
          <option value="">Все источники</option>
          <option value="Website">Сайт</option>
          <option value="Phone">Звонок</option>
          <option value="WalkIn">Живой приход</option>
          <option value="Email">Почта</option>
          <option value="Messenger">Мессенджер</option>
          <option value="Referral">Рекомендация</option>
          <option value="Other">Другое</option>
        </select>
      </div>

      {/* List */}
      <div className="flex-1 overflow-y-auto space-y-2">
        {authError ? (
          <div className="text-center py-8">
            <LogIn size={48} className="mx-auto text-neutral-600 mb-4" />
            <p className="text-neutral-500 mb-4">Требуется авторизация для просмотра заявок</p>
            <button
              onClick={() => loadData()}
              className="px-4 py-2 bg-white text-black rounded-lg text-sm font-medium hover:bg-gray-200 transition"
            >
              Повторить загрузку
            </button>
          </div>
        ) : loading ? (
          <div className="text-center py-8 text-neutral-500">Загрузка...</div>
        ) : filteredCallbacks.length === 0 ? (
          <div className="text-center py-8 text-neutral-500">Заявок не найдено</div>
        ) : (
          filteredCallbacks.map((callback) => (
            <CallbackCard
              key={callback.id}
              callback={callback}
              onStatusChange={handleStatusChange}
              onDelete={handleDelete}
              onView={() => setSelectedCallback(callback)}
            />
          ))
        )}
      </div>

      {/* Add Modal */}
      <AnimatePresence>
        {showAddModal && (
          <AddCallbackModal
            onClose={() => setShowAddModal(false)}
            onSave={() => { setShowAddModal(false); loadData(true) }}
          />
        )}
        {selectedCallback && (
          <CallbackDetailModal
            callback={selectedCallback}
            onClose={() => setSelectedCallback(null)}
            onUpdate={() => { setSelectedCallback(null); loadData(true) }}
          />
        )}
      </AnimatePresence>
    </div>
  )
}

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
  const colors: Record<string, string> = {
    yellow: 'bg-yellow-500/10 border-yellow-500/20',
    blue: 'bg-blue-500/10 border-blue-500/20',
    green: 'bg-green-500/10 border-green-500/20',
    purple: 'bg-purple-500/10 border-purple-500/20',
  }
  return (
    <div className={`${colors[color]} border rounded-xl p-3`}>
      <div className="text-2xl font-bold">{value}</div>
      <div className="text-xs text-neutral-400">{label}</div>
    </div>
  )
}

function CallbackCard({ 
  callback, 
  onStatusChange, 
  onDelete,
  onView 
}: { 
  callback: CallbackRequest
  onStatusChange: (id: number, status: RequestStatus) => void
  onDelete: (id: number) => void
  onView: () => void
}) {
  const source = SOURCE_LABELS[callback.source]
  const status = STATUS_LABELS[callback.status]

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="bg-neutral-800 rounded-xl p-4 hover:bg-neutral-750 transition"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className="font-medium truncate">{callback.name}</span>
            <span className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] ${source.color}`}>
              {source.icon} {source.label}
            </span>
          </div>
          <div className="flex items-center gap-3 text-sm text-neutral-400">
            <span className="flex items-center gap-1">
              <Phone size={12} /> {callback.phone}
            </span>
            {callback.carModel && (
              <span className="flex items-center gap-1">
                <Car size={12} /> {callback.carModel}
              </span>
            )}
          </div>
          {callback.message && (
            <p className="text-xs text-neutral-500 mt-2 line-clamp-1">{callback.message}</p>
          )}
          <div className="flex items-center gap-2 mt-2 text-[10px] text-neutral-500">
            <Calendar size={10} />
            {new Date(callback.createdAt).toLocaleString('ru-RU')}
          </div>
        </div>

        <div className="flex flex-col items-end gap-2">
          <span className={`flex items-center gap-1 px-2 py-1 rounded-lg text-xs ${status.color}`}>
            {status.icon} {status.label}
          </span>
          
          <div className="flex items-center gap-1">
            <button
              onClick={onView}
              className="p-1.5 hover:bg-neutral-700 rounded-lg transition"
              title="Подробнее"
            >
              <Eye size={14} />
            </button>
            {callback.status === 'New' && (
              <button
                onClick={() => onStatusChange(callback.id, 'Processing')}
                className="p-1.5 hover:bg-blue-500/20 text-blue-400 rounded-lg transition"
                title="Взять в работу"
              >
                <Clock size={14} />
              </button>
            )}
            {callback.status === 'Processing' && (
              <button
                onClick={() => onStatusChange(callback.id, 'Completed')}
                className="p-1.5 hover:bg-green-500/20 text-green-400 rounded-lg transition"
                title="Завершить"
              >
                <CheckCircle size={14} />
              </button>
            )}
            <button
              onClick={() => onDelete(callback.id)}
              className="p-1.5 hover:bg-red-500/20 text-red-400 rounded-lg transition"
              title="Удалить"
            >
              <Trash2 size={14} />
            </button>
          </div>
        </div>
      </div>
    </motion.div>
  )
}

function AddCallbackModal({ onClose, onSave }: { onClose: () => void; onSave: () => void }) {
  const [form, setForm] = useState({
    name: '',
    phone: '',
    carModel: '',
    licensePlate: '',
    message: '',
    source: 'WalkIn' as RequestSource,
    sourceDetails: ''
  })
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.name || !form.phone) return
    
    setLoading(true)
    const result = await api.callbacks.createManual(form)
    setLoading(false)
    
    if (result.data) {
      onSave()
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.95 }}
        animate={{ scale: 1 }}
        exit={{ scale: 0.95 }}
        className="bg-neutral-900 rounded-2xl p-6 w-full max-w-md"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="text-xl font-bold mb-4">Новая заявка</h3>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="text-xs text-neutral-400 mb-1 block">Источник *</label>
            <select
              value={form.source}
              onChange={(e) => setForm({ ...form, source: e.target.value as RequestSource })}
              className="w-full bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none"
            >
              <option value="WalkIn">Живой приход</option>
              <option value="Phone">Звонок</option>
              <option value="Email">Почта</option>
              <option value="Messenger">Мессенджер</option>
              <option value="Referral">Рекомендация</option>
              <option value="Other">Другое</option>
            </select>
          </div>

          <div>
            <label className="text-xs text-neutral-400 mb-1 block">Имя *</label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="w-full bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none focus:border-white/30"
              required
            />
          </div>

          <div>
            <label className="text-xs text-neutral-400 mb-1 block">Телефон *</label>
            <input
              type="tel"
              value={form.phone}
              onChange={(e) => setForm({ ...form, phone: e.target.value })}
              className="w-full bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none focus:border-white/30"
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs text-neutral-400 mb-1 block">Марка авто</label>
              <input
                type="text"
                value={form.carModel}
                onChange={(e) => setForm({ ...form, carModel: e.target.value })}
                className="w-full bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none focus:border-white/30"
              />
            </div>
            <div>
              <label className="text-xs text-neutral-400 mb-1 block">Госномер</label>
              <input
                type="text"
                value={form.licensePlate}
                onChange={(e) => setForm({ ...form, licensePlate: e.target.value.toUpperCase() })}
                className="w-full bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none focus:border-white/30"
              />
            </div>
          </div>

          <div>
            <label className="text-xs text-neutral-400 mb-1 block">Комментарий</label>
            <textarea
              value={form.message}
              onChange={(e) => setForm({ ...form, message: e.target.value })}
              rows={3}
              className="w-full bg-neutral-800 border border-neutral-700 rounded-lg px-3 py-2 text-sm outline-none focus:border-white/30 resize-none"
            />
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 bg-neutral-800 py-2 rounded-lg text-sm font-medium hover:bg-neutral-700 transition"
            >
              Отмена
            </button>
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-white text-black py-2 rounded-lg text-sm font-medium hover:bg-gray-200 transition disabled:opacity-50"
            >
              {loading ? 'Сохранение...' : 'Сохранить'}
            </button>
          </div>
        </form>
      </motion.div>
    </motion.div>
  )
}

function CallbackDetailModal({ 
  callback, 
  onClose, 
  onUpdate 
}: { 
  callback: CallbackRequest
  onClose: () => void
  onUpdate: () => void
}) {
  const source = SOURCE_LABELS[callback.source]
  const status = STATUS_LABELS[callback.status]

  const handleStatusChange = async (newStatus: RequestStatus) => {
    await api.callbacks.updateStatus(callback.id, newStatus)
    onUpdate()
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4"
      onClick={onClose}
    >
      <motion.div
        initial={{ scale: 0.95 }}
        animate={{ scale: 1 }}
        exit={{ scale: 0.95 }}
        className="bg-neutral-900 rounded-2xl p-6 w-full max-w-md"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-xl font-bold">Заявка #{callback.id}</h3>
          <span className={`flex items-center gap-1 px-3 py-1 rounded-lg text-sm ${status.color}`}>
            {status.icon} {status.label}
          </span>
        </div>

        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <span className={`flex items-center gap-1 px-2 py-1 rounded-lg text-xs ${source.color}`}>
              {source.icon} {source.label}
            </span>
            {callback.sourceDetails && (
              <span className="text-xs text-neutral-500">({callback.sourceDetails})</span>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <div className="text-xs text-neutral-500 mb-1">Имя</div>
              <div className="font-medium">{callback.name}</div>
            </div>
            <div>
              <div className="text-xs text-neutral-500 mb-1">Телефон</div>
              <a href={`tel:${callback.phone}`} className="font-medium text-blue-400 hover:underline">
                {callback.phone}
              </a>
            </div>
            {callback.carModel && (
              <div>
                <div className="text-xs text-neutral-500 mb-1">Автомобиль</div>
                <div>{callback.carModel}</div>
              </div>
            )}
            {callback.licensePlate && (
              <div>
                <div className="text-xs text-neutral-500 mb-1">Госномер</div>
                <div className="font-mono">{callback.licensePlate}</div>
              </div>
            )}
          </div>

          {callback.message && (
            <div>
              <div className="text-xs text-neutral-500 mb-1">Сообщение</div>
              <div className="text-sm bg-neutral-800 rounded-lg p-3">{callback.message}</div>
            </div>
          )}

          <div className="grid grid-cols-2 gap-4 text-xs text-neutral-500">
            <div>
              <div className="mb-1">Создана</div>
              <div className="text-white">{new Date(callback.createdAt).toLocaleString('ru-RU')}</div>
            </div>
            {callback.processedAt && (
              <div>
                <div className="mb-1">Взята в работу</div>
                <div className="text-white">{new Date(callback.processedAt).toLocaleString('ru-RU')}</div>
              </div>
            )}
            {callback.completedAt && (
              <div>
                <div className="mb-1">Завершена</div>
                <div className="text-white">{new Date(callback.completedAt).toLocaleString('ru-RU')}</div>
              </div>
            )}
          </div>

          <div className="flex gap-2 pt-4 border-t border-neutral-800">
            {callback.status === 'New' && (
              <button
                onClick={() => handleStatusChange('Processing')}
                className="flex-1 bg-blue-500/20 text-blue-400 py-2 rounded-lg text-sm font-medium hover:bg-blue-500/30 transition"
              >
                Взять в работу
              </button>
            )}
            {callback.status === 'Processing' && (
              <button
                onClick={() => handleStatusChange('Completed')}
                className="flex-1 bg-green-500/20 text-green-400 py-2 rounded-lg text-sm font-medium hover:bg-green-500/30 transition"
              >
                Завершить
              </button>
            )}
            {(callback.status === 'New' || callback.status === 'Processing') && (
              <button
                onClick={() => handleStatusChange('Cancelled')}
                className="bg-red-500/20 text-red-400 px-4 py-2 rounded-lg text-sm font-medium hover:bg-red-500/30 transition"
              >
                Отменить
              </button>
            )}
            <button
              onClick={onClose}
              className="bg-neutral-800 px-4 py-2 rounded-lg text-sm font-medium hover:bg-neutral-700 transition"
            >
              Закрыть
            </button>
          </div>
        </div>
      </motion.div>
    </motion.div>
  )
}
