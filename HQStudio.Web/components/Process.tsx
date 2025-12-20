'use client'

import { motion } from 'framer-motion'
import { useAdmin } from '@/lib/store'

export default function Process() {
  const { data } = useAdmin()
  
  return (
    <section className="py-24 bg-white text-black">
      <div className="container mx-auto px-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12">
          {data.processSteps.map((step, i) => (
            <motion.div 
              key={i}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.2 }}
              viewport={{ once: true }}
              className="relative group"
            >
              <span className="text-7xl font-black text-neutral-100 group-hover:text-neutral-200 transition-colors absolute -top-10 -left-4 z-0 select-none">
                {step.num}
              </span>
              <div className="relative z-10 pt-4">
                <h3 className="text-xl font-bold uppercase tracking-widest mb-4 border-b border-black w-fit pb-1">
                  {step.title}
                </h3>
                <p className="text-neutral-500 text-sm leading-relaxed">
                  {step.text}
                </p>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  )
}
