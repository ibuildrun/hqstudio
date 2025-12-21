'use client'

import { useState } from 'react'
import { motion } from 'framer-motion'
import { Sparkles, Moon, Zap } from 'lucide-react'
import { useAdmin } from '@/lib/store'

const DEFAULT_MODES = [
  { id: 'zen', name: 'Zen Mode', color: 'rgba(255, 140, 0, 0.4)', desc: 'Теплый уют для вечерних поездок' },
  { id: 'arctic', name: 'Arctic Blue', color: 'rgba(0, 191, 255, 0.4)', desc: 'Свежесть и концентрация за рулем' },
  { id: 'royal', name: 'Royal Violet', color: 'rgba(138, 43, 226, 0.4)', desc: 'Элегантность и статус вашего интерьера' }
]

const getIcon = (id: string) => {
  switch (id) {
    case 'zen': return <Moon size={16} />
    case 'arctic': return <Zap size={16} />
    default: return <Sparkles size={16} />
  }
}

export default function MoodLightExperience() {
  const { data } = useAdmin()
  const modes = data.moodlightModes || DEFAULT_MODES
  const [activeMode, setActiveMode] = useState(modes[0])

  return (
    <section className="py-24 bg-black text-white overflow-hidden">
      <div className="container mx-auto px-4">
        <div className="flex flex-col md:flex-row items-center gap-16">
          <div className="w-full md:w-1/2">
            <span className="text-xs font-bold uppercase tracking-[0.5em] text-neutral-500 mb-4 block">{data.moodlightSubtitle || 'Освещение как искусство'}</span>
            <h2 className="text-4xl md:text-6xl font-black uppercase tracking-tighter mb-8">
              {(data.moodlightTitle || 'Управляйте атмосферой').split(' ').map((word, i) => (
                <span key={i}>{word}{i === 0 && <br/>} </span>
              ))}
            </h2>
            
            <div className="space-y-4">
              {modes.map((mode: any) => (
                <button
                  key={mode.id}
                  onClick={() => setActiveMode(mode)}
                  className={`w-full p-6 text-left border transition-all duration-500 flex items-center justify-between group ${
                    activeMode.id === mode.id ? 'bg-white text-black border-white' : 'border-white/10 hover:border-white/30'
                  }`}
                >
                  <div className="flex items-center gap-4">
                    <div className={`p-2 rounded-full ${activeMode.id === mode.id ? 'bg-black text-white' : 'bg-white/5'}`}>
                      {getIcon(mode.id)}
                    </div>
                    <div>
                      <h4 className="font-bold uppercase tracking-widest text-sm">{mode.name}</h4>
                      <p className={`text-xs mt-1 ${activeMode.id === mode.id ? 'text-neutral-600' : 'text-neutral-500'}`}>
                        {mode.desc}
                      </p>
                    </div>
                  </div>
                </button>
              ))}
            </div>
          </div>

          <div className="w-full md:w-1/2 relative aspect-square md:aspect-auto md:h-[600px] bg-neutral-900 rounded-2xl overflow-hidden shadow-2xl group">
            <img 
              src={data.moodlightImage || "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&q=80&w=1200"} 
              alt="Interior" 
              className="w-full h-full object-cover opacity-50 transition-transform duration-1000 group-hover:scale-105"
            />
            <motion.div 
              animate={{ backgroundColor: activeMode.color }}
              transition={{ duration: 1 }}
              className="absolute inset-0 mix-blend-screen pointer-events-none"
              style={{ boxShadow: `inset 0 0 150px ${activeMode.color}` }}
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black via-transparent to-transparent" />
            
            <div className="absolute bottom-8 left-8 right-8">
               <div className="flex justify-between items-end">
                  <div className="bg-black/40 backdrop-blur-md p-4 border border-white/10 rounded-lg">
                    <span className="text-[10px] uppercase tracking-[0.3em] text-neutral-400 block mb-1">Режим активен</span>
                    <span className="text-xl font-bold uppercase tracking-widest">{activeMode.name}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-4xl font-light italic">64+</span>
                    <span className="block text-[8px] uppercase tracking-widest text-neutral-400">Цветовых оттенков</span>
                  </div>
               </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
