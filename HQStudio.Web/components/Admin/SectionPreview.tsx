'use client'

import { motion } from 'framer-motion'
import { X, Monitor, Smartphone, Tablet, RotateCcw } from 'lucide-react'
import { useState, useEffect, useCallback } from 'react'

interface SectionPreviewProps {
  sectionId: string
  onClose: () => void
}

type ViewportSize = 'desktop' | 'tablet' | 'mobile'

const viewportConfig = {
  desktop: { width: 1440, height: 900, label: 'Desktop' },
  tablet: { width: 768, height: 1024, label: 'iPad' },
  mobile: { width: 375, height: 812, label: 'iPhone' }
}

const sectionNames: Record<string, string> = {
  hero: 'Главный экран',
  services: 'Услуги',
  ticker: 'Бегущая строка',
  manifest: 'Манифест',
  quality: 'Качество материалов',
  sound: 'Звуковой опыт',
  process: 'Процесс работы',
  gallery: 'Галерея студии',
  moodlight: 'Ambient Light',
  configurator: 'Конфигуратор',
  showcase: 'Портфолио',
  lookbook: 'Портфолио',
  testimonials: 'Отзывы клиентов',
  game: 'Промо-игра',
  faq: 'Вопросы и ответы',
  newsletter: 'Подписка',
  contact: 'Контакты',
}

// Map section IDs to anchor IDs on the main page (using section-{id} format)
const getSectionAnchor = (sectionId: string): string => {
  // lookbook maps to showcase
  const id = sectionId === 'lookbook' ? 'showcase' : sectionId
  return `section-${id}`
}

