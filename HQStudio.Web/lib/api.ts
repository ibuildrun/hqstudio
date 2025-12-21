const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

// Типы для enum'ов (определяем в начале для использования в маппинге)
export type RequestSource = 'Website' | 'Phone' | 'WalkIn' | 'Email' | 'Messenger' | 'Referral' | 'Other'
export type RequestStatus = 'New' | 'Processing' | 'Completed' | 'Cancelled'

interface ApiResponse<T> {
  data?: T
  error?: string
  unauthorized?: boolean
}

// Маппинг enum'ов из API (числа) в строки
const SOURCE_MAP: Record<number, RequestSource> = {
  0: 'Website',
  1: 'Phone',
  2: 'WalkIn',
  3: 'Email',
  4: 'Messenger',
  5: 'Referral',
  6: 'Other'
}

const STATUS_MAP: Record<number, RequestStatus> = {
  0: 'New',
  1: 'Processing',
  2: 'Completed',
  3: 'Cancelled'
}

// Преобразование callback из API
function mapCallback(raw: RawCallbackRequest): CallbackRequest {
  return {
    ...raw,
    source: typeof raw.source === 'number' ? SOURCE_MAP[raw.source] || 'Other' : raw.source,
    status: typeof raw.status === 'number' ? STATUS_MAP[raw.status] || 'New' : raw.status
  }
}

// Raw типы из API (с числовыми enum'ами)
interface RawCallbackRequest {
  id: number
  name: string
  phone: string
  carModel?: string
  licensePlate?: string
  message?: string
  status: number | RequestStatus
  source: number | RequestSource
  sourceDetails?: string
  assignedUserId?: number
  createdAt: string
  processedAt?: string
  completedAt?: string
}

// Функция для проверки истечения токена
function isTokenExpired(token: string): boolean {
  try {
    // JWT использует Base64Url, нужно конвертировать в обычный Base64
    let payload = token.split('.')[1]
    payload = payload.replace(/-/g, '+').replace(/_/g, '/')
    // Добавляем padding если нужно
    const pad = payload.length % 4
    if (pad) {
      payload += '='.repeat(4 - pad)
    }
    
    const decoded = JSON.parse(atob(payload))
    return decoded.exp * 1000 < Date.now()
  } catch {
    return true
  }
}

// Функция для получения токена - ВСЕГДА читаем из localStorage
export function getToken(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem('hq_token')
}

// Функция для сохранения токена
export function setToken(token: string | null) {
  if (typeof window === 'undefined') return
  if (token) {
    localStorage.setItem('hq_token', token)
  } else {
    localStorage.removeItem('hq_token')
  }
}

// Функция для очистки сессии
function clearSession() {
  if (typeof window !== 'undefined') {
    try {
      localStorage.removeItem('hq_token')
    } catch {
      // ignore
    }
  }
}

async function request<T>(endpoint: string, options?: RequestInit): Promise<ApiResponse<T>> {
  try {
    const token = getToken()
    
    // Проверяем токен перед запросом
    if (token && isTokenExpired(token)) {
      clearSession()
      return { error: 'Сессия истекла. Войдите снова.', unauthorized: true }
    }
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...options?.headers as Record<string, string>,
    }
    
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
    
    const res = await fetch(`${API_URL}${endpoint}`, {
      ...options,
      headers,
    })

    // Обработка 401 - невалидный токен
    if (res.status === 401) {
      return { error: 'Требуется авторизация', unauthorized: true }
    }

    // Обработка 429 - слишком много запросов
    if (res.status === 429) {
      return { error: 'Слишком много запросов. Подождите минуту.' }
    }

    if (!res.ok) {
      const error = await res.json().catch(() => ({ message: 'Ошибка сервера' }))
      return { error: error.message || `Ошибка ${res.status}` }
    }

    const data = await res.json()
    return { data }
  } catch (e) {
    return { error: 'Нет соединения с сервером' }
  }
}

