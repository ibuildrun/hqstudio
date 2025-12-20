'use client'

import { useState, useRef } from 'react'
import { AdminProvider, useAdmin } from '@/lib/store'
import { EyeOff } from 'lucide-react'
import { APP_NAME } from '@/lib/constants'

// Components
import Navigation from '@/components/Navigation'
import Hero from '@/components/Hero'
import Services from '@/components/Services'
import InfoBlock from '@/components/InfoBlock'
import Contact from '@/components/Contact'
import FAQ from '@/components/FAQ'
import Process from '@/components/Process'
import Preloader from '@/components/Preloader'
import SoundExperience from '@/components/SoundExperience'
import MaterialQuality from '@/components/MaterialQuality'
import StudioGallery from '@/components/StudioGallery'
import Testimonials from '@/components/Testimonials'
import Configurator from '@/components/Configurator'
import MoodLightExperience from '@/components/MoodLightExperience'
import HorizontalShowcase from '@/components/HorizontalShowcase'
import PromoGame from '@/components/PromoGame'
import Newsletter from '@/components/Newsletter'
import AdminPanel from '@/components/Admin/AdminPanel'

// Manifest block component
const ManifestBlock = () => {
  const { data } = useAdmin()
  return (
    <div className="bg-white text-black py-24 px-4 text-center">
      <span className="text-xs font-bold uppercase tracking-[0.3em] text-neutral-400 mb-4 block">
        {data.aboutManifestSubtitle || 'Искусство детализации'}
      </span>
      <h3 className="text-4xl md:text-6xl font-black uppercase tracking-tight mb-8">
        {data.aboutManifestTitle || 'Ваш комфорт — наша работа'}
      </h3>
      <p className="max-w-2xl mx-auto text-lg font-light text-neutral-600 leading-relaxed italic">
        {data.aboutManifestText}
      </p>
    </div>
  )
}

// Block wrapper with visibility indicator
const BlockWrapper: React.FC<{ blockId: string; children: React.ReactNode }> = ({ blockId, children }) => {
  const { data, isAuthenticated } = useAdmin()
  const block = data.siteBlocks?.find(b => b.id === blockId)
  const isEnabled = block?.enabled !== false

  if (!isEnabled && !isAuthenticated) return null

  // Для services и showcase не оборачиваем в div чтобы не ломать sticky scroll
  if (blockId === 'services' || blockId === 'showcase') {
    if (!isEnabled && isAuthenticated) {
      return (
        <div id={`section-${blockId}`} className="relative opacity-40 grayscale border-2 border-dashed border-red-500/30 m-4 rounded-3xl overflow-hidden">
          <div className="absolute top-4 left-4 z-50 bg-red-500 text-white px-3 py-1 rounded-full text-[10px] font-bold uppercase tracking-widest flex items-center gap-2 shadow-xl">
            <EyeOff size={12} /> Скрыто
          </div>
          {children}
        </div>
      )
    }
    return <div id={`section-${blockId}`}>{children}</div>
  }

  return (
    <div id={`section-${blockId}`} className={`relative ${!isEnabled ? 'opacity-40 grayscale border-2 border-dashed border-red-500/30 m-4 rounded-3xl overflow-hidden' : ''}`}>
      {!isEnabled && isAuthenticated && (
        <div className="absolute top-4 left-4 z-50 bg-red-500 text-white px-3 py-1 rounded-full text-[10px] font-bold uppercase tracking-widest flex items-center gap-2 shadow-xl">
          <EyeOff size={12} /> Скрыто
        </div>
      )}
      {children}
    </div>
  )
}

// Map block IDs to components
const blockComponents: Record<string, React.FC> = {
  hero: Hero,
  services: Services,
  ticker: InfoBlock,
  manifest: ManifestBlock,
  quality: MaterialQuality,
  sound: SoundExperience,
  process: Process,
  gallery: StudioGallery,
  moodlight: MoodLightExperience,
  configurator: Configurator,
  showcase: HorizontalShowcase,
  testimonials: Testimonials,
  game: PromoGame,
  faq: FAQ,
  newsletter: Newsletter,
  contact: () => null, // Contact handled separately
}

const AppContent: React.FC = () => {
  const [isAdminOpen, setIsAdminOpen] = useState(false)
  const { data } = useAdmin()
  const clickCount = useRef(0)
  const lastClickTime = useRef(0)

  const handleSecretTrigger = () => {
    const now = Date.now()
    if (now - lastClickTime.current < 500) clickCount.current += 1
    else clickCount.current = 1
    lastClickTime.current = now
    if (clickCount.current >= 5) {
      setIsAdminOpen(true)
      clickCount.current = 0
    }
  }

  // Get ordered blocks (excluding hero and contact which are fixed)
  const orderedBlocks = data.siteBlocks?.filter(b => b.id !== 'hero' && b.id !== 'contact') || []

  return (
    <main className="w-full min-h-screen bg-black text-white selection:bg-white selection:text-black">
      <Preloader />
      <Navigation />
      
      {/* Hero is always first */}
      <div id="section-hero">
        <Hero />
      </div>
      
      {/* Dynamic blocks in order */}
      <div id="about" className="relative z-20">
        {orderedBlocks.map(block => {
          const Component = blockComponents[block.id]
          if (!Component) return null
          
          return (
            <BlockWrapper key={block.id} blockId={block.id}>
              <Component />
            </BlockWrapper>
          )
        })}
      </div>
      
      {/* Contact and footer are always last */}
      <footer>
        <div id="section-contact">
          <Contact onLogoClick={() => setIsAdminOpen(true)} />
        </div>
        <div className="bg-black py-8 border-t border-white/5 flex flex-col md:flex-row justify-between items-center px-8 text-[10px] text-neutral-600 uppercase tracking-widest">
          <div onClick={handleSecretTrigger} className="cursor-default select-none py-2">
            © {new Date().getFullYear()} {APP_NAME}. {data.ownerName}
          </div>
        </div>
      </footer>
      
      {isAdminOpen && <AdminPanel onClose={() => setIsAdminOpen(false)} />}
    </main>
  )
}

export default function ClientPage() {
  return (
    <AdminProvider>
      <AppContent />
    </AdminProvider>
  )
}
