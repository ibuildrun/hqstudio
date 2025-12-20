'use client'

import { motion } from 'framer-motion'
import { Quote } from 'lucide-react'
import { useAdmin } from '@/lib/store'

export default function Testimonials() {
  const { data } = useAdmin()
  
  const reviews = data.testimonials || [
    { name: "Марина", car: "Audi Q7", text: "HQ_Studio превратили мою машину в настоящий оазис." },
    { name: "Александр", car: "Range Rover", text: "Делал антихром и доводчики. Качество исполнения на высоте." },
    { name: "Екатерина", car: "Porsche Macan", text: "Контурная подсветка просто преобразила интерьер!" }
  ]

  return (
    <section id="reviews" className="py-32 bg-white text-black">
      <div className="container mx-auto px-4 md:px-8">
        <div className="flex flex-col md:flex-row items-end justify-between mb-16 gap-8 border-b border-neutral-100 pb-12">
          <div className="max-w-xl">
            <span className="text-xs font-bold uppercase tracking-[0.4em] text-neutral-400 mb-4 block">{data.testimonialsSubtitle || 'Клиентский опыт'}</span>
            <h2 className="text-5xl md:text-7xl font-black uppercase tracking-tighter leading-none">{data.testimonialsTitle || 'Истории наших клиентов'}</h2>
          </div>
          <div className="flex items-center gap-4 text-neutral-400">
             <div className="text-right">
                <span className="text-4xl font-black text-black block leading-none">{data.testimonialsRating || '4.9'}</span>
                <span className="text-[10px] uppercase tracking-widest font-bold">Рейтинг ателье</span>
             </div>
             <div className="h-12 w-[1px] bg-neutral-100" />
             <div className="text-left">
                <span className="text-4xl font-black text-black block leading-none">{data.testimonialsCount || '45+'}</span>
                <span className="text-[10px] uppercase tracking-widest font-bold">Отзывов</span>
             </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-12">
          {reviews.map((rev, i) => (
            <motion.div 
              key={i}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.1 }}
              viewport={{ once: true }}
              className="relative p-8 border border-neutral-100 flex flex-col justify-between h-full group hover:bg-black hover:text-white transition-all duration-500"
            >
              <Quote className="text-neutral-200 group-hover:text-neutral-800 mb-8 transition-colors" size={40} />
              <p className="text-xl font-light leading-relaxed mb-12 italic">
                «{rev.text}»
              </p>
              <div>
                <h4 className="font-bold uppercase tracking-widest text-sm">{rev.name}</h4>
                <p className="text-neutral-400 text-[10px] uppercase tracking-tighter mt-1 group-hover:text-neutral-500">{rev.car}</p>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  )
}