// Auth
export const api = {
  auth: {
    login: (login: string, password: string) =>
      request<{ token: string; user: User; mustChangePassword?: boolean }>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ login, password }),
      }),
    me: () => request<User>('/auth/me'),
    changePassword: (currentPassword: string, newPassword: string) =>
      request<{ message: string }>('/auth/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword, newPassword }),
      }),
    register: (login: string, password: string, name: string, role: string) =>
      request<User>('/auth/register', {
        method: 'POST',
        body: JSON.stringify({ login, password, name, role }),
      }),
  },

  // Public endpoints
  site: {
    getData: () => request<SiteData>('/site'),
  },

  services: {
    getAll: (activeOnly = true) => request<Service[]>(`/services?activeOnly=${activeOnly}`),
    get: (id: number) => request<Service>(`/services/${id}`),
    create: (service: Partial<Service>) =>
      request<Service>('/services', { method: 'POST', body: JSON.stringify(service) }),
    update: (id: number, service: Partial<Service>) =>
      request<Service>(`/services/${id}`, { method: 'PUT', body: JSON.stringify(service) }),
    delete: (id: number) => request(`/services/${id}`, { method: 'DELETE' }),
  },

  callbacks: {
    create: (data: CallbackRequestData) =>
      request<{ message: string; id: number }>('/callbacks', { method: 'POST', body: JSON.stringify(data) }),
    createManual: async (data: CallbackRequestData): Promise<ApiResponse<CallbackRequest>> => {
      const result = await request<RawCallbackRequest>('/callbacks/manual', { method: 'POST', body: JSON.stringify(data) })
      if (result.data) {
        return { data: mapCallback(result.data) }
      }
      return result as ApiResponse<CallbackRequest>
    },
    getAll: async (params?: CallbackFilters): Promise<ApiResponse<CallbackRequest[]>> => {
      const query = new URLSearchParams()
      if (params?.status) query.append('status', params.status)
      if (params?.source) query.append('source', params.source)
      if (params?.from) query.append('from', params.from)
      if (params?.to) query.append('to', params.to)
      const result = await request<RawCallbackRequest[]>(`/callbacks?${query.toString()}`)
      if (result.data) {
        return { data: result.data.map(mapCallback) }
      }
      return result as ApiResponse<CallbackRequest[]>
    },
    get: async (id: number): Promise<ApiResponse<CallbackRequest>> => {
      const result = await request<RawCallbackRequest>(`/callbacks/${id}`)
      if (result.data) {
        return { data: mapCallback(result.data) }
      }
      return result as ApiResponse<CallbackRequest>
    },
    update: async (id: number, data: Partial<CallbackRequest>): Promise<ApiResponse<CallbackRequest>> => {
      const result = await request<RawCallbackRequest>(`/callbacks/${id}`, { method: 'PUT', body: JSON.stringify(data) })
      if (result.data) {
        return { data: mapCallback(result.data) }
      }
      return result as ApiResponse<CallbackRequest>
    },
    updateStatus: (id: number, status: string) =>
      request(`/callbacks/${id}/status`, { method: 'PUT', body: JSON.stringify(status) }),
    getStats: () => request<CallbackStats>('/callbacks/stats'),
    delete: (id: number) => request(`/callbacks/${id}`, { method: 'DELETE' }),
  },

  subscriptions: {
    create: (email: string) =>
      request<{ message: string }>('/subscriptions', { method: 'POST', body: JSON.stringify({ email }) }),
    getAll: () => request<Subscription[]>('/subscriptions'),
    delete: (id: number) => request(`/subscriptions/${id}`, { method: 'DELETE' }),
  },

  clients: {
    getAll: () => request<Client[]>('/clients'),
    get: (id: number) => request<Client>(`/clients/${id}`),
    create: (client: Partial<Client>) =>
      request<Client>('/clients', { method: 'POST', body: JSON.stringify(client) }),
    update: (id: number, client: Partial<Client>) =>
      request<Client>(`/clients/${id}`, { method: 'PUT', body: JSON.stringify(client) }),
    delete: (id: number) => request(`/clients/${id}`, { method: 'DELETE' }),
  },

  orders: {
    getAll: (status?: string) => request<Order[]>(`/orders${status ? `?status=${status}` : ''}`),
    get: (id: number) => request<Order>(`/orders/${id}`),
    create: (order: CreateOrderData) =>
      request<Order>('/orders', { method: 'POST', body: JSON.stringify(order) }),
    updateStatus: (id: number, status: string) =>
      request(`/orders/${id}/status`, { method: 'PUT', body: JSON.stringify(status) }),
    delete: (id: number) => request(`/orders/${id}`, { method: 'DELETE' }),
  },

  dashboard: {
    getStats: () => request<DashboardStats>('/dashboard'),
  },

  content: {
    getBlocks: () => request<SiteBlock[]>('/site/blocks'),
    updateBlock: (id: number, block: SiteBlock) =>
      request(`/site/blocks/${id}`, { method: 'PUT', body: JSON.stringify(block) }),
    reorderBlocks: (blockIds: number[]) =>
      request('/site/blocks/reorder', { method: 'POST', body: JSON.stringify(blockIds) }),
    getTestimonials: () => request<Testimonial[]>('/site/testimonials'),
    createTestimonial: (t: Partial<Testimonial>) =>
      request<Testimonial>('/site/testimonials', { method: 'POST', body: JSON.stringify(t) }),
    updateTestimonial: (id: number, t: Partial<Testimonial>) =>
      request(`/site/testimonials/${id}`, { method: 'PUT', body: JSON.stringify(t) }),
    deleteTestimonial: (id: number) => request(`/site/testimonials/${id}`, { method: 'DELETE' }),
    getFaq: () => request<FaqItem[]>('/site/faq'),
    createFaq: (f: Partial<FaqItem>) =>
      request<FaqItem>('/site/faq', { method: 'POST', body: JSON.stringify(f) }),
    updateFaq: (id: number, f: Partial<FaqItem>) =>
      request(`/site/faq/${id}`, { method: 'PUT', body: JSON.stringify(f) }),
    deleteFaq: (id: number) => request(`/site/faq/${id}`, { method: 'DELETE' }),
    updateContent: (content: Record<string, string>) =>
      request('/site/content', { method: 'PUT', body: JSON.stringify(content) }),
  },

  users: {
    getAll: () => request<User[]>('/users'),
    get: (id: number) => request<User>(`/users/${id}`),
    update: (id: number, user: Partial<User>) =>
      request(`/users/${id}`, { method: 'PUT', body: JSON.stringify(user) }),
    delete: (id: number) => request(`/users/${id}`, { method: 'DELETE' }),
  },

  activityLog: {
    getAll: (params?: ActivityLogFilters) => {
      const query = new URLSearchParams()
      if (params?.page) query.append('page', params.page.toString())
      if (params?.pageSize) query.append('pageSize', params.pageSize.toString())
      if (params?.source) query.append('source', params.source)
      if (params?.userId) query.append('userId', params.userId.toString())
      return request<ActivityLogResponse>(`/activitylog?${query.toString()}`)
    },
    getStats: () => request<ActivityLogStats>('/activitylog/stats'),
    create: (data: CreateActivityLogData) =>
      request<{ id: number; createdAt: string }>('/activitylog', { method: 'POST', body: JSON.stringify(data) }),
  },
}