export default function SectionPreview({ sectionId, onClose }: SectionPreviewProps) {
  const [viewport, setViewport] = useState<ViewportSize>('desktop')
  const [scale, setScale] = useState(0.5)
  const [key, setKey] = useState(0)
  const [iframeLoaded, setIframeLoaded] = useState(false)

  useEffect(() => {
    document.body.style.overflow = 'hidden'
    return () => { document.body.style.overflow = 'unset' }
  }, [])

  // Calculate optimal scale based on available space
  const calculateScale = useCallback(() => {
    const config = viewportConfig[viewport]
    const padding = 180
    const availableWidth = window.innerWidth - padding
    const availableHeight = window.innerHeight - padding
    
    const widthScale = availableWidth / config.width
    const heightScale = availableHeight / config.height
    
    const optimalScale = Math.min(0.8, widthScale, heightScale)
    setScale(Math.max(0.25, optimalScale))
  }, [viewport])

  useEffect(() => {
    calculateScale()
    window.addEventListener('resize', calculateScale)
    return () => window.removeEventListener('resize', calculateScale)
  }, [calculateScale])

  // Reset iframe when viewport changes
  useEffect(() => {
    setIframeLoaded(false)
    setKey(prev => prev + 1)
  }, [viewport])

  const config = viewportConfig[viewport]
  const anchor = getSectionAnchor(sectionId)

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-[500] bg-[#0a0a0a] flex flex-col items-center justify-center overflow-hidden"
    >
      {/* Background Pattern */}
      <div 
        className="absolute inset-0 z-0 opacity-[0.015] pointer-events-none" 
        style={{ 
          backgroundImage: `radial-gradient(circle at center, #fff 1px, transparent 1px)`,
          backgroundSize: '24px 24px' 
        }} 
      />

      {/* Command Bar */}
      <div className="absolute top-5 left-1/2 -translate-x-1/2 z-[600] flex items-center gap-2 px-2 py-1.5 bg-black/90 backdrop-blur-2xl border border-white/10 rounded-2xl shadow-2xl">
        {/* Section Name */}
        <div className="flex items-center gap-2 px-3 border-r border-white/10">
          <div className={`w-1.5 h-1.5 rounded-full ${iframeLoaded ? 'bg-emerald-500' : 'bg-amber-500 animate-pulse'}`} />
          <span className="text-[10px] font-bold uppercase tracking-widest text-white">
            {sectionNames[sectionId] || 'Preview'}
          </span>
        </div>

        {/* Viewport Switcher */}
        <div className="flex items-center gap-0.5 px-1">
          {(['desktop', 'tablet', 'mobile'] as ViewportSize[]).map((v) => (
            <button
              key={v}
              onClick={() => setViewport(v)}
              className={`p-2 rounded-lg transition-all duration-200 ${
                viewport === v 
                  ? 'bg-white text-black shadow-lg' 
                  : 'text-white/40 hover:text-white hover:bg-white/10'
              }`}
              title={viewportConfig[v].label}
            >
              {v === 'desktop' && <Monitor size={14} />}
              {v === 'tablet' && <Tablet size={14} />}
              {v === 'mobile' && <Smartphone size={14} />}
            </button>
          ))}
        </div>

        {/* Refresh */}
        <button
          onClick={() => {
            setIframeLoaded(false)
            setKey(prev => prev + 1)
          }}
          className="p-2 text-white/40 hover:text-white hover:bg-white/10 rounded-lg transition-all"
          title="Обновить"
        >
          <RotateCcw size={14} />
        </button>

        {/* Close */}
        <button
          onClick={onClose}
          className="p-2 bg-white/5 text-white/60 rounded-lg hover:bg-red-500/80 hover:text-white transition-all ml-1"
        >
          <X size={14} />
        </button>
      </div>

      {/* Preview Container */}
      <div className="relative z-10 flex items-center justify-center w-full h-full pt-16 pb-16">
        <motion.div
          key={key}
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.3 }}
          className="relative"
          style={{
            width: config.width * scale,
            height: config.height * scale,
          }}
        >
          {/* Device Frame */}
          <div className={`
            w-full h-full rounded-2xl overflow-hidden relative
            ${viewport !== 'desktop' ? 'border-[8px] border-neutral-800 shadow-[0_0_60px_rgba(0,0,0,0.5)]' : 'border border-white/10 shadow-2xl'}
          `}>
            {/* Browser Chrome for Desktop */}
            {viewport === 'desktop' && (
              <div className="bg-neutral-900 border-b border-white/5 px-3 py-1.5 flex items-center gap-2 shrink-0" style={{ height: 28 * scale }}>
                <div className="flex gap-1">
                  <div className="w-2 h-2 rounded-full bg-red-500/70" style={{ width: 8 * scale, height: 8 * scale }} />
                  <div className="w-2 h-2 rounded-full bg-yellow-500/70" style={{ width: 8 * scale, height: 8 * scale }} />
                  <div className="w-2 h-2 rounded-full bg-green-500/70" style={{ width: 8 * scale, height: 8 * scale }} />
                </div>
                <div className="flex-1 mx-2">
                  <div className="bg-black/50 rounded px-2 py-0.5 text-center font-mono truncate text-neutral-500" style={{ fontSize: 8 * scale }}>
                    hq-studio.ru/#section-{sectionId === 'lookbook' ? 'showcase' : sectionId}
                  </div>
                </div>
              </div>
            )}

            {/* Loading Indicator */}
            {!iframeLoaded && (
              <div className="absolute inset-0 bg-black flex items-center justify-center z-10">
                <div className="flex flex-col items-center gap-3">
                  <div className="w-8 h-8 border-2 border-white/20 border-t-white rounded-full animate-spin" />
                  <span className="text-[10px] text-neutral-500 uppercase tracking-widest">Загрузка...</span>
                </div>
              </div>
            )}

            {/* Iframe with actual site */}
            <iframe
              key={`iframe-${key}-${viewport}`}
              src={`/?preview=true#${anchor}`}
              className="bg-black"
              style={{
                width: config.width,
                height: viewport === 'desktop' ? config.height - 28 : config.height,
                transform: `scale(${scale})`,
                transformOrigin: 'top left',
                border: 'none',
              }}
              onLoad={() => setIframeLoaded(true)}
              title="Preview"
            />
          </div>

          {/* Mobile Notch */}
          {viewport === 'mobile' && (
            <div 
              className="absolute left-1/2 -translate-x-1/2 bg-neutral-800 rounded-b-2xl z-10 flex items-center justify-center"
              style={{ 
                top: 8 * scale,
                width: 80 * scale, 
                height: 20 * scale 
              }}
            >
              <div className="bg-neutral-700 rounded-full" style={{ width: 40 * scale, height: 4 * scale }} />
            </div>
          )}

          {/* Mobile Home Indicator */}
          {viewport === 'mobile' && (
            <div 
              className="absolute left-1/2 -translate-x-1/2 bg-white/20 rounded-full z-10"
              style={{ 
                bottom: 12 * scale,
                width: 100 * scale, 
                height: 4 * scale 
              }}
            />
          )}
        </motion.div>
      </div>

      {/* Viewport Info */}
      <div className="absolute bottom-5 left-1/2 -translate-x-1/2 z-[600]">
        <div className="flex items-center gap-3 px-4 py-2 bg-black/70 backdrop-blur-xl border border-white/5 rounded-full">
          <span className="text-[9px] font-mono text-white/40 uppercase tracking-wider">
            {config.label}
          </span>
          <span className="text-[9px] font-mono text-white/25">
            {config.width} × {config.height}
          </span>
          <span className="text-[9px] font-mono text-emerald-500/70">
            {Math.round(scale * 100)}%
          </span>
        </div>
      </div>
    </motion.div>
  )
}
