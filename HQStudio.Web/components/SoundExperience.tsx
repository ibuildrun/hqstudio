'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Volume2, VolumeX } from 'lucide-react'
import { useAdmin } from '@/lib/store'

export default function SoundExperience() {
  const [isQuiet, setIsQuiet] = useState(false)
  const { data } = useAdmin()

  return (
    <section className="py-24 bg-white text-black overflow-hidden">
      <div className="container mx-auto px-4">
        <div className="flex flex-col items-center text-center mb-16">
          <span className="text-xs font-bold uppercase tracking-[0.4em] text-neutral-400 mb-4">Акустический комфорт</span>
          <h2 className="text-4xl md:text-6xl font-black uppercase tracking-tighter">Почувствуйте разницу</h2>
        </div>

        <div className="relative max-w-5xl mx-auto h-[400px] bg-black rounded-3xl overflow-hidden shadow-2xl flex flex-col md:flex-row">
          <div className="absolute inset-0 opacity-20 flex items-center justify-around px-12">
            {[...Array(20)].map((_, i) => (
              <motion.div
                key={i}
                animate={{ 
                  height: isQuiet ? [10, 20, 10] : [20, 150, 40, 180, 20] 
                }}
                transition={{ 
                  repeat: Infinity, 
                  duration: isQuiet ? 2 : 0.5, 
                  delay: i * 0.05 
                }}
                className="w-1 bg-white rounded-full"
              />
            ))}
          </div>

          <div className="relative z-10 w-full flex flex-col items-center justify-center p-12">
            <div className="bg-neutral-900/50 backdrop-blur-xl p-2 rounded-full border border-white/10 flex gap-2 mb-8">
              <button 
                onClick={() => setIsQuiet(false)}
                className={`px-6 py-3 rounded-full flex items-center gap-2 transition-all ${!isQuiet ? 'bg-white text-black' : 'text-white'}`}
              >
                <Volume2 size={18} />
                <span className="text-xs font-bold uppercase tracking-wider">Обычный авто</span>
              </button>
              <button 
                onClick={() => setIsQuiet(true)}
                className={`px-6 py-3 rounded-full flex items-center gap-2 transition-all ${isQuiet ? 'bg-white text-black' : 'text-white'}`}
              >
                <VolumeX size={18} />
                <span className="text-xs font-bold uppercase tracking-wider">Студия HQ</span>
              </button>
            </div>

            <div className="text-center">
              <AnimatePresence mode="wait">
                <motion.div
                  key={isQuiet ? 'quiet' : 'noisy'}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -10 }}
                >
                  <h3 className="text-white text-2xl md:text-4xl font-light mb-4 italic">
                    {isQuiet ? data.soundExpQuietTitle : data.soundExpNoisyTitle}
                  </h3>
                  <p className="text-neutral-500 max-w-md mx-auto text-sm uppercase tracking-widest leading-relaxed">
                    {isQuiet ? data.soundExpQuietDesc : data.soundExpNoisyDesc}
                  </p>
                </motion.div>
              </AnimatePresence>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
