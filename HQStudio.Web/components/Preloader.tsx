'use client'

import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { APP_NAME } from '@/lib/constants'

export default function Preloader() {
  const [isVisible, setIsVisible] = useState(true)

  useEffect(() => {
    const timer = setTimeout(() => setIsVisible(false), 2800)
    return () => clearTimeout(timer)
  }, [])

  return (
    <AnimatePresence>
      {isVisible && (
        <motion.div
          exit={{ 
            y: '-100%', 
            transition: { duration: 0.9, ease: [0.7, 0, 0.3, 1] } 
          }}
          className="fixed inset-0 z-[100] bg-black flex flex-col items-center justify-center text-white overflow-hidden"
        >
          <div className="relative overflow-hidden h-24 md:h-32 w-full flex items-center justify-center px-4">
            <motion.h1
              initial={{ y: 150 }}
              animate={{ y: 0 }}
              transition={{ duration: 1, ease: [0.16, 1, 0.3, 1], delay: 0.2 }}
              className="text-5xl md:text-8xl font-black uppercase tracking-[0.15em] text-center"
            >
              {APP_NAME}
            </motion.h1>
          </div>

          <div className="w-48 h-[1px] bg-white/20 mt-4 relative">
            <motion.div 
              initial={{ scaleX: 0 }}
              animate={{ scaleX: 1 }}
              transition={{ duration: 1.8, ease: "easeInOut", delay: 0.4 }}
              className="absolute inset-0 bg-white origin-left"
            />
          </div>

          <div className="overflow-hidden mt-6 h-6 flex items-center justify-center">
            <motion.p
              initial={{ y: 30, opacity: 0 }}
              animate={{ y: 0, opacity: 1 }}
              transition={{ delay: 1.2, duration: 0.8, ease: "easeOut" }}
              className="text-[10px] uppercase tracking-[0.6em] text-neutral-500 font-bold"
            >
              Evolution of Comfort
            </motion.p>
          </div>

          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 0.03 }}
            transition={{ duration: 2 }}
            className="absolute inset-0 flex items-center justify-center pointer-events-none"
          >
            <span className="text-[30vw] font-black uppercase leading-none select-none">HQ</span>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
