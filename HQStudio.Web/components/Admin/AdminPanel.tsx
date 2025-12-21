'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useAdmin, UserRole } from '@/lib/store'
import { ServiceItem } from '@/lib/types'
import {
  X, Trash2, LogOut, Inbox, Edit3, History, User, Lock, ShieldCheck,
  Users, Layers, Mail, Key, Home, Palette, Camera, Wrench, Monitor, Upload,
  Eye, Star, Lightbulb, HelpCircle, Gift, Phone, Settings, Volume2, ExternalLink,
  ChevronUp, ChevronDown, EyeOff, LayoutGrid, GripVertical, Minimize2, Maximize2,
  ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Bell
} from 'lucide-react'
import SectionPreview from './SectionPreview'
import NotificationSettings from './NotificationSettings'

const AdminPanel: React.FC<{ onClose: () => void }> = ({ onClose }) => {
  const {
    data, updateData, logout, isAuthenticated, isLoading, login, currentUser,
    deleteRequest, updateService, addService, deleteService,
    deleteSubscription, toggleBlock, moveBlockUp, moveBlockDown, reorderBlocks,
    mustChangePassword, changePassword, loadUsers, deleteUser, addUser, loadActivityLog
  } = useAdmin()

  const [loginInput, setLoginInput] = useState('')
  const [passInput, setPassInput] = useState('')
  const [loginError, setLoginError] = useState(false)
  const [isLoggingIn, setIsLoggingIn] = useState(false)
  const [activeTab, setActiveTab] = useState<string>('requests')
  const [contentSubTab, setContentSubTab] = useState<string>('hero')
  const [editingService, setEditingService] = useState<ServiceItem | null>(null)
  const [isAddingUser, setIsAddingUser] = useState(false)
  const [newUser, setNewUser] = useState({ name: '', login: '', pass: '', role: 'EDITOR' as UserRole })
  const [showFullPreview, setShowFullPreview] = useState(false)
  const [draggedBlock, setDraggedBlock] = useState<number | null>(null)
  const [dragOverBlock, setDragOverBlock] = useState<number | null>(null)
  const [isMinimized, setIsMinimized] = useState(false)
  const [historyPage, setHistoryPage] = useState(1)
  const HISTORY_PER_PAGE = 10
  
  // Состояние для смены пароля
  const [currentPass, setCurrentPass] = useState('')
  const [newPass, setNewPass] = useState('')
  const [confirmPass, setConfirmPass] = useState('')
  const [changePassError, setChangePassError] = useState('')
  const [isChangingPass, setIsChangingPass] = useState(false)

  // Helper to check if a block is enabled
  const isBlockEnabled = (blockId: string) => {
    const block = data.siteBlocks?.find(b => b.id === blockId)
    return block?.enabled !== false
  }

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>, callback: (base64: string) => void) => {
    const file = e.target.files?.[0]
    if (file) {
      const reader = new FileReader()
      reader.onloadend = () => callback(reader.result as string)
      reader.readAsDataURL(file)
    }
  }

  const handleLogin = async (e?: React.FormEvent) => {
    if (e) e.preventDefault()
    if (isLoggingIn) return
    
    setIsLoggingIn(true)
    setLoginError(false)
    
    const success = await login(loginInput, passInput)
    
    if (!success) {
      setLoginError(true)
      setTimeout(() => setLoginError(false), 3000)
    }
    
    setIsLoggingIn(false)
  }

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    if (isChangingPass) return
    
    if (newPass !== confirmPass) {
      setChangePassError('Пароли не совпадают')
      return
    }
    if (newPass.length < 6) {
      setChangePassError('Пароль должен быть не менее 6 символов')
      return
    }
    
    setIsChangingPass(true)
    setChangePassError('')
    
    const result = await changePassword(currentPass, newPass)
    
    if (!result.success) {
      setChangePassError(result.error || 'Ошибка смены пароля')
    } else {
      setCurrentPass('')
      setNewPass('')
      setConfirmPass('')
    }
    
    setIsChangingPass(false)
  }

  // Модальное окно смены пароля (обязательное при первом входе)
  if (isAuthenticated && mustChangePassword) {
    return (
      <div className="fixed inset-0 z-[200] bg-black/90 backdrop-blur-xl flex items-center justify-center p-4">
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          className="w-full max-w-md bg-neutral-900 p-10 rounded-[40px] border border-white/10 shadow-2xl relative"
        >
          <div className="text-center mb-10">
            <div className="w-16 h-16 bg-amber-500 text-black rounded-full flex items-center justify-center mx-auto mb-6 shadow-xl">
              <Key size={24} />
            </div>
            <h2 className="text-2xl font-black uppercase tracking-tighter text-white">Смена пароля</h2>
            <p className="text-[10px] uppercase tracking-[0.4em] text-amber-500 font-bold mt-2">Обязательно при первом входе</p>
          </div>

          <form onSubmit={handleChangePassword} className="space-y-4">
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Текущий пароль</label>
              <input 
                type="password" 
                placeholder="••••••••" 
                value={currentPass} 
                onChange={(e) => setCurrentPass(e.target.value)} 
                disabled={isChangingPass}
                className="w-full bg-black border border-white/10 p-5 rounded-2xl text-white outline-none focus:border-white/30 transition-all shadow-inner disabled:opacity-50" 
              />
            </div>
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Новый пароль</label>
              <input 
                type="password" 
                placeholder="Минимум 6 символов" 
                value={newPass} 
                onChange={(e) => setNewPass(e.target.value)} 
                disabled={isChangingPass}
                className="w-full bg-black border border-white/10 p-5 rounded-2xl text-white outline-none focus:border-white/30 transition-all shadow-inner disabled:opacity-50" 
              />
            </div>
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Подтвердите пароль</label>
              <input 
                type="password" 
                placeholder="••••••••" 
                value={confirmPass} 
                onChange={(e) => setConfirmPass(e.target.value)} 
                disabled={isChangingPass}
                className="w-full bg-black border border-white/10 p-5 rounded-2xl text-white outline-none focus:border-white/30 transition-all shadow-inner disabled:opacity-50" 
              />
            </div>
            <AnimatePresence>
              {changePassError && (
                <motion.p initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} className="text-red-500 text-[10px] text-center font-bold uppercase tracking-widest">
                  {changePassError}
                </motion.p>
              )}
            </AnimatePresence>
            <button 
              type="submit" 
              disabled={isChangingPass || !currentPass || !newPass || !confirmPass}
              className="w-full bg-amber-500 text-black py-5 rounded-2xl font-black uppercase tracking-[0.3em] text-[10px] hover:bg-amber-400 transition-all active:scale-95 shadow-xl mt-4 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isChangingPass ? 'Сохранение...' : 'Сменить пароль'}
            </button>
          </form>

          <div className="mt-8 pt-8 border-t border-white/5 text-center">
            <button onClick={logout} className="text-[9px] text-neutral-500 uppercase tracking-widest hover:text-red-500 transition-colors">
              Выйти из системы
            </button>
          </div>
        </motion.div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="fixed inset-0 z-[200] bg-black/90 backdrop-blur-xl flex items-center justify-center p-4">
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          className="w-full max-w-md bg-neutral-900 p-10 rounded-[40px] border border-white/10 shadow-2xl relative"
        >
          <button onClick={onClose} className="absolute top-6 right-6 text-neutral-500 hover:text-white transition-colors">
            <X size={20} />
          </button>

          <div className="text-center mb-10">
            <div className="w-16 h-16 bg-white text-black rounded-full flex items-center justify-center mx-auto mb-6 shadow-xl shadow-white/5">
              <Lock size={24} />
            </div>
            <h2 className="text-2xl font-black uppercase tracking-tighter text-white">Админ-панель</h2>
            <p className="text-[10px] uppercase tracking-[0.4em] text-neutral-500 font-bold mt-2 italic">Только для сотрудников</p>
          </div>

          <form onSubmit={handleLogin} className="space-y-4">
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Логин</label>
              <input 
                type="text" 
                placeholder="Введите логин" 
                value={loginInput} 
                onChange={(e) => setLoginInput(e.target.value)} 
                disabled={isLoggingIn}
                className="w-full bg-black border border-white/10 p-5 rounded-2xl text-white outline-none focus:border-white/30 transition-all shadow-inner font-mono disabled:opacity-50" 
              />
            </div>
            <div className="space-y-1">
              <label className="text-[9px] uppercase tracking-widest text-neutral-500 ml-2 font-black">Пароль</label>
              <input 
                type="password" 
                placeholder="••••••••" 
                value={passInput} 
                onChange={(e) => setPassInput(e.target.value)} 
                disabled={isLoggingIn}
                className="w-full bg-black border border-white/10 p-5 rounded-2xl text-white outline-none focus:border-white/30 transition-all shadow-inner disabled:opacity-50" 
              />
            </div>
            <AnimatePresence>
              {loginError && (
                <motion.p initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }} className="text-red-500 text-[10px] text-center font-bold uppercase tracking-widest">
                  Неверный логин или пароль
                </motion.p>
              )}
            </AnimatePresence>
            <button 
              type="submit" 
              disabled={isLoggingIn || !loginInput || !passInput}
              className="w-full bg-white text-black py-5 rounded-2xl font-black uppercase tracking-[0.3em] text-[10px] hover:bg-neutral-200 transition-all active:scale-95 shadow-xl shadow-white/5 mt-4 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoggingIn ? 'Вход...' : 'Войти в систему'}
            </button>
          </form>

          <div className="mt-8 pt-8 border-t border-white/5 text-center">
            <p className="text-[9px] text-neutral-600 uppercase tracking-widest">
              Доступ только для авторизованных сотрудников HQ Studio
            </p>
          </div>
        </motion.div>
      </div>
    )
  }

  const isAdmin = currentUser?.role === 'ADMIN'
  const isEditor = currentUser?.role === 'EDITOR' || isAdmin

  const contentSections = [
    { id: 'hero', label: 'Главная', icon: Home, blockId: 'hero' },
    { id: 'ticker', label: 'Бегущая строка', icon: Palette, blockId: 'ticker' },
    { id: 'manifest', label: 'Манифест', icon: Palette, blockId: 'manifest' },
    { id: 'quality', label: 'Качество', icon: Palette, blockId: 'quality' },
    { id: 'gallery', label: 'Галерея', icon: Camera, blockId: 'gallery' },
    { id: 'sound', label: 'Звук', icon: Volume2, blockId: 'sound' },
    { id: 'process', label: 'Процесс', icon: Wrench, blockId: 'process' },
    { id: 'lookbook', label: 'Lookbook', icon: Camera, blockId: 'showcase' },
    { id: 'testimonials', label: 'Отзывы', icon: Star, blockId: 'testimonials' },
    { id: 'faq', label: 'FAQ', icon: HelpCircle, blockId: 'faq' },
    { id: 'newsletter', label: 'Рассылка', icon: Mail, blockId: 'newsletter' },
    { id: 'contact', label: 'Контакты', icon: Phone, blockId: 'contact' },
    { id: 'moodlight', label: 'Подсветка', icon: Lightbulb, blockId: 'moodlight' },
    { id: 'game', label: 'Промо-игра', icon: Gift, blockId: 'game' },
    { id: 'settings', label: 'Настройки', icon: Settings, blockId: null },
  ]

  // Minimized floating button
  if (isMinimized) {
    return (
      <motion.div
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        className="fixed bottom-6 right-6 z-[200] flex items-center gap-2"
      >
        <button
          onClick={() => setIsMinimized(false)}
          className="flex items-center gap-3 px-5 py-3 bg-black border border-white/20 rounded-full shadow-2xl hover:bg-white hover:text-black transition-all group"
        >
          <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
          <span className="text-[10px] font-bold uppercase tracking-widest text-white group-hover:text-black">Админ-панель</span>
          <Maximize2 size={14} className="text-white/60 group-hover:text-black" />
        </button>
        <button
          onClick={onClose}
          className="p-3 bg-black border border-white/20 rounded-full text-white/60 hover:bg-red-500 hover:text-white hover:border-red-500 transition-all"
        >
          <X size={14} />
        </button>
      </motion.div>
    )
  }

  return (
    <div className="fixed inset-0 z-[200] bg-black flex flex-col h-full overflow-hidden font-sans">
      {/* Header */}
      <div className="flex-none bg-black border-b border-white/5 p-2 md:p-4 flex flex-col md:flex-row items-start md:items-center justify-between z-10 shadow-xl gap-2">
        <div className="flex items-center justify-between w-full md:w-auto">
          <div className="flex flex-col">
            <h1 className="text-xs md:text-sm font-black uppercase tracking-tighter text-white">HQ_STUDIO / CMD</h1>
            <span className="text-[7px] md:text-[8px] text-neutral-500 uppercase tracking-widest font-bold">{currentUser?.name}</span>
          </div>
          <div className="flex gap-1 md:hidden">
            <button onClick={() => setIsMinimized(true)} className="p-2 bg-neutral-900 text-neutral-500 hover:text-white rounded-full transition-colors"><Minimize2 size={14} /></button>
            <button onClick={logout} className="p-2 bg-neutral-900 text-neutral-500 hover:text-red-500 rounded-full transition-colors"><LogOut size={14} /></button>
            <button onClick={onClose} className="p-2 bg-neutral-900 text-white rounded-full hover:bg-white hover:text-black transition-colors"><X size={14} /></button>
          </div>
        </div>

        <div className="flex bg-neutral-900 rounded-full p-1 gap-0.5 md:gap-1 overflow-x-auto w-full md:w-auto md:max-w-[60vw] scrollbar-hide">
          {isEditor && (
            <button onClick={() => setActiveTab('general')} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'general' ? 'bg-white text-black' : 'text-neutral-500'}`}><Monitor size={12} className="hidden md:block" /> Контент</button>
          )}
          {isEditor && (
            <button onClick={() => setActiveTab('blocks')} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'blocks' ? 'bg-white text-black' : 'text-neutral-500'}`}><LayoutGrid size={12} className="hidden md:block" /> Блоки</button>
          )}
          <button onClick={() => setActiveTab('services')} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'services' ? 'bg-white text-black' : 'text-neutral-500'}`}><Layers size={12} className="hidden md:block" /> Услуги</button>
          <button onClick={() => setActiveTab('requests')} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'requests' ? 'bg-white text-black' : 'text-neutral-500'}`}><Inbox size={12} className="hidden md:block" /> Заявки</button>
          <button onClick={() => setActiveTab('subscriptions')} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'subscriptions' ? 'bg-white text-black' : 'text-neutral-500'}`}><Mail size={12} className="hidden md:block" /> Почты</button>
          {isAdmin && (
            <button onClick={() => { setActiveTab('users'); loadUsers(); }} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'users' ? 'bg-white text-black' : 'text-neutral-500'}`}><Users size={12} className="hidden md:block" /> Команда</button>
          )}
          <button onClick={() => { setActiveTab('history'); loadActivityLog(); }} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'history' ? 'bg-white text-black' : 'text-neutral-500'}`}><History size={12} className="hidden md:block" /> Аудит</button>
          <button onClick={() => setActiveTab('notifications')} className={`flex items-center gap-1 md:gap-2 text-[8px] md:text-[10px] uppercase font-bold px-2 md:px-4 py-1.5 md:py-2 rounded-full whitespace-nowrap transition-all ${activeTab === 'notifications' ? 'bg-white text-black' : 'text-neutral-500'}`}><Bell size={12} className="hidden md:block" /> Уведомления</button>
        </div>

        <div className="hidden md:flex gap-2">
          <button onClick={() => setIsMinimized(true)} className="p-3 bg-neutral-900 text-neutral-500 hover:text-white rounded-full transition-colors" title="Свернуть"><Minimize2 size={16} /></button>
          <button onClick={logout} className="p-3 bg-neutral-900 text-neutral-500 hover:text-red-500 rounded-full transition-colors"><LogOut size={16} /></button>
          <button onClick={onClose} className="p-3 bg-neutral-900 text-white rounded-full hover:bg-white hover:text-black transition-colors"><X size={16} /></button>
        </div>
      </div>

      <div className="flex-1 overflow-hidden flex flex-col md:flex-row bg-neutral-950">
        {/* Sidebar for Content tab - horizontal scroll on mobile */}
        {activeTab === 'general' && (
          <div className="md:w-64 md:min-w-[256px] bg-black border-b md:border-b-0 md:border-r border-white/5 p-2 md:p-4 flex md:flex-col gap-1 overflow-x-auto md:overflow-y-auto md:max-h-[calc(100vh-80px)] scrollbar-hide">
            {contentSections.map(section => {
              const enabled = section.blockId ? isBlockEnabled(section.blockId) : true
              return (
                <button 
                  key={section.id} 
                  onClick={() => setContentSubTab(section.id)} 
                  className={`px-3 md:px-4 py-2 md:py-3 rounded-lg md:rounded-xl text-[9px] md:text-[10px] uppercase font-bold tracking-widest text-left transition-all flex items-center gap-1.5 md:gap-2 shrink-0 whitespace-nowrap md:w-full ${
                    contentSubTab === section.id 
                      ? 'bg-white text-black shadow-lg' 
                      : enabled 
                        ? 'text-neutral-500 hover:bg-white/5' 
                        : 'text-neutral-700 hover:bg-white/5'
                  }`}
                >
                  <section.icon size={12} className="md:w-[14px] md:h-[14px]" />
                  <span className="flex-1">{section.label}</span>
                  {section.blockId && (
                    <span className={`w-1.5 h-1.5 rounded-full ${
                      enabled ? 'bg-emerald-500' : 'bg-red-500'
                    }`} title={enabled ? 'Отображается' : 'Скрыт'} />
                  )}
                </button>
              )
            })}
          </div>
        )}

        <div className="flex-1 overflow-y-auto p-4 md:p-8">
          <div className="max-w-4xl mx-auto">

            {/* Content Tab */}
            {activeTab === 'general' && (
              <div className="space-y-6 md:space-y-8 pb-12">
                {/* Section Header with Preview Button */}
                <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-3">
                  <h2 className="text-lg md:text-xl font-black uppercase text-white">{contentSections.find(s => s.id === contentSubTab)?.label}</h2>
                  {contentSubTab !== 'settings' && (
                    <button 
                      onClick={() => setShowFullPreview(true)} 
                      className="flex items-center gap-2 px-4 md:px-5 py-2 md:py-2.5 rounded-full text-[9px] md:text-[10px] uppercase font-bold transition-all bg-gradient-to-r from-neutral-800 to-neutral-900 text-white hover:from-white hover:to-neutral-100 hover:text-black border border-white/10 hover:border-white shadow-lg"
                    >
                      <Eye size={12} /> Превью
                      <ExternalLink size={10} />
                    </button>
                  )}
                </div>

                {/* Hero Section */}
                {contentSubTab === 'hero' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок H1</label>
                        <input type="text" value={data.heroTitle} onChange={(e) => updateData({ heroTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                        <textarea value={data.heroSubtitle} onChange={(e) => updateData({ heroSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all min-h-[100px]" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Фоновое изображение</label>
                        <p className="text-[9px] text-neutral-600 mb-3">Рекомендуемый размер: 1920×1080px (16:9), формат JPG/WebP</p>
                        <div className="space-y-3">
                          <label className="flex items-center gap-3 px-5 py-4 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 rounded-xl cursor-pointer hover:bg-emerald-500/20 transition-all">
                            <Upload size={18} />
                            <span className="text-[10px] uppercase font-bold tracking-widest">Загрузить изображение</span>
                            <input type="file" className="hidden" accept="image/*" onChange={(e) => handleFileUpload(e, (base64) => updateData({ heroImage: base64 }))} />
                          </label>
                          <div className="flex items-center gap-3">
                            <div className="flex-1 h-px bg-white/10" />
                            <span className="text-[8px] text-neutral-600 uppercase">или вставьте ссылку</span>
                            <div className="flex-1 h-px bg-white/10" />
                          </div>
                          <div>
                            <input type="text" value={data.heroImage} onChange={(e) => updateData({ heroImage: e.target.value })} className="w-full bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm" placeholder="https://..." />
                            <p className="text-[8px] text-amber-500/70 mt-1">⚠ Внешние ссылки могут перестать работать</p>
                          </div>
                          {data.heroImage && (
                            <div className="relative aspect-video rounded-xl overflow-hidden border border-white/10">
                              <img src={data.heroImage} alt="Preview" className="w-full h-full object-cover" />
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* Ticker Section */}
                {contentSubTab === 'ticker' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-6 md:p-8 rounded-3xl border border-white/5 space-y-5">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Основной текст</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Текст бегущей строки</label>
                        <input type="text" value={data.aboutTickerText} onChange={(e) => updateData({ aboutTickerText: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        <p className="text-[10px] text-neutral-600 mt-2">Используйте • для разделения слов. Пример: ШУМОИЗОЛЯЦИЯ • АНТИХРОМ • АВТОСВЕТ</p>
                      </div>
                    </div>
                    <div className="bg-neutral-900/40 p-6 md:p-8 rounded-3xl border border-white/5 space-y-5">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Подпись под бегущей строкой</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Полный текст подписи</label>
                        <input 
                          type="text" 
                          value={data.aboutTickerDisclaimer || ''} 
                          onChange={(e) => updateData({ aboutTickerDisclaimer: e.target.value })} 
                          className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" 
                          placeholder="Игонин Павел Васильевич. Информация на сайте не является публичной офертой. 2025 г." 
                        />
                        <p className="text-[10px] text-neutral-600 mt-2">Введите полный текст подписи как он должен отображаться</p>
                      </div>
                    </div>
                  </div>
                )}

                {/* Manifest Section */}
                {contentSubTab === 'manifest' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-6 md:p-8 rounded-3xl border border-white/5 space-y-5">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Заголовки</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок (маленький текст сверху)</label>
                        <input type="text" value={data.aboutManifestSubtitle || ''} onChange={(e) => updateData({ aboutManifestSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Искусство детализации" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок (большой текст)</label>
                        <input type="text" value={data.aboutManifestTitle || ''} onChange={(e) => updateData({ aboutManifestTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Ваш комфорт — наша работа" />
                      </div>
                    </div>
                    <div className="bg-neutral-900/40 p-6 md:p-8 rounded-3xl border border-white/5 space-y-5">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Основной текст</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Текст манифеста</label>
                        <textarea value={data.aboutManifestText} onChange={(e) => updateData({ aboutManifestText: e.target.value })} rows={6} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Философия и миссия компании..." />
                        <p className="text-[10px] text-neutral-600 mt-2">Опишите философию и миссию вашей компании</p>
                      </div>
                    </div>
                  </div>
                )}

                {/* Quality Section */}
                {contentSubTab === 'quality' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-4">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Качество материалов</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок</label>
                        <input type="text" value={data.aboutQualityTitle} onChange={(e) => updateData({ aboutQualityTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Описание</label>
                        <textarea value={data.aboutQualityDesc} onChange={(e) => updateData({ aboutQualityDesc: e.target.value })} rows={4} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                    </div>
                  </div>
                )}

                {/* Gallery Section */}
                {contentSubTab === 'gallery' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок галереи</label>
                        <input type="text" value={data.galleryTitle} onChange={(e) => updateData({ galleryTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                        <input type="text" value={data.gallerySubtitle} onChange={(e) => updateData({ gallerySubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Изображение галереи</label>
                        <p className="text-[9px] text-neutral-600 mb-3">Рекомендуемый размер: 1920×1080px (16:9), формат JPG/WebP</p>
                        <div className="space-y-3">
                          <label className="flex items-center gap-3 px-5 py-4 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 rounded-xl cursor-pointer hover:bg-emerald-500/20 transition-all">
                            <Upload size={18} />
                            <span className="text-[10px] uppercase font-bold tracking-widest">Загрузить изображение</span>
                            <input type="file" className="hidden" accept="image/*" onChange={(e) => handleFileUpload(e, (base64) => updateData({ galleryImage: base64 }))} />
                          </label>
                          <div className="flex items-center gap-3">
                            <div className="flex-1 h-px bg-white/10" />
                            <span className="text-[8px] text-neutral-600 uppercase">или вставьте ссылку</span>
                            <div className="flex-1 h-px bg-white/10" />
                          </div>
                          <div>
                            <input type="text" value={data.galleryImage} onChange={(e) => updateData({ galleryImage: e.target.value })} className="w-full bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm" placeholder="https://..." />
                            <p className="text-[8px] text-amber-500/70 mt-1">⚠ Внешние ссылки могут перестать работать</p>
                          </div>
                          {data.galleryImage && (
                            <div className="relative aspect-video rounded-xl overflow-hidden border border-white/10">
                              <img src={data.galleryImage} alt="Preview" className="w-full h-full object-cover" />
                            </div>
                          )}
                        </div>
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Особенности студии</label>
                        {data.galleryFeatures?.map((feature: string, idx: number) => (
                          <div key={idx} className="flex gap-2 mb-2">
                            <input type="text" value={feature} onChange={(e) => {
                              const features = [...(data.galleryFeatures || [])]
                              features[idx] = e.target.value
                              updateData({ galleryFeatures: features })
                            }} className="flex-1 bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm" />
                            <button onClick={() => updateData({ galleryFeatures: data.galleryFeatures?.filter((_: any, i: number) => i !== idx) })} className="p-3 text-neutral-600 hover:text-red-500 rounded-xl hover:bg-red-500/10 transition-all"><Trash2 size={16} /></button>
                          </div>
                        ))}
                        <button onClick={() => updateData({ galleryFeatures: [...(data.galleryFeatures || []), 'Новая особенность'] })} className="w-full py-3 border border-dashed border-white/10 rounded-xl text-neutral-500 hover:text-white hover:border-white/30 transition-all text-[10px] uppercase font-bold">+ Добавить</button>
                      </div>
                    </div>
                  </div>
                )}

                {/* Sound Experience Section */}
                {contentSubTab === 'sound' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Обычный автомобиль</h3>
                      <input type="text" value={data.soundExpNoisyTitle} onChange={(e) => updateData({ soundExpNoisyTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Заголовок" />
                      <textarea value={data.soundExpNoisyDesc} onChange={(e) => updateData({ soundExpNoisyDesc: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Описание" />
                    </div>
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">После обработки HQ</h3>
                      <input type="text" value={data.soundExpQuietTitle} onChange={(e) => updateData({ soundExpQuietTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Заголовок" />
                      <textarea value={data.soundExpQuietDesc} onChange={(e) => updateData({ soundExpQuietDesc: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Описание" />
                    </div>
                  </div>
                )}

                {/* Process Section */}
                {contentSubTab === 'process' && (
                  <div className="space-y-6">
                    <div className="grid gap-4">
                      {data.processSteps?.map((step: any, idx: number) => (
                        <div key={idx} className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 space-y-3">
                          <div className="flex items-center gap-4">
                            <span className="text-3xl font-black text-white/20">{step.num}</span>
                            <input type="text" value={step.title} onChange={(e) => { const steps = [...data.processSteps]; steps[idx].title = e.target.value; updateData({ processSteps: steps }) }} className="flex-1 bg-transparent border-b border-white/10 outline-none text-white font-bold py-1 focus:border-white/30" />
                            <button onClick={() => updateData({ processSteps: data.processSteps.filter((_: any, i: number) => i !== idx) })} className="p-2 text-neutral-600 hover:text-red-500 rounded-full hover:bg-red-500/10 transition-all"><Trash2 size={16} /></button>
                          </div>
                          <textarea value={step.text} onChange={(e) => { const steps = [...data.processSteps]; steps[idx].text = e.target.value; updateData({ processSteps: steps }) }} className="w-full bg-black border border-white/10 p-3 rounded-xl text-neutral-400 text-sm outline-none focus:border-white/30 transition-all" />
                        </div>
                      ))}
                      <button onClick={() => updateData({ processSteps: [...data.processSteps, { num: String(data.processSteps.length + 1).padStart(2, '0'), title: 'Новый этап', text: 'Описание этапа' }] })} className="w-full py-6 border-2 border-dashed border-white/5 rounded-3xl text-neutral-500 hover:text-white hover:border-white/20 transition-all uppercase text-[10px] font-bold tracking-widest">+ Добавить этап</button>
                    </div>
                  </div>
                )}

                {/* Lookbook Section */}
                {contentSubTab === 'lookbook' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-6 md:p-8 rounded-3xl border border-white/5 space-y-5">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Заголовки секции</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                        <input type="text" value={data.showcaseSubtitle || ''} onChange={(e) => updateData({ showcaseSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Наше портфолио" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок (используйте \n для переноса строки)</label>
                        <input type="text" value={data.showcaseTitle || ''} onChange={(e) => updateData({ showcaseTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="LOOK\nBOOK" />
                        <p className="text-[10px] text-neutral-600 mt-2">Используйте \n для переноса строки. Например: LOOK\nBOOK</p>
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Описание</label>
                        <textarea value={data.showcaseDescription || ''} onChange={(e) => updateData({ showcaseDescription: e.target.value })} rows={3} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Листайте горизонтально, чтобы увидеть результаты нашей работы в деталях." />
                      </div>
                    </div>
                    <h3 className="text-xs font-bold uppercase text-neutral-500">Проекты портфолио</h3>
                    <div className="grid gap-4">
                      {data.showcaseItems?.map((item: any, idx: number) => (
                        <div key={idx} className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 flex gap-6 items-center hover:bg-neutral-900/60 transition-all group">
                          <div className="w-24 h-24 bg-black rounded-2xl overflow-hidden relative flex items-center justify-center border border-white/5">
                            {item.img ? <img src={item.img} className="w-full h-full object-cover grayscale group-hover:grayscale-0 transition-all" /> : <Camera size={24} className="text-neutral-700" />}
                            <label className="absolute inset-0 bg-black/70 opacity-0 group-hover:opacity-100 flex items-center justify-center cursor-pointer transition-opacity">
                              <Upload size={16} className="text-white" />
                              <input type="file" className="hidden" accept="image/*" onChange={(e) => handleFileUpload(e, (base64) => {
                                const items = [...data.showcaseItems]
                                items[idx].img = base64
                                updateData({ showcaseItems: items })
                              })} />
                            </label>
                          </div>
                          <div className="flex-1 space-y-3">
                            <input type="text" value={item.title} onChange={(e) => { const items = [...data.showcaseItems]; items[idx].title = e.target.value; updateData({ showcaseItems: items }) }} className="w-full bg-transparent border-b border-white/10 outline-none text-white font-bold py-1 focus:border-white/30" />
                            <input type="text" value={item.desc} onChange={(e) => { const items = [...data.showcaseItems]; items[idx].desc = e.target.value; updateData({ showcaseItems: items }) }} className="w-full bg-transparent border-b border-white/10 outline-none text-neutral-500 text-[10px] uppercase tracking-widest py-1 focus:border-white/30" />
                          </div>
                          <button onClick={() => updateData({ showcaseItems: data.showcaseItems.filter((_: any, i: number) => i !== idx) })} className="p-3 text-neutral-700 hover:text-red-500 hover:bg-red-500/5 rounded-full transition-all"><Trash2 size={18} /></button>
                        </div>
                      ))}
                      <button onClick={() => updateData({ showcaseItems: [...data.showcaseItems, { title: 'Новый проект', desc: 'Краткое описание', img: '' }] })} className="w-full py-6 border-2 border-dashed border-white/5 rounded-3xl text-neutral-500 hover:text-white hover:border-white/20 transition-all uppercase text-[10px] font-bold tracking-widest">+ Добавить проект</button>
                    </div>
                  </div>
                )}

                {/* Testimonials Section */}
                {contentSubTab === 'testimonials' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-4">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Заголовки секции</h3>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок</label>
                          <input type="text" value={data.testimonialsTitle || ''} onChange={(e) => updateData({ testimonialsTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                          <input type="text" value={data.testimonialsSubtitle || ''} onChange={(e) => updateData({ testimonialsSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Рейтинг</label>
                          <input type="text" value={data.testimonialsRating || ''} onChange={(e) => updateData({ testimonialsRating: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Кол-во отзывов</label>
                          <input type="text" value={data.testimonialsCount || ''} onChange={(e) => updateData({ testimonialsCount: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                      </div>
                    </div>
                    <h3 className="text-xs font-bold uppercase text-neutral-500 mt-8">Отзывы клиентов</h3>
                    <div className="grid gap-4">
                      {data.testimonials?.map((t: any, idx: number) => (
                        <div key={idx} className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 space-y-4">
                          <div className="flex gap-4">
                            <input type="text" value={t.name} onChange={(e) => { const items = [...(data.testimonials || [])]; items[idx].name = e.target.value; updateData({ testimonials: items }) }} className="flex-1 bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Имя" />
                            <input type="text" value={t.car} onChange={(e) => { const items = [...(data.testimonials || [])]; items[idx].car = e.target.value; updateData({ testimonials: items }) }} className="flex-1 bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Автомобиль" />
                            <button onClick={() => updateData({ testimonials: data.testimonials?.filter((_: any, i: number) => i !== idx) })} className="p-3 text-neutral-600 hover:text-red-500 rounded-xl hover:bg-red-500/10 transition-all"><Trash2 size={16} /></button>
                          </div>
                          <textarea value={t.text} onChange={(e) => { const items = [...(data.testimonials || [])]; items[idx].text = e.target.value; updateData({ testimonials: items }) }} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all min-h-[80px]" placeholder="Текст отзыва" />
                        </div>
                      ))}
                      <button onClick={() => updateData({ testimonials: [...(data.testimonials || []), { name: 'Имя', car: 'Автомобиль', text: 'Текст отзыва' }] })} className="w-full py-6 border-2 border-dashed border-white/5 rounded-3xl text-neutral-500 hover:text-white hover:border-white/20 transition-all uppercase text-[10px] font-bold tracking-widest">+ Добавить отзыв</button>
                    </div>
                  </div>
                )}

                {/* FAQ Section */}
                {contentSubTab === 'faq' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-4">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Заголовки секции</h3>
                      <input type="text" value={data.faqTitle || ''} onChange={(e) => updateData({ faqTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Заголовок" />
                      <input type="text" value={data.faqSubtitle || ''} onChange={(e) => updateData({ faqSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Подзаголовок" />
                      <textarea value={data.faqDescription || ''} onChange={(e) => updateData({ faqDescription: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Описание" />
                    </div>
                    <h3 className="text-xs font-bold uppercase text-neutral-500 mt-8">Вопросы и ответы</h3>
                    <div className="grid gap-4">
                      {data.faqItems?.map((faq: any, idx: number) => (
                        <div key={idx} className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 space-y-4">
                          <div className="flex gap-4 items-start">
                            <div className="flex-1 space-y-3">
                              <input type="text" value={faq.q} onChange={(e) => { const items = [...data.faqItems]; items[idx].q = e.target.value; updateData({ faqItems: items }) }} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Вопрос" />
                              <textarea value={faq.a} onChange={(e) => { const items = [...data.faqItems]; items[idx].a = e.target.value; updateData({ faqItems: items }) }} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all min-h-[80px]" placeholder="Ответ" />
                            </div>
                            <button onClick={() => updateData({ faqItems: data.faqItems.filter((_: any, i: number) => i !== idx) })} className="p-3 text-neutral-600 hover:text-red-500 rounded-xl hover:bg-red-500/10 transition-all"><Trash2 size={16} /></button>
                          </div>
                        </div>
                      ))}
                      <button onClick={() => updateData({ faqItems: [...data.faqItems, { q: 'Новый вопрос?', a: 'Ответ на вопрос' }] })} className="w-full py-6 border-2 border-dashed border-white/5 rounded-3xl text-neutral-500 hover:text-white hover:border-white/20 transition-all uppercase text-[10px] font-bold tracking-widest">+ Добавить вопрос</button>
                    </div>
                  </div>
                )}

                {/* Newsletter Section */}
                {contentSubTab === 'newsletter' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок</label>
                        <input type="text" value={data.newsletterTitle || ''} onChange={(e) => updateData({ newsletterTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                        <input type="text" value={data.newsletterSubtitle || ''} onChange={(e) => updateData({ newsletterSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Описание</label>
                        <textarea value={data.newsletterDescription || ''} onChange={(e) => updateData({ newsletterDescription: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all min-h-[100px]" />
                      </div>
                    </div>
                  </div>
                )}

                {/* Contact Section */}
                {contentSubTab === 'contact' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Основная информация</h3>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Название компании</label>
                          <input type="text" value={data.appName} onChange={(e) => updateData({ appName: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Владелец</label>
                          <input type="text" value={data.ownerName} onChange={(e) => updateData({ ownerName: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Телефон</label>
                          <input type="text" value={data.phone} onChange={(e) => updateData({ phone: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Адрес</label>
                          <input type="text" value={data.address} onChange={(e) => updateData({ address: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                      </div>
                    </div>
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Тексты секции</h3>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок</label>
                        <input type="text" value={data.contactTitle || ''} onChange={(e) => updateData({ contactTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Описание</label>
                        <textarea value={data.contactDescription || ''} onChange={(e) => updateData({ contactDescription: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all min-h-[80px]" />
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок формы</label>
                          <input type="text" value={data.contactFormTitle || ''} onChange={(e) => updateData({ contactFormTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                        <div>
                          <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок формы</label>
                          <input type="text" value={data.contactFormSubtitle || ''} onChange={(e) => updateData({ contactFormSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* MoodLight Section */}
                {contentSubTab === 'moodlight' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок</label>
                        <input type="text" value={data.moodlightTitle || ''} onChange={(e) => updateData({ moodlightTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                        <input type="text" value={data.moodlightSubtitle || ''} onChange={(e) => updateData({ moodlightSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Изображение</label>
                        <p className="text-[9px] text-neutral-600 mb-3">Рекомендуемый размер: 1200×800px, формат JPG/WebP</p>
                        <div className="space-y-3">
                          <label className="flex items-center gap-3 px-5 py-4 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 rounded-xl cursor-pointer hover:bg-emerald-500/20 transition-all">
                            <Upload size={18} />
                            <span className="text-[10px] uppercase font-bold tracking-widest">Загрузить изображение</span>
                            <input type="file" className="hidden" accept="image/*" onChange={(e) => handleFileUpload(e, (base64) => updateData({ moodlightImage: base64 }))} />
                          </label>
                          <div className="flex items-center gap-3">
                            <div className="flex-1 h-px bg-white/10" />
                            <span className="text-[8px] text-neutral-600 uppercase">или вставьте ссылку</span>
                            <div className="flex-1 h-px bg-white/10" />
                          </div>
                          <div>
                            <input type="text" value={data.moodlightImage || ''} onChange={(e) => updateData({ moodlightImage: e.target.value })} className="w-full bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm" placeholder="https://..." />
                            <p className="text-[8px] text-amber-500/70 mt-1">⚠ Внешние ссылки могут перестать работать</p>
                          </div>
                          {data.moodlightImage && (
                            <div className="relative aspect-video rounded-xl overflow-hidden border border-white/10">
                              <img src={data.moodlightImage} alt="Preview" className="w-full h-full object-cover" />
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                    <h3 className="text-xs font-bold uppercase text-neutral-500 mt-8">Режимы подсветки</h3>
                    <div className="grid gap-4">
                      {data.moodlightModes?.map((mode: any, idx: number) => (
                        <div key={idx} className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 space-y-4">
                          <div className="flex gap-4">
                            <input type="text" value={mode.name} onChange={(e) => { const items = [...(data.moodlightModes || [])]; items[idx].name = e.target.value; updateData({ moodlightModes: items }) }} className="flex-1 bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Название" />
                            <input type="text" value={mode.color} onChange={(e) => { const items = [...(data.moodlightModes || [])]; items[idx].color = e.target.value; updateData({ moodlightModes: items }) }} className="w-48 bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm" placeholder="Цвет (rgba)" />
                            <button onClick={() => updateData({ moodlightModes: data.moodlightModes?.filter((_: any, i: number) => i !== idx) })} className="p-3 text-neutral-600 hover:text-red-500 rounded-xl hover:bg-red-500/10 transition-all"><Trash2 size={16} /></button>
                          </div>
                          <input type="text" value={mode.desc} onChange={(e) => { const items = [...(data.moodlightModes || [])]; items[idx].desc = e.target.value; updateData({ moodlightModes: items }) }} className="w-full bg-black border border-white/10 p-3 rounded-xl text-white outline-none focus:border-white/30 transition-all" placeholder="Описание" />
                        </div>
                      ))}
                      <button onClick={() => updateData({ moodlightModes: [...(data.moodlightModes || []), { id: Date.now().toString(), name: 'Новый режим', color: 'rgba(255, 255, 255, 0.4)', desc: 'Описание режима' }] })} className="w-full py-6 border-2 border-dashed border-white/5 rounded-3xl text-neutral-500 hover:text-white hover:border-white/20 transition-all uppercase text-[10px] font-bold tracking-widest">+ Добавить режим</button>
                    </div>
                  </div>
                )}

                {/* Promo Game Section */}
                {contentSubTab === 'game' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Заголовок</label>
                        <input type="text" value={data.gameTitle || ''} onChange={(e) => updateData({ gameTitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Подзаголовок</label>
                        <input type="text" value={data.gameSubtitle || ''} onChange={(e) => updateData({ gameSubtitle: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Описание</label>
                        <textarea value={data.gameDescription || ''} onChange={(e) => updateData({ gameDescription: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all min-h-[80px]" />
                      </div>
                      <div>
                        <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Текст кнопки</label>
                        <input type="text" value={data.gameButtonText || ''} onChange={(e) => updateData({ gameButtonText: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all" />
                      </div>
                    </div>
                  </div>
                )}

                {/* Settings Section */}
                {contentSubTab === 'settings' && (
                  <div className="space-y-6">
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-6">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Настройки формы</h3>
                      <div className="flex flex-col md:flex-row gap-4 md:gap-6">
                        <label className="flex items-center gap-3 cursor-pointer">
                          <input type="checkbox" checked={data.formConfig?.showCarModel} onChange={() => updateData({ formConfig: { ...data.formConfig, showCarModel: !data.formConfig?.showCarModel } })} className="w-5 h-5 rounded accent-white" />
                          <span className="text-sm text-white">Показывать поле "Модель авто"</span>
                        </label>
                        <label className="flex items-center gap-3 cursor-pointer">
                          <input type="checkbox" checked={data.formConfig?.showLicensePlate} onChange={() => updateData({ formConfig: { ...data.formConfig, showLicensePlate: !data.formConfig?.showLicensePlate } })} className="w-5 h-5 rounded accent-white" />
                          <span className="text-sm text-white">Показывать поле "Госномер"</span>
                        </label>
                      </div>
                    </div>
                    <div className="bg-neutral-900/40 p-8 rounded-3xl border border-white/5 space-y-4">
                      <h3 className="text-xs font-bold uppercase text-neutral-500">Рекомендации по изображениям</h3>
                      <div className="grid gap-3 text-[11px] text-neutral-400">
                        <div className="flex justify-between items-center p-3 bg-black/30 rounded-xl">
                          <span>Главный экран (Hero)</span>
                          <span className="text-white font-mono">1920 × 1080px</span>
                        </div>
                        <div className="flex justify-between items-center p-3 bg-black/30 rounded-xl">
                          <span>Галерея студии</span>
                          <span className="text-white font-mono">1920 × 1080px</span>
                        </div>
                        <div className="flex justify-between items-center p-3 bg-black/30 rounded-xl">
                          <span>Ambient Light</span>
                          <span className="text-white font-mono">1200 × 800px</span>
                        </div>
                        <div className="flex justify-between items-center p-3 bg-black/30 rounded-xl">
                          <span>Услуги</span>
                          <span className="text-white font-mono">800 × 600px</span>
                        </div>
                        <div className="flex justify-between items-center p-3 bg-black/30 rounded-xl">
                          <span>Портфолио (Lookbook)</span>
                          <span className="text-white font-mono">1200 × 800px</span>
                        </div>
                      </div>
                      <p className="text-[10px] text-neutral-600">Форматы: JPG, PNG, WebP. Рекомендуется WebP для лучшей производительности.</p>
                    </div>
                    <div className="bg-neutral-900/20 p-6 rounded-2xl border border-white/5">
                      <p className="text-[10px] text-neutral-500 uppercase tracking-widest mb-2 font-bold">Управление блоками</p>
                      <p className="text-sm text-neutral-400">
                        Для управления видимостью и порядком блоков используйте вкладку "Блоки" в верхнем меню.
                      </p>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Blocks Tab */}
            {activeTab === 'blocks' && (
              <div className="space-y-6 pb-12">
                <div className="flex justify-between items-center mb-4">
                  <h2 className="text-xl font-black uppercase text-white">Управление блоками</h2>
                  <p className="text-[10px] text-neutral-500 uppercase tracking-widest">Перетаскивайте блоки для изменения порядка</p>
                </div>
                {(!data.siteBlocks || data.siteBlocks.length === 0) ? (
                  <div className="text-center py-12 text-neutral-500">
                    <p className="text-sm">Блоки не найдены. Перезагрузите страницу.</p>
                  </div>
                ) : (
                <div className="grid gap-2">
                  {data.siteBlocks.map((block, idx) => (
                    <div 
                      key={block.id}
                      draggable
                      onDragStart={() => setDraggedBlock(idx)}
                      onDragEnd={() => {
                        if (draggedBlock !== null && dragOverBlock !== null && draggedBlock !== dragOverBlock) {
                          reorderBlocks(draggedBlock, dragOverBlock)
                        }
                        setDraggedBlock(null)
                        setDragOverBlock(null)
                      }}
                      onDragOver={(e) => {
                        e.preventDefault()
                        setDragOverBlock(idx)
                      }}
                      onDragLeave={() => setDragOverBlock(null)}
                      className={`bg-neutral-900/40 p-4 rounded-2xl border flex items-center justify-between transition-all group cursor-grab active:cursor-grabbing ${
                        block.enabled ? 'border-white/10 hover:bg-neutral-900/60' : 'border-red-500/20 bg-red-500/5 opacity-60'
                      } ${draggedBlock === idx ? 'opacity-50 scale-95' : ''} ${dragOverBlock === idx && draggedBlock !== idx ? 'border-white/40 bg-white/5' : ''}`}
                    >
                      <div className="flex items-center gap-3">
                        <div className="text-neutral-600 hover:text-neutral-400 transition-colors cursor-grab">
                          <GripVertical size={18} />
                        </div>
                        <div className={`w-8 h-8 rounded-lg flex items-center justify-center text-xs font-black ${
                          block.enabled ? 'bg-white/10 text-white' : 'bg-red-500/20 text-red-400'
                        }`}>
                          {String(idx + 1).padStart(2, '0')}
                        </div>
                        <div>
                          <p className={`font-bold text-sm ${block.enabled ? 'text-white' : 'text-red-400'}`}>{block.name}</p>
                          <p className="text-[8px] text-neutral-600 uppercase tracking-widest">ID: {block.id}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-1">
                        <button 
                          onClick={() => moveBlockUp(block.id)} 
                          disabled={idx === 0}
                          className={`p-2 rounded-lg transition-all ${
                            idx === 0 ? 'text-neutral-700 cursor-not-allowed' : 'text-neutral-500 hover:text-white hover:bg-white/10'
                          }`}
                        >
                          <ChevronUp size={16} />
                        </button>
                        <button 
                          onClick={() => moveBlockDown(block.id)} 
                          disabled={idx === (data.siteBlocks?.length || 0) - 1}
                          className={`p-2 rounded-lg transition-all ${
                            idx === (data.siteBlocks?.length || 0) - 1 ? 'text-neutral-700 cursor-not-allowed' : 'text-neutral-500 hover:text-white hover:bg-white/10'
                          }`}
                        >
                          <ChevronDown size={16} />
                        </button>
                        <button 
                          onClick={() => toggleBlock(block.id)} 
                          className={`p-2 rounded-lg transition-all ${
                            block.enabled 
                              ? 'text-green-500 hover:bg-green-500/10' 
                              : 'text-red-500 hover:bg-red-500/10'
                          }`}
                        >
                          {block.enabled ? <Eye size={16} /> : <EyeOff size={16} />}
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
                )}
                <div className="bg-neutral-900/20 p-5 rounded-2xl border border-white/5 mt-6">
                  <p className="text-[10px] text-neutral-500 uppercase tracking-widest mb-2 font-bold">Подсказка</p>
                  <p className="text-xs text-neutral-400">
                    Перетаскивайте блоки за иконку ≡ или используйте стрелки. Изменения сразу отображаются на сайте.
                  </p>
                </div>
              </div>
            )}

            {/* Services Tab */}
            {activeTab === 'services' && (
              <div className="space-y-6 pb-12">
                <div className="flex justify-between items-center mb-4">
                  <h2 className="text-xl font-black uppercase text-white">Каталог услуг</h2>
                  <button onClick={() => setEditingService({ id: Date.now().toString(), title: '', category: 'Стайлинг', description: '', price: 'от 5 000 ₽', image: '' })} className="bg-white text-black px-6 py-2 rounded-full text-[10px] font-bold uppercase tracking-widest hover:bg-neutral-200 transition-all shadow-xl">+ Добавить</button>
                </div>
                <div className="grid gap-4">
                  {data.services.map(s => (
                    <div key={s.id} className="bg-neutral-900/40 p-5 rounded-3xl border border-white/5 flex items-center justify-between hover:bg-neutral-900/60 transition-all group">
                      <div className="flex items-center gap-5">
                        <div className="w-16 h-16 bg-black rounded-2xl overflow-hidden border border-white/5">
                          <img src={s.image} alt={s.title} className="w-full h-full object-cover grayscale group-hover:grayscale-0 transition-all" />
                        </div>
                        <div>
                          <p className="font-bold text-white text-lg">{s.title}</p>
                          <p className="text-[10px] text-neutral-500 uppercase tracking-widest">{s.category} • {s.price}</p>
                        </div>
                      </div>
                      <div className="flex gap-2">
                        <button onClick={() => setEditingService(s)} className="p-3 text-neutral-500 hover:text-white hover:bg-white/5 rounded-full transition-all"><Edit3 size={18} /></button>
                        <button onClick={() => deleteService(s.id)} className="p-3 text-neutral-500 hover:text-red-500 hover:bg-red-500/5 rounded-full transition-all"><Trash2 size={18} /></button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Requests Tab */}
            {activeTab === 'requests' && (
              <div className="space-y-6 pb-12">
                <h2 className="text-xl font-black uppercase text-white">Входящие заявки</h2>
                {data.callbackRequests.length === 0 ? (
                  <p className="text-neutral-600 text-center py-24 uppercase text-xs tracking-widest italic">Список заявок пуст</p>
                ) : (
                  <div className="space-y-4">
                    {data.callbackRequests.map(r => (
                      <div key={r.id} className="bg-neutral-900/40 p-8 rounded-[40px] border border-white/10 flex justify-between items-center hover:bg-neutral-900/60 transition-all">
                        <div className="space-y-1">
                          <p className="text-xl font-bold text-white">{r.name}</p>
                          <a href={`tel:${r.phone}`} className="text-2xl text-neutral-400 font-light tracking-tighter hover:text-white transition-colors">{r.phone}</a>
                          <div className="flex gap-2 mt-4">
                            <span className="text-[9px] font-black bg-white/5 px-3 py-1.5 rounded-xl text-neutral-400 uppercase tracking-widest border border-white/5">{r.service || 'ОБЩИЙ ЗАПРОС'}</span>
                            {r.carModel && <span className="text-[9px] font-black bg-white/5 px-3 py-1.5 rounded-xl text-neutral-400 uppercase tracking-widest border border-white/5">{r.carModel}</span>}
                          </div>
                        </div>
                        <button onClick={() => deleteRequest(r.id)} className="p-6 bg-red-500/10 text-red-500 rounded-full hover:bg-red-500 hover:text-white transition-all shadow-lg active:scale-90"><Trash2 size={24} /></button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Subscriptions Tab */}
            {activeTab === 'subscriptions' && (
              <div className="space-y-6 pb-12">
                <h2 className="text-xl font-black uppercase text-white">База подписчиков</h2>
                <div className="grid gap-3">
                  {data.subscriptions.length === 0 ? (
                    <p className="text-neutral-600 text-center py-24 uppercase text-xs tracking-widest italic">Подписчиков нет</p>
                  ) : (
                    data.subscriptions.map(s => (
                      <div key={s.id} className="bg-neutral-900/40 p-5 rounded-2xl border border-white/5 flex justify-between items-center hover:bg-neutral-900/60 transition-all">
                        <span className="font-mono text-sm text-neutral-200">{s.email}</span>
                        <button onClick={() => deleteSubscription(s.id)} className="p-2 text-neutral-600 hover:text-red-500 rounded-full hover:bg-red-500/5 transition-all"><Trash2 size={16} /></button>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            {/* Users Tab */}
            {activeTab === 'users' && (
              <div className="space-y-6 pb-12">
                <div className="flex justify-between items-center mb-4">
                  <h2 className="text-xl font-black uppercase text-white">Команда проекта</h2>
                  <button onClick={() => setIsAddingUser(true)} className="bg-white text-black px-6 py-2 rounded-full text-[10px] font-bold uppercase tracking-widest hover:bg-neutral-200 shadow-xl transition-all">+ Новый сотрудник</button>
                </div>
                <div className="grid gap-4">
                  {data.users.map(u => (
                    <div key={u.id} className="bg-neutral-900/40 p-6 rounded-3xl border border-white/5 flex justify-between items-center hover:bg-neutral-900/60 transition-all group">
                      <div className="flex items-center gap-5">
                        <div className={`w-12 h-12 rounded-2xl flex items-center justify-center ${u.role === 'ADMIN' ? 'bg-white text-black' : 'bg-neutral-800 text-neutral-500'}`}>
                          {u.role === 'ADMIN' ? <ShieldCheck size={20} /> : <User size={20} />}
                        </div>
                        <div>
                          <p className="text-white font-bold text-lg">{u.name}</p>
                          <div className="flex gap-4 mt-1">
                            <p className="text-[9px] text-neutral-500 uppercase tracking-widest">Роль: <span className="text-white">{u.role}</span></p>
                            <p className="text-[9px] text-neutral-500 uppercase tracking-widest">ID: <span className="text-white">{u.login}</span></p>
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {u.login !== 'admin' && (
                          <button onClick={() => deleteUser(u.id)} className="p-3 text-neutral-600 hover:text-red-500 hover:bg-red-500/5 rounded-full transition-all"><Trash2 size={18} /></button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* History Tab */}
            {activeTab === 'history' && (() => {
              const totalItems = data.activityLog.length
              const totalPages = Math.ceil(totalItems / HISTORY_PER_PAGE)
              const startIdx = (historyPage - 1) * HISTORY_PER_PAGE
              const endIdx = startIdx + HISTORY_PER_PAGE
              const currentItems = data.activityLog.slice(startIdx, endIdx)
              
              return (
              <div className="space-y-6 pb-12">
                <div className="flex justify-between items-center">
                  <h2 className="text-xl font-black uppercase text-white">Журнал ответственности</h2>
                  <span className="text-[10px] text-neutral-500 uppercase tracking-widest">
                    Всего записей: {totalItems}
                  </span>
                </div>
                
                {totalItems === 0 ? (
                  <p className="text-neutral-600 text-center py-24 uppercase text-xs tracking-widest italic">Журнал пуст</p>
                ) : (
                  <>
                    <div className="bg-neutral-900/40 rounded-3xl border border-white/5 overflow-hidden shadow-2xl">
                      <table className="w-full text-left text-[10px] uppercase tracking-widest">
                        <thead className="bg-black/60 text-neutral-500">
                          <tr>
                            <th className="p-6 font-black">Сотрудник</th>
                            <th className="p-6 font-black">Действие</th>
                            <th className="p-6 text-right font-black">Дата</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-white/5">
                          {currentItems.map(l => (
                            <tr key={l.id} className="text-neutral-400 hover:bg-white/5 transition-colors group">
                              <td className="p-6 font-bold text-white">{l.user}</td>
                              <td className="p-6">{l.action}</td>
                              <td className="p-6 text-right font-mono text-[9px] opacity-60">{new Date(l.timestamp).toLocaleString()}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    
                    {/* Pagination */}
                    {totalPages > 1 && (
                      <div className="flex items-center justify-between pt-4">
                        <div className="text-[10px] text-neutral-500 uppercase tracking-widest">
                          Показано {startIdx + 1}–{Math.min(endIdx, totalItems)} из {totalItems}
                        </div>
                        <div className="flex items-center gap-1">
                          <button
                            onClick={() => setHistoryPage(1)}
                            disabled={historyPage === 1}
                            className={`p-2 rounded-lg transition-all ${historyPage === 1 ? 'text-neutral-700 cursor-not-allowed' : 'text-neutral-500 hover:text-white hover:bg-white/10'}`}
                          >
                            <ChevronsLeft size={16} />
                          </button>
                          <button
                            onClick={() => setHistoryPage(p => Math.max(1, p - 1))}
                            disabled={historyPage === 1}
                            className={`p-2 rounded-lg transition-all ${historyPage === 1 ? 'text-neutral-700 cursor-not-allowed' : 'text-neutral-500 hover:text-white hover:bg-white/10'}`}
                          >
                            <ChevronLeft size={16} />
                          </button>
                          
                          <div className="flex items-center gap-1 px-2">
                            {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                              let pageNum: number
                              if (totalPages <= 5) {
                                pageNum = i + 1
                              } else if (historyPage <= 3) {
                                pageNum = i + 1
                              } else if (historyPage >= totalPages - 2) {
                                pageNum = totalPages - 4 + i
                              } else {
                                pageNum = historyPage - 2 + i
                              }
                              return (
                                <button
                                  key={pageNum}
                                  onClick={() => setHistoryPage(pageNum)}
                                  className={`w-8 h-8 rounded-lg text-[10px] font-bold transition-all ${
                                    historyPage === pageNum 
                                      ? 'bg-white text-black' 
                                      : 'text-neutral-500 hover:text-white hover:bg-white/10'
                                  }`}
                                >
                                  {pageNum}
                                </button>
                              )
                            })}
                          </div>
                          
                          <button
                            onClick={() => setHistoryPage(p => Math.min(totalPages, p + 1))}
                            disabled={historyPage === totalPages}
                            className={`p-2 rounded-lg transition-all ${historyPage === totalPages ? 'text-neutral-700 cursor-not-allowed' : 'text-neutral-500 hover:text-white hover:bg-white/10'}`}
                          >
                            <ChevronRight size={16} />
                          </button>
                          <button
                            onClick={() => setHistoryPage(totalPages)}
                            disabled={historyPage === totalPages}
                            className={`p-2 rounded-lg transition-all ${historyPage === totalPages ? 'text-neutral-700 cursor-not-allowed' : 'text-neutral-500 hover:text-white hover:bg-white/10'}`}
                          >
                            <ChevronsRight size={16} />
                          </button>
                        </div>
                      </div>
                    )}
                  </>
                )}
              </div>
              )
            })()}

            {/* Notifications Tab */}
            {activeTab === 'notifications' && (
              <div className="space-y-6 pb-12">
                <div className="flex justify-between items-center">
                  <h2 className="text-xl font-black uppercase text-white">Настройки уведомлений</h2>
                </div>
                <NotificationSettings />
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Service Edit Modal */}
      <AnimatePresence>
        {editingService && (
          <div className="fixed inset-0 z-[500] bg-black/90 backdrop-blur-2xl flex items-center justify-center p-4">
            <motion.div initial={{ scale: 0.9, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.9, y: 20 }} className="bg-neutral-900 w-full max-w-xl rounded-[40px] border border-white/10 overflow-hidden shadow-2xl">
              <div className="p-6 bg-black flex justify-between items-center border-b border-white/5">
                <h3 className="font-black uppercase text-white text-sm tracking-widest">Управление услугой</h3>
                <button onClick={() => setEditingService(null)} className="p-2 hover:bg-white/10 rounded-full transition-all"><X size={20} /></button>
              </div>
              <div className="p-10 space-y-5">
                <div className="grid md:grid-cols-2 gap-5">
                  <div>
                    <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Название</label>
                    <input type="text" value={editingService.title} onChange={(e) => setEditingService({ ...editingService, title: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                  </div>
                  <div>
                    <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Категория</label>
                    <input type="text" value={editingService.category} onChange={(e) => setEditingService({ ...editingService, category: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                  </div>
                </div>
                <div>
                  <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Описание</label>
                  <textarea value={editingService.description} onChange={(e) => setEditingService({ ...editingService, description: e.target.value })} rows={3} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                </div>
                <div className="grid md:grid-cols-2 gap-5">
                  <div>
                    <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Цена</label>
                    <input type="text" value={editingService.price} onChange={(e) => setEditingService({ ...editingService, price: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                  </div>
                  <div>
                    <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">URL изображения</label>
                    <input type="text" value={editingService.image} onChange={(e) => setEditingService({ ...editingService, image: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                  </div>
                </div>
                <button
                  onClick={() => {
                    if (data.services.find(s => s.id === editingService.id)) {
                      updateService(editingService.id, editingService)
                    } else {
                      addService(editingService)
                    }
                    setEditingService(null)
                  }}
                  className="w-full bg-white text-black py-5 rounded-2xl font-black uppercase tracking-[0.3em] text-[10px] hover:bg-neutral-200 transition-all mt-4"
                >
                  Сохранить
                </button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>

      {/* Add User Modal */}
      <AnimatePresence>
        {isAddingUser && (
          <div className="fixed inset-0 z-[500] bg-black/90 backdrop-blur-2xl flex items-center justify-center p-4">
            <motion.div initial={{ scale: 0.9, y: 20 }} animate={{ scale: 1, y: 0 }} exit={{ scale: 0.9, y: 20 }} className="bg-neutral-900 w-full max-w-md rounded-[40px] border border-white/10 overflow-hidden shadow-2xl">
              <div className="p-6 bg-black flex justify-between items-center border-b border-white/5">
                <h3 className="font-black uppercase text-white text-sm tracking-widest">Новый сотрудник</h3>
                <button onClick={() => setIsAddingUser(false)} className="p-2 hover:bg-white/10 rounded-full transition-all"><X size={20} /></button>
              </div>
              <div className="p-10 space-y-5">
                <div>
                  <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">ФИО</label>
                  <input type="text" value={newUser.name} onChange={(e) => setNewUser({ ...newUser, name: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                </div>
                <div className="grid grid-cols-2 gap-5">
                  <div>
                    <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Логин</label>
                    <input type="text" value={newUser.login} onChange={(e) => setNewUser({ ...newUser, login: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                  </div>
                  <div>
                    <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Пароль</label>
                    <input type="text" value={newUser.pass} onChange={(e) => setNewUser({ ...newUser, pass: e.target.value })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30" />
                  </div>
                </div>
                <div>
                  <label className="text-[9px] uppercase tracking-widest text-neutral-500 mb-2 block font-bold">Роль</label>
                  <select value={newUser.role} onChange={(e) => setNewUser({ ...newUser, role: e.target.value as UserRole })} className="w-full bg-black border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30">
                    <option value="EDITOR">Editor</option>
                    <option value="MANAGER">Manager</option>
                    <option value="ADMIN">Admin</option>
                  </select>
                </div>
                <button
                  onClick={async () => {
                    if (newUser.name && newUser.login && newUser.pass) {
                      const success = await addUser({ login: newUser.login, password: newUser.pass, name: newUser.name, role: newUser.role })
                      if (success) {
                        setNewUser({ name: '', login: '', pass: '', role: 'EDITOR' })
                        setIsAddingUser(false)
                      }
                    }
                  }}
                  className="w-full bg-white text-black py-5 rounded-2xl font-black uppercase tracking-[0.3em] text-[10px] hover:bg-neutral-200 transition-all mt-4"
                >
                  Добавить
                </button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>

      {/* Full Section Preview Modal */}
      <AnimatePresence>
        {showFullPreview && (
          <SectionPreview 
            sectionId={contentSubTab} 
            onClose={() => setShowFullPreview(false)} 
          />
        )}
      </AnimatePresence>
    </div>
  )
}

export default AdminPanel
