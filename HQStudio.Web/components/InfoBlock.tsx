'use client'

import { motion } from 'framer-motion'
import { useAdmin } from '@/lib/store'

export default function InfoBlock() {
  const { data } = useAdmin()
  
  const defaultDisclaimer = `${data.ownerName}. Информация на сайте не является публичной офертой. ${new Date().getFullYear()} г.`
  
  return (
    <div className="w-full py-12 bg-white text-black overflow-hidden border-y border-neutral-200">
      <motion.div 
        className="flex whitespace-nowrap"
        animate={{ x: [0, -1500] }}
        transition={{ repeat: Infinity, duration: 30, ease: "linear" }}
      >
        {[...Array(8)].map((_, i) => (
          <div key={i} className="flex items-center">
            <span className="text-6xl md:text-8xl font-black uppercase tracking-tighter px-8">
              {data.aboutTickerText} •
            </span>
          </div>
        ))}
      </motion.div>
      <div className="container mx-auto px-4 mt-8 flex justify-center">
        <p className="text-xs md:text-sm font-mono text-center max-w-2xl opacity-70 uppercase tracking-widest">
          {data.aboutTickerDisclaimer || defaultDisclaimer}
        </p>
      </div>
    </div>
  )
}
