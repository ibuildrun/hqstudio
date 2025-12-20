'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Plus, Minus } from 'lucide-react'
import { useAdmin } from '@/lib/store'

export default function FAQ() {
  const [openIndex, setOpenIndex] = useState<number | null>(null)
  const { data } = useAdmin()

  return (
    <section id="faq" className="py-32 bg-black text-white border-t border-white/5">
      <div className="container mx-auto px-4 md:px-8 max-w-5xl">
        <div className="flex flex-col md:flex-row gap-16 lg:gap-24">
          <div className="w-full md:w-1/3">
            <span className="text-xs font-bold uppercase tracking-[0.5em] text-neutral-600 mb-4 block">{data.faqSubtitle || 'База знаний'}</span>
            <h2 className="text-4xl md:text-5xl lg:text-6xl font-black uppercase tracking-tighter leading-tight mb-8">
              {(data.faqTitle || 'Вопросы и ответы').split(' ').map((word, i) => (
                <span key={i}>{word}{i === 0 && <br/>} </span>
              ))}
            </h2>
            <p className="text-neutral-500 text-[10px] uppercase tracking-[0.3em] leading-relaxed">
              {data.faqDescription || 'Мы за прозрачность в работе. Если у вас остались сомнения — мы развеем их здесь.'}
            </p>
          </div>

          <div className="w-full md:w-2/3 space-y-2">
            {data.faqItems.map((faq, i) => (
              <div key={i} className="border-b border-white/5 group">
                <button
                  onClick={() => setOpenIndex(openIndex === i ? null : i)}
                  className="w-full py-8 flex justify-between items-center text-left hover:text-neutral-400 transition-colors"
                >
                  <span className={`text-lg md:text-xl font-medium tracking-tight transition-all ${openIndex === i ? 'text-white pl-4' : 'text-neutral-300'}`}>
                    {faq.q}
                  </span>
                  <div className={`w-8 h-8 rounded-full border border-white/10 flex items-center justify-center transition-transform duration-500 ${openIndex === i ? 'rotate-180 bg-white text-black' : ''}`}>
                    {openIndex === i ? <Minus size={14} /> : <Plus size={14} />}
                  </div>
                </button>
                <AnimatePresence>
                  {openIndex === i && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      className="overflow-hidden"
                    >
                      <p className="pb-8 pl-4 pr-12 text-neutral-500 leading-relaxed font-light text-base md:text-lg italic">
                        {faq.a}
                      </p>
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
