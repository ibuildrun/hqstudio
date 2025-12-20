'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Gift, RefreshCcw, CheckCircle2, XCircle, Sparkle } from 'lucide-react'
import { useAdmin } from '@/lib/store'

export default function PromoGame() {
  const { data } = useAdmin()
  const [gameState, setGameState] = useState<'idle' | 'playing' | 'won' | 'lost'>('idle')
  const [promoCode, setPromoCode] = useState('')

  const handlePlay = async () => {
    setGameState('playing')
    
    setTimeout(async () => {
      const isWin = Math.random() > 0.4 
      
      if (isWin) {
        const code = Math.floor(10000 + Math.random() * 90000).toString()
        setPromoCode(code)
        setGameState('won')
      } else {
        setGameState('lost')
      }
    }, 2500)
  }

  return (
    <section className="py-24 bg-black border-y border-white/10 overflow-hidden relative">
      <div className="container mx-auto px-4 max-w-4xl relative z-10">
        <div className="text-center mb-16">
          <motion.div
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            className="inline-flex items-center gap-2 mb-4"
          >
            <Sparkle size={14} className="text-neutral-500" />
            <span className="text-[10px] uppercase tracking-[0.4em] text-neutral-500 font-bold">{data.gameSubtitle || 'Privilege Program'}</span>
            <Sparkle size={14} className="text-neutral-500" />
          </motion.div>
          <h2 className="text-4xl md:text-6xl font-black uppercase tracking-tighter mb-4">
            {(data.gameTitle || 'Aesthetic Rewards').split(' ')[0]} <br/>
            <span className="italic font-light lowercase opacity-60">{(data.gameTitle || 'Aesthetic Rewards').split(' ').slice(1).join(' ')}</span>
          </h2>
          <p className="text-neutral-500 uppercase tracking-[0.2em] text-[10px] max-w-sm mx-auto leading-relaxed">
            {data.gameDescription || 'Персональные привилегии для тех, кто ценит безупречный стиль и премиальный комфорт.'}
          </p>
        </div>

        <div className="relative bg-neutral-950 border border-white/5 p-12 md:p-20 text-center shadow-[0_20px_80px_rgba(0,0,0,0.5)] overflow-hidden rounded-2xl">
          <div className="absolute top-0 right-0 w-80 h-80 bg-rose-200/5 rounded-full -mr-40 -mt-40 blur-[120px] pointer-events-none" />
          <div className="absolute bottom-0 left-0 w-60 h-60 bg-orange-100/3 rounded-full -ml-30 -mb-30 blur-[100px] pointer-events-none" />
          
          <AnimatePresence mode="wait">
            {gameState === 'idle' && (
              <motion.div
                key="idle"
                initial={{ opacity: 0, scale: 0.98 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 1.02 }}
                className="flex flex-col items-center"
              >
                <div className="w-24 h-24 border border-white/10 rounded-full flex items-center justify-center mb-10 relative">
                  <motion.div 
                    animate={{ scale: [1, 1.2, 1], opacity: [0.2, 0.5, 0.2] }}
                    transition={{ repeat: Infinity, duration: 4 }}
                    className="absolute inset-0 rounded-full border border-rose-300/20"
                  />
                  <Gift className="text-white font-thin" size={32} strokeWidth={1} />
                </div>
                <h3 className="text-2xl md:text-3xl font-light mb-10 max-w-md leading-tight text-neutral-200 tracking-tight">
                  Получите вашу персональную привилегию на услуги HQ_Studio
                </h3>
                <button
                  onClick={handlePlay}
                  className="group relative px-14 py-6 bg-white text-black font-bold uppercase tracking-[0.3em] text-[10px] overflow-hidden transition-all hover:pr-16 rounded-full shadow-[0_10px_30px_rgba(255,255,255,0.1)] hover:shadow-[0_15px_40px_rgba(255,255,255,0.2)]"
                >
                  <span className="relative z-10">{data.gameButtonText || 'Испытать удачу'}</span>
                  <motion.div className="absolute right-5 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <Sparkle size={12} />
                  </motion.div>
                </button>
              </motion.div>
            )}

            {gameState === 'playing' && (
              <motion.div
                key="playing"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                className="py-16"
              >
                <div className="flex justify-center gap-3">
                  {[...Array(5)].map((_, i) => (
                    <motion.div
                      key={i}
                      animate={{ 
                        opacity: [0.3, 1, 0.3],
                        y: [0, -10, 0],
                        borderColor: ['#262626', '#fbcfe8', '#262626']
                      }}
                      transition={{ 
                        repeat: Infinity, 
                        duration: 1.2, 
                        delay: i * 0.15 
                      }}
                      className="w-10 h-16 md:w-16 md:h-24 border border-white/10 flex items-center justify-center text-2xl md:text-5xl font-black text-neutral-500 rounded-xl"
                    >
                      ?
                    </motion.div>
                  ))}
                </div>
                <p className="mt-12 text-neutral-500 uppercase text-[8px] tracking-[0.7em] animate-pulse">Анализируем доступные слоты привилегий...</p>
              </motion.div>
            )}

            {gameState === 'won' && (
              <motion.div
                key="won"
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                className="flex flex-col items-center"
              >
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{ type: "spring", stiffness: 200, damping: 15 }}
                >
                  <CheckCircle2 className="text-rose-200 mb-8" size={72} strokeWidth={1} />
                </motion.div>
                <h3 className="text-3xl md:text-5xl font-black uppercase mb-4 tracking-tighter">Победа!</h3>
                <p className="text-neutral-400 mb-12 italic text-sm font-light tracking-wide">Ваш персональный 5-значный промокод:</p>
                
                <motion.div 
                  initial={{ letterSpacing: '0.1em' }}
                  animate={{ letterSpacing: '0.5em' }}
                  className="text-6xl md:text-9xl font-black mb-12 text-white relative px-4"
                >
                  {promoCode}
                  <motion.div 
                    animate={{ opacity: [0, 1, 0], scale: [0.5, 1.8, 0.5] }}
                    transition={{ repeat: Infinity, duration: 3 }}
                    className="absolute -top-12 -right-12 text-rose-200/50"
                  >
                    <Sparkle size={40} />
                  </motion.div>
                </motion.div>
                
                <p className="text-[9px] text-neutral-500 uppercase tracking-[0.6em] max-w-xs leading-loose border-t border-white/10 pt-10">
                  Сохраните этот код. Предъявите его нашему менеджеру для активации бонуса.
                </p>
              </motion.div>
            )}

            {gameState === 'lost' && (
              <motion.div
                key="lost"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                className="flex flex-col items-center"
              >
                <XCircle className="text-neutral-800 mb-8" size={64} strokeWidth={1} />
                <h3 className="text-2xl font-light mb-6 uppercase tracking-[0.4em] text-neutral-300">Почти получилось</h3>
                <p className="text-neutral-500 mb-12 max-w-sm italic font-light leading-relaxed text-sm">
                  В этот раз удача была совсем близко. Мы сохранили ваше место в очереди на следующую раздачу.
                </p>
                <button
                  onClick={() => setGameState('idle')}
                  className="group flex items-center gap-4 text-white border border-white/20 px-12 py-6 hover:bg-white hover:text-black transition-all uppercase text-[9px] font-bold tracking-[0.5em] rounded-full"
                >
                  <RefreshCcw size={16} className="group-hover:rotate-180 transition-transform duration-1000" />
                  Попробовать еще раз
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </section>
  )
}
