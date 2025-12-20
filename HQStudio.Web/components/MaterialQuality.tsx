'use client'

import { motion } from 'framer-motion'
import { ShieldCheck, Leaf, Wind } from 'lucide-react'
import { useAdmin } from '@/lib/store'

export default function MaterialQuality() {
  const { data } = useAdmin()
  
  return (
    <section className="py-24 bg-white text-black">
      <div className="container mx-auto px-4 md:px-8">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-24 items-center">
          <div className="order-2 md:order-1 relative">
            <motion.div 
              initial={{ opacity: 0, scale: 0.95 }}
              whileInView={{ opacity: 1, scale: 1 }}
              transition={{ duration: 0.8, ease: "easeOut" }}
              className="relative z-10 w-full aspect-[4/5] bg-neutral-200 rounded-sm overflow-hidden shadow-2xl"
            >
              <img 
                src="https://images.unsplash.com/photo-1583121274602-3e2820c69888?auto=format&fit=crop&q=80&w=1000&grayscale=true" 
                alt="Premium Materials" 
                className="w-full h-full object-cover"
              />
            </motion.div>

            <div className="absolute -bottom-8 -right-8 w-full h-full border border-black/5 bg-neutral-50 -z-0" />
            
            <motion.div 
              initial={{ x: -30, opacity: 0 }}
              whileInView={{ x: 0, opacity: 1 }}
              transition={{ delay: 0.4, duration: 0.6 }}
              className="absolute -left-6 md:-left-12 top-1/4 z-20 bg-black text-white p-6 md:p-10 shadow-[20px_20px_60px_rgba(0,0,0,0.3)] hidden lg:block"
            >
              <p className="text-3xl md:text-5xl font-black uppercase tracking-tighter leading-none">
                SAFETY<br/>
                <span className="text-neutral-400">FIRST</span>
              </p>
              <div className="w-12 h-1 bg-white mt-4" />
            </motion.div>
          </div>

          <div className="order-1 md:order-2">
            <motion.div
              initial={{ opacity: 0, x: 20 }}
              whileInView={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.6 }}
            >
              <span className="text-xs font-bold uppercase tracking-[0.4em] text-neutral-400 mb-4 block">Стандарт качества HQ</span>
              <h2 className="text-4xl md:text-6xl font-black uppercase tracking-tighter mb-8 leading-none">
                {data.aboutQualityTitle}
              </h2>
              <p className="text-neutral-500 text-lg font-light mb-12 leading-relaxed max-w-lg">
                {data.aboutQualityDesc}
              </p>

              <div className="space-y-10">
                {[
                  { icon: <Leaf size={24} />, title: "Экологичность", desc: "Материалы не выделяют формальдегидов и летучих веществ даже при экстремальном нагреве салона." },
                  { icon: <Wind size={24} />, title: "Гипоаллергенность", desc: "Безопасно для детей и домашних животных. Мы используем только клеевые составы на водной основе." },
                  { icon: <ShieldCheck size={24} />, title: "Без запаха", desc: "Ваш автомобиль сохранит аромат новой кожи. Никакого запаха резины или битума после шумоизоляции." }
                ].map((item, i) => (
                  <div key={i} className="flex gap-6 group">
                    <div className="mt-1 text-neutral-300 group-hover:text-black transition-colors duration-500">{item.icon}</div>
                    <div>
                      <h4 className="font-bold uppercase tracking-widest text-sm mb-2">{item.title}</h4>
                      <p className="text-neutral-400 text-sm leading-relaxed group-hover:text-neutral-600 transition-colors duration-500">{item.desc}</p>
                    </div>
                  </div>
                ))}
              </div>
            </motion.div>
          </div>
        </div>
      </div>
    </section>
  )
}
