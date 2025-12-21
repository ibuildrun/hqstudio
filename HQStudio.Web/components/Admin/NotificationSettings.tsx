'use client'

import { useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Bell, BellOff, Smartphone, Shield, Package, MessageSquare, Settings } from 'lucide-react'
import {
  isPWASupported,
  isPWAInstalled,
  getNotificationPermission,
  requestNotificationPermission,
  registerServiceWorker,
  getNotificationSettings,
  saveNotificationSettings,
  showLocalNotification,
  NotificationSettings as NotificationSettingsType
} from '@/lib/pwa'

export default function NotificationSettings() {
  const [isSupported, setIsSupported] = useState(false)
  const [isInstalled, setIsInstalled] = useState(false)
  const [permission, setPermission] = useState<NotificationPermission | 'unsupported'>('default')
  const [settings, setSettings] = useState<NotificationSettingsType>(getNotificationSettings())
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    setIsSupported(isPWASupported())
    setIsInstalled(isPWAInstalled())
    setPermission(getNotificationPermission())
    
    // Регистрируем Service Worker
    registerServiceWorker()
  }, [])

  const handleEnableNotifications = async () => {
    setIsLoading(true)
    const granted = await requestNotificationPermission()
    setPermission(granted ? 'granted' : 'denied')
    
    if (granted) {
      const newSettings = { ...settings, enabled: true }
      setSettings(newSettings)
      saveNotificationSettings(newSettings)
      
      // Показываем тестовое уведомление
      setTimeout(() => {
        showLocalNotification('Уведомления включены', 'Теперь вы будете получать важные уведомления от HQ Studio', 'system')
      }, 500)
    }
    setIsLoading(false)
  }

  const handleDisableNotifications = () => {
    const newSettings = { ...settings, enabled: false }
    setSettings(newSettings)
    saveNotificationSettings(newSettings)
  }

  const toggleSetting = (key: keyof NotificationSettingsType) => {
    if (key === 'enabled') return
    const newSettings = { ...settings, [key]: !settings[key] }
    setSettings(newSettings)
    saveNotificationSettings(newSettings)
  }

  const testNotification = (type: 'callbacks' | 'orders' | 'security') => {
    const messages = {
      callbacks: { title: '📞 Новая заявка', body: 'Клиент Иван оставил заявку на шумоизоляцию' },
      orders: { title: '📦 Заказ обновлён', body: 'Заказ #123 переведён в статус "В работе"' },
      security: { title: '🔐 Вход в систему', body: 'Выполнен вход с нового устройства' }
    }
    showLocalNotification(messages[type].title, messages[type].body, type)
  }

  if (!isSupported) {
    return (
      <div className="bg-neutral-900/40 p-6 rounded-2xl border border-white/5">
        <div className="flex items-center gap-3 text-neutral-500">
          <BellOff size={20} />
          <span className="text-sm">Push-уведомления не поддерживаются в этом браузере</span>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* PWA Status */}
      <div className="bg-neutral-900/40 p-6 rounded-2xl border border-white/5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Smartphone size={20} className="text-white" />
            <span className="text-sm font-bold uppercase tracking-widest text-white">PWA Статус</span>
          </div>
          <span className={`text-[10px] uppercase tracking-widest px-3 py-1 rounded-full ${
            isInstalled ? 'bg-emerald-500/20 text-emerald-400' : 'bg-amber-500/20 text-amber-400'
          }`}>
            {isInstalled ? 'Установлено' : 'Браузер'}
          </span>
        </div>
        {!isInstalled && (
          <p className="text-[11px] text-neutral-500">
            Для лучшего опыта установите приложение на главный экран через меню браузера
          </p>
        )}
      </div>

      {/* Notification Permission */}
      <div className="bg-neutral-900/40 p-6 rounded-2xl border border-white/5">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <Bell size={20} className="text-white" />
            <span className="text-sm font-bold uppercase tracking-widest text-white">Уведомления</span>
          </div>
          {permission === 'granted' && settings.enabled ? (
            <button
              onClick={handleDisableNotifications}
              className="text-[10px] uppercase tracking-widest px-4 py-2 rounded-full bg-red-500/20 text-red-400 hover:bg-red-500/30 transition-colors"
            >
              Отключить
            </button>
          ) : permission === 'denied' ? (
            <span className="text-[10px] uppercase tracking-widest px-3 py-1 rounded-full bg-red-500/20 text-red-400">
              Заблокировано
            </span>
          ) : (
            <button
              onClick={handleEnableNotifications}
              disabled={isLoading}
              className="text-[10px] uppercase tracking-widest px-4 py-2 rounded-full bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30 transition-colors disabled:opacity-50"
            >
              {isLoading ? 'Подключение...' : 'Включить'}
            </button>
          )}
        </div>

        {permission === 'denied' && (
          <p className="text-[11px] text-red-400">
            Уведомления заблокированы. Разрешите их в настройках браузера.
          </p>
        )}
      </div>

      {/* Notification Types */}
      {permission === 'granted' && settings.enabled && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-neutral-900/40 p-6 rounded-2xl border border-white/5 space-y-4"
        >
          <div className="flex items-center gap-3 mb-4">
            <Settings size={20} className="text-white" />
            <span className="text-sm font-bold uppercase tracking-widest text-white">Типы уведомлений</span>
          </div>

          {/* Callbacks */}
          <div className="flex items-center justify-between p-4 bg-black/30 rounded-xl">
            <div className="flex items-center gap-3">
              <MessageSquare size={18} className="text-blue-400" />
              <div>
                <p className="text-sm font-medium text-white">Заявки</p>
                <p className="text-[10px] text-neutral-500">Новые заявки и обратные звонки</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => testNotification('callbacks')}
                className="text-[9px] uppercase tracking-widest px-2 py-1 rounded bg-neutral-800 text-neutral-400 hover:text-white transition-colors"
              >
                Тест
              </button>
              <button
                onClick={() => toggleSetting('callbacks')}
                className={`w-12 h-6 rounded-full transition-colors ${
                  settings.callbacks ? 'bg-emerald-500' : 'bg-neutral-700'
                }`}
              >
                <motion.div
                  animate={{ x: settings.callbacks ? 24 : 2 }}
                  className="w-5 h-5 bg-white rounded-full shadow"
                />
              </button>
            </div>
          </div>

          {/* Orders */}
          <div className="flex items-center justify-between p-4 bg-black/30 rounded-xl">
            <div className="flex items-center gap-3">
              <Package size={18} className="text-amber-400" />
              <div>
                <p className="text-sm font-medium text-white">Заказы</p>
                <p className="text-[10px] text-neutral-500">Изменения статусов заказов</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => testNotification('orders')}
                className="text-[9px] uppercase tracking-widest px-2 py-1 rounded bg-neutral-800 text-neutral-400 hover:text-white transition-colors"
              >
                Тест
              </button>
              <button
                onClick={() => toggleSetting('orders')}
                className={`w-12 h-6 rounded-full transition-colors ${
                  settings.orders ? 'bg-emerald-500' : 'bg-neutral-700'
                }`}
              >
                <motion.div
                  animate={{ x: settings.orders ? 24 : 2 }}
                  className="w-5 h-5 bg-white rounded-full shadow"
                />
              </button>
            </div>
          </div>

          {/* Security */}
          <div className="flex items-center justify-between p-4 bg-black/30 rounded-xl">
            <div className="flex items-center gap-3">
              <Shield size={18} className="text-red-400" />
              <div>
                <p className="text-sm font-medium text-white">Безопасность</p>
                <p className="text-[10px] text-neutral-500">Входы в систему, смена пароля</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => testNotification('security')}
                className="text-[9px] uppercase tracking-widest px-2 py-1 rounded bg-neutral-800 text-neutral-400 hover:text-white transition-colors"
              >
                Тест
              </button>
              <button
                onClick={() => toggleSetting('security')}
                className={`w-12 h-6 rounded-full transition-colors ${
                  settings.security ? 'bg-emerald-500' : 'bg-neutral-700'
                }`}
              >
                <motion.div
                  animate={{ x: settings.security ? 24 : 2 }}
                  className="w-5 h-5 bg-white rounded-full shadow"
                />
              </button>
            </div>
          </div>

          {/* System */}
          <div className="flex items-center justify-between p-4 bg-black/30 rounded-xl">
            <div className="flex items-center gap-3">
              <Settings size={18} className="text-neutral-400" />
              <div>
                <p className="text-sm font-medium text-white">Системные</p>
                <p className="text-[10px] text-neutral-500">Обновления и техническая информация</p>
              </div>
            </div>
            <button
              onClick={() => toggleSetting('system')}
              className={`w-12 h-6 rounded-full transition-colors ${
                settings.system ? 'bg-emerald-500' : 'bg-neutral-700'
              }`}
            >
              <motion.div
                animate={{ x: settings.system ? 24 : 2 }}
                className="w-5 h-5 bg-white rounded-full shadow"
              />
            </button>
          </div>
        </motion.div>
      )}
    </div>
  )
}
