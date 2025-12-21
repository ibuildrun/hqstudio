'use client'

import React, { createContext, useContext, useState, useEffect } from 'react'
import { SiteData, CallbackRequest, ServiceItem } from './types'
import { DEFAULT_SITE_DATA } from './constants'
import { api, setToken } from './api'

export type UserRole = 'ADMIN' | 'EDITOR' | 'MANAGER'

export interface AdminUser {
  id: number
  login: string
  role: UserRole
  name: string
}

export interface ActivityEntry {
  id: string
  user: string
  action: string
  timestamp: number
  source?: string
  entityType?: string
  entityId?: number
}

export interface Subscription {
  id: string
  email: string
  timestamp: number
}

export interface LocalUser {
  id: string
  login: string
  name: string
  role: UserRole
}

interface ExtendedSiteData extends SiteData {
  callbackRequests: CallbackRequest[]
  subscriptions: Subscription[]
  activityLog: ActivityEntry[]
  users: LocalUser[]
}

interface AdminContextType {
  data: ExtendedSiteData
  currentUser: AdminUser | null
  isAuthenticated: boolean
  isLoading: boolean
  mustChangePassword: boolean
  login: (login: string, pass: string) => Promise<boolean>
  logout: () => void
  changePassword: (currentPassword: string, newPassword: string) => Promise<{ success: boolean; error?: string }>
  updateData: (newData: Partial<ExtendedSiteData>) => void
  logActivity: (action: string, entityType?: string, entityId?: number) => void
  loadActivityLog: () => Promise<void>
  addRequest: (request: Omit<CallbackRequest, 'id' | 'timestamp' | 'status'>) => void
  deleteRequest: (id: string) => void
  addSubscription: (email: string) => void
  deleteSubscription: (id: string) => void
  toggleBlock: (blockId: string) => void
  moveBlockUp: (blockId: string) => void
  moveBlockDown: (blockId: string) => void
  reorderBlocks: (fromIndex: number, toIndex: number) => void
  updateService: (id: string, updatedService: ServiceItem) => void
  addService: (service: ServiceItem) => void
  deleteService: (id: string) => void
  loadUsers: () => Promise<void>
  deleteUser: (id: string) => Promise<void>
  addUser: (user: { login: string; password: string; name: string; role: UserRole }) => Promise<boolean>
}

const AdminContext = createContext<AdminContextType | undefined>(undefined)

const getDefaultData = (): ExtendedSiteData => ({
  ...DEFAULT_SITE_DATA,
  callbackRequests: [],
  subscriptions: [],
  activityLog: [],
  users: [],
})

// Маппинг ролей API -> фронтенд
const mapApiRole = (role: string | number): UserRole => {
  if (typeof role === 'number') {
    switch (role) {
      case 0: return 'ADMIN'
      case 1: return 'EDITOR'
      case 2: return 'MANAGER'
      default: return 'MANAGER'
    }
  }
  const roleUpper = role.toUpperCase()
  if (roleUpper === 'ADMIN') return 'ADMIN'
  if (roleUpper === 'EDITOR') return 'EDITOR'
  return 'MANAGER'
}

