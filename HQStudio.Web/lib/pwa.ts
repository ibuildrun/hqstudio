'use client'

// Типы уведомлений
export type NotificationType = 
  | 'callbacks'      // Новые заявки
  | 'orders'         // Изменения заказов
  | 'security'       // Безопасность (входы, смена пароля)
  | 'system'         // Системные уведомления

export interface NotificationSettings {
  enabled: boolean
  callbacks: boolean
  orders: boolean
  security: boolean
  system: boolean
}

const DEFAULT_SETTINGS: NotificationSettings = {
  enabled: false,
  callbacks: true,
  orders: true,
  security: true,
  system: false
}

// Получить настройки уведомлений
export function getNotificationSettings(): NotificationSettings {
  if (typeof window === 'undefined') return DEFAULT_SETTINGS
  
  const saved = localStorage.getItem('hq_notification_settings')
  if (saved) {
    try {
      return { ...DEFAULT_SETTINGS, ...JSON.parse(saved) }
    } catch {
      return DEFAULT_SETTINGS
    }
  }
  return DEFAULT_SETTINGS
}

// Сохранить настройки уведомлений
export function saveNotificationSettings(settings: NotificationSettings): void {
  if (typeof window === 'undefined') return
  localStorage.setItem('hq_notification_settings', JSON.stringify(settings))
}

// Проверить поддержку PWA
export function isPWASupported(): boolean {
  if (typeof window === 'undefined') return false
  return 'serviceWorker' in navigator && 'PushManager' in window
}

// Проверить, установлено ли приложение как PWA
export function isPWAInstalled(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia('(display-mode: standalone)').matches ||
         (window.navigator as any).standalone === true
}

// Проверить разрешение на уведомления
export function getNotificationPermission(): NotificationPermission | 'unsupported' {
  if (typeof window === 'undefined' || !('Notification' in window)) {
    return 'unsupported'
  }
  return Notification.permission
}

// Запросить разрешение на уведомления
export async function requestNotificationPermission(): Promise<boolean> {
  if (typeof window === 'undefined' || !('Notification' in window)) {
    return false
  }
  
  const permission = await Notification.requestPermission()
  return permission === 'granted'
}

// Зарегистрировать Service Worker
export async function registerServiceWorker(): Promise<ServiceWorkerRegistration | null> {
  if (typeof window === 'undefined' || !('serviceWorker' in navigator)) {
    return null
  }
  
  try {
    const registration = await navigator.serviceWorker.register('/sw.js')
    console.log('Service Worker registered:', registration.scope)
    return registration
  } catch (error) {
    console.error('Service Worker registration failed:', error)
    return null
  }
}

// Подписаться на push-уведомления
export async function subscribeToPush(vapidPublicKey: string): Promise<PushSubscription | null> {
  if (typeof window === 'undefined') return null
  
  try {
    const registration = await navigator.serviceWorker.ready
    
    const subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey) as BufferSource
    })
    
    return subscription
  } catch (error) {
    console.error('Push subscription failed:', error)
    return null
  }
}

// Отписаться от push-уведомлений
export async function unsubscribeFromPush(): Promise<boolean> {
  if (typeof window === 'undefined') return false
  
  try {
    const registration = await navigator.serviceWorker.ready
    const subscription = await registration.pushManager.getSubscription()
    
    if (subscription) {
      await subscription.unsubscribe()
      return true
    }
    return false
  } catch (error) {
    console.error('Push unsubscription failed:', error)
    return false
  }
}

// Получить текущую подписку
export async function getPushSubscription(): Promise<PushSubscription | null> {
  if (typeof window === 'undefined') return null
  
  try {
    const registration = await navigator.serviceWorker.ready
    return await registration.pushManager.getSubscription()
  } catch {
    return null
  }
}

// Показать локальное уведомление (для тестирования)
export function showLocalNotification(title: string, body: string, type: NotificationType = 'system'): void {
  if (typeof window === 'undefined' || !('Notification' in window)) return
  if (Notification.permission !== 'granted') return
  
  const settings = getNotificationSettings()
  if (!settings.enabled || !settings[type]) return
  
  new Notification(title, {
    body,
    icon: '/icons/icon-192x192.svg',
    tag: `hqstudio-${type}-${Date.now()}`
  })
}

// Утилита для конвертации VAPID ключа
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - base64String.length % 4) % 4)
  const base64 = (base64String + padding)
    .replace(/-/g, '+')
    .replace(/_/g, '/')
  
  const rawData = window.atob(base64)
  const outputArray = new Uint8Array(rawData.length)
  
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }
  return outputArray
}