// Types
export interface User {
  id: number
  login: string
  name: string
  role: 'Admin' | 'Editor' | 'Manager'
}

export interface Service {
  id: number
  title: string
  category: string
  description: string
  price: string
  image?: string
  isActive: boolean
  sortOrder: number
}

export interface Client {
  id: number
  name: string
  phone: string
  email?: string
  carModel?: string
  licensePlate?: string
  notes?: string
  createdAt: string
}

export interface Order {
  id: number
  clientId: number
  client: Client
  status: 'New' | 'InProgress' | 'Completed' | 'Cancelled'
  totalPrice: number
  notes?: string
  createdAt: string
  completedAt?: string
}

export interface CreateOrderData {
  clientId: number
  serviceIds: number[]
  totalPrice: number
  notes?: string
}

export interface CallbackRequest {
  id: number
  name: string
  phone: string
  carModel?: string
  licensePlate?: string
  message?: string
  status: RequestStatus
  source: RequestSource
  sourceDetails?: string
  assignedUserId?: number
  createdAt: string
  processedAt?: string
  completedAt?: string
}

export interface CallbackRequestData {
  name: string
  phone: string
  carModel?: string
  licensePlate?: string
  message?: string
  source?: RequestSource
  sourceDetails?: string
}

export interface CallbackFilters {
  status?: string
  source?: string
  from?: string
  to?: string
}

export interface CallbackStats {
  totalNew: number
  totalProcessing: number
  totalCompleted: number
  todayCount: number
  weekCount: number
  monthCount: number
  bySource: { source: RequestSource; count: number }[]
}

export interface Subscription {
  id: number
  email: string
  createdAt: string
}

export interface SiteBlock {
  id: number
  blockId: string
  name: string
  enabled: boolean
  sortOrder: number
}

export interface Testimonial {
  id: number
  name: string
  car: string
  text: string
  isActive: boolean
  sortOrder: number
}

export interface FaqItem {
  id: number
  question: string
  answer: string
  isActive: boolean
  sortOrder: number
}

export interface SiteData {
  services: Service[]
  blocks: SiteBlock[]
  testimonials: Testimonial[]
  faq: FaqItem[]
  showcase: any[]
  content: Record<string, string>
}

export interface DashboardStats {
  totalClients: number
  totalOrders: number
  newCallbacks: number
  monthlyRevenue: number
  ordersInProgress: number
  completedThisMonth: number
  newSubscribers: number
  popularServices: { name: string; count: number }[]
  recentOrders: { id: number; clientName: string; status: string; totalPrice: number; createdAt: string }[]
}

// Activity Log types
export interface ActivityLogEntry {
  id: number
  userId: number
  userName: string
  action: string
  entityType?: string
  entityId?: number
  details?: string
  source: string
  ipAddress?: string
  createdAt: string
}

export interface ActivityLogResponse {
  items: ActivityLogEntry[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ActivityLogStats {
  totalToday: number
  totalWeek: number
  totalAll: number
  bySource: { source: string; count: number }[]
  byUser: { userId: number; userName: string; count: number }[]
}

export interface ActivityLogFilters {
  page?: number
  pageSize?: number
  source?: string
  userId?: number
}

export interface CreateActivityLogData {
  action: string
  entityType?: string
  entityId?: number
  details?: string
  source?: string
}