export const AdminProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [currentUser, setCurrentUser] = useState<AdminUser | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [mustChangePassword, setMustChangePassword] = useState(false)
  const [data, setData] = useState<ExtendedSiteData>(getDefaultData)
  const [isHydrated, setIsHydrated] = useState(false)
  const [isLoggingIn, setIsLoggingIn] = useState(false)

  // Загружаем данные из localStorage и проверяем токен
  useEffect(() => {
    const initAuth = async () => {
      // Если идёт процесс логина - не трогаем токен
      if (isLoggingIn) {
        setIsHydrated(true)
        return
      }

      try {
        const saved = localStorage.getItem('hq_studio_data_v10')
        if (saved) {
          const parsed = JSON.parse(saved)
          if (!parsed.siteBlocks || parsed.siteBlocks.length === 0) {
            parsed.siteBlocks = DEFAULT_SITE_DATA.siteBlocks
          }
          setData(parsed)
        }

        // Проверяем сохранённый токен
        const token = localStorage.getItem('hq_token')
        if (token) {
          // Проверяем срок действия токена локально
          try {
            // JWT использует Base64Url, конвертируем в обычный Base64
            let payloadStr = token.split('.')[1]
            payloadStr = payloadStr.replace(/-/g, '+').replace(/_/g, '/')
            const pad = payloadStr.length % 4
            if (pad) {
              payloadStr += '='.repeat(4 - pad)
            }
            const payload = JSON.parse(atob(payloadStr))
            if (payload.exp * 1000 < Date.now()) {
              // Токен истёк
              console.log('[initAuth] Token expired, removing')
              localStorage.removeItem('hq_token')
              setIsHydrated(true)
              return
            }
          } catch (e) {
            // Невалидный токен
            console.log('[initAuth] Invalid token, removing:', e)
            localStorage.removeItem('hq_token')
            setIsHydrated(true)
            return
          }

          const result = await api.auth.me()
          if (result.data) {
            setCurrentUser({
              id: result.data.id,
              login: result.data.login,
              name: result.data.name,
              role: mapApiRole(result.data.role),
            })
          }
          // НЕ удаляем токен если /auth/me вернул ошибку
        }
      } catch (e) {
        console.error('Failed to initialize auth:', e)
      }
      setIsHydrated(true)
    }

    initAuth()
  }, [isLoggingIn])

  // Сохраняем в localStorage только после гидратации
  useEffect(() => {
    if (isHydrated) {
      try {
        localStorage.setItem('hq_studio_data_v10', JSON.stringify(data))
      } catch (e) {
        console.error('Failed to save data to localStorage:', e)
      }
    }
  }, [data, isHydrated])

  const logActivity = async (action: string, entityType?: string, entityId?: number) => {
    const newEntry: ActivityEntry = {
      id: Date.now().toString(),
      user: currentUser?.name || 'Система',
      action,
      timestamp: Date.now(),
      source: 'Web',
      entityType,
      entityId
    }
    setData(prev => ({
      ...prev,
      activityLog: [newEntry, ...prev.activityLog].slice(0, 100)
    }))
    
    // Отправляем на сервер в фоне (не блокируем и не обрабатываем ошибки)
    api.activityLog.create({ action, entityType, entityId, source: 'Web' }).catch(() => {
      // Игнорируем ошибки логирования
    })
  }

  const loadActivityLog = async () => {
    try {
      // На сайте показываем только записи с источником Web
      const result = await api.activityLog.getAll({ page: 1, pageSize: 100, source: 'Web' })
      if (result.data) {
        setData(prev => ({
          ...prev,
          activityLog: result.data!.items.map(item => ({
            id: item.id.toString(),
            user: item.userName,
            action: item.action,
            timestamp: new Date(item.createdAt).getTime(),
            source: item.source,
            entityType: item.entityType,
            entityId: item.entityId
          }))
        }))
      }
    } catch (e) {
      console.error('Failed to load activity log:', e)
    }
  }

  const login = async (loginStr: string, pass: string): Promise<boolean> => {
    setIsLoading(true)
    setIsLoggingIn(true)
    try {
      const result = await api.auth.login(loginStr, pass)

      if (result.error) {
        return false
      }

      if (result.data && result.data.token) {
        // Сохраняем токен СИНХРОННО
        setToken(result.data.token)

        if (result.data.mustChangePassword) {
          setMustChangePassword(true)
        }

        // Уведомляем компоненты об авторизации ПЕРЕД установкой пользователя
        if (typeof window !== 'undefined') {
          window.dispatchEvent(new Event('auth-changed'))
        }

        // Устанавливаем пользователя ПОСЛЕ сохранения токена
        setCurrentUser({
          id: result.data.user.id,
          login: result.data.user.login,
          name: result.data.user.name,
          role: mapApiRole(result.data.user.role),
        })

        return true
      }
      return false
    } catch {
      return false
    } finally {
      setIsLoading(false)
      setIsLoggingIn(false)
    }
  }

  const changePassword = async (currentPassword: string, newPassword: string): Promise<{ success: boolean; error?: string }> => {
    try {
      const result = await api.auth.changePassword(currentPassword, newPassword)
      if (result.data) {
        setMustChangePassword(false)
        logActivity('Сменил пароль')
        return { success: true }
      }
      return { success: false, error: result.error || 'Ошибка смены пароля' }
    } catch (e) {
      return { success: false, error: 'Ошибка соединения' }
    }
  }

  const logout = () => {
    setToken(null)
    setCurrentUser(null)
    setMustChangePassword(false)
  }

  const updateData = (newData: Partial<ExtendedSiteData>) => {
    setData(prev => ({ ...prev, ...newData }))
  }

  const addSubscription = (email: string) => {
    if (data.subscriptions.some(s => s.email === email)) return
    const newSub: Subscription = { id: Date.now().toString(), email, timestamp: Date.now() }
    setData(prev => ({ ...prev, subscriptions: [newSub, ...prev.subscriptions] }))
  }

  const deleteSubscription = (id: string) => {
    setData(prev => ({ ...prev, subscriptions: prev.subscriptions.filter(s => s.id !== id) }))
    logActivity(`Удален подписчик: ${id}`)
  }

  const addRequest = (req: Omit<CallbackRequest, 'id' | 'timestamp' | 'status'>) => {
    const newReq: CallbackRequest = { ...req, id: Date.now().toString(), timestamp: Date.now(), status: 'new' }
    setData(prev => ({ ...prev, callbackRequests: [newReq, ...prev.callbackRequests] }))
  }

  const deleteRequest = (id: string) => {
    setData(prev => ({ ...prev, callbackRequests: prev.callbackRequests.filter(r => r.id !== id) }))
    logActivity(`Удалена заявка: ${id}`)
  }

  const toggleBlock = (blockId: string) => {
    setData(prev => ({
      ...prev,
      siteBlocks: prev.siteBlocks?.map(b => b.id === blockId ? { ...b, enabled: !b.enabled } : b) || []
    }))
    logActivity(`Переключен блок: ${blockId}`)
  }

  const moveBlockUp = (blockId: string) => {
    setData(prev => {
      const blocks = [...(prev.siteBlocks || [])]
      const idx = blocks.findIndex(b => b.id === blockId)
      if (idx > 0) {
        [blocks[idx - 1], blocks[idx]] = [blocks[idx], blocks[idx - 1]]
      }
      return { ...prev, siteBlocks: blocks }
    })
    logActivity(`Блок перемещен вверх: ${blockId}`)
  }

  const moveBlockDown = (blockId: string) => {
    setData(prev => {
      const blocks = [...(prev.siteBlocks || [])]
      const idx = blocks.findIndex(b => b.id === blockId)
      if (idx < blocks.length - 1) {
        [blocks[idx], blocks[idx + 1]] = [blocks[idx + 1], blocks[idx]]
      }
      return { ...prev, siteBlocks: blocks }
    })
    logActivity(`Блок перемещен вниз: ${blockId}`)
  }

  const reorderBlocks = (fromIndex: number, toIndex: number) => {
    setData(prev => {
      const blocks = [...(prev.siteBlocks || [])]
      const [removed] = blocks.splice(fromIndex, 1)
      blocks.splice(toIndex, 0, removed)
      return { ...prev, siteBlocks: blocks }
    })
    logActivity(`Блоки переупорядочены`)
  }

  const updateService = (id: string, updated: ServiceItem) => {
    setData(prev => ({ ...prev, services: prev.services.map(s => s.id === id ? updated : s) }))
    logActivity(`Обновлена услуга: ${updated.title}`)
  }

  const addService = (service: ServiceItem) => {
    setData(prev => ({ ...prev, services: [...prev.services, service] }))
    logActivity(`Добавлена услуга: ${service.title}`)
  }

  const deleteService = (id: string) => {
    setData(prev => ({ ...prev, services: prev.services.filter(s => s.id !== id) }))
    logActivity(`Удалена услуга: ${id}`)
  }

  const loadUsers = async () => {
    try {
      const result = await api.users.getAll()
      if (result.data) {
        setData(prev => ({
          ...prev,
          users: result.data!.map(u => ({
            id: u.id.toString(),
            login: u.login,
            name: u.name,
            role: mapApiRole(u.role)
          }))
        }))
      }
    } catch (e) {
      console.error('Failed to load users:', e)
    }
  }

  const deleteUser = async (id: string) => {
    try {
      await api.users.delete(parseInt(id))
      setData(prev => ({ ...prev, users: prev.users.filter(u => u.id !== id) }))
      logActivity(`Удалён пользователь: ${id}`)
    } catch (e) {
      console.error('Failed to delete user:', e)
    }
  }

  const addUser = async (user: { login: string; password: string; name: string; role: UserRole }): Promise<boolean> => {
    try {
      const result = await api.auth.register(user.login, user.password, user.name, user.role)
      if (result.data) {
        await loadUsers()
        logActivity(`Добавлен пользователь: ${user.name}`)
        return true
      }
      return false
    } catch (e) {
      console.error('Failed to add user:', e)
      return false
    }
  }

  return (
    <AdminContext.Provider value={{
      data, currentUser, isAuthenticated: !!currentUser, isLoading, mustChangePassword, login, logout, changePassword,
      updateData, logActivity, loadActivityLog, addRequest, deleteRequest, toggleBlock, moveBlockUp, moveBlockDown, reorderBlocks,
      updateService, addService, deleteService, addSubscription, deleteSubscription, loadUsers, deleteUser, addUser
    }}>
      {children}
    </AdminContext.Provider>
  )
}

export const useAdmin = () => {
  const context = useContext(AdminContext)
  if (!context) throw new Error('useAdmin must be used within AdminProvider')
  return context
}
