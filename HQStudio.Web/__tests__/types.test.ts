import { describe, it, expect } from 'vitest'
import type { 
  ServiceItem, 
  CallbackRequest, 
  SiteBlock, 
  FAQItem, 
  Testimonial,
  MoodlightMode,
  ConfiguratorStep
} from '../lib/types'

describe('Type Validation', () => {
  describe('ServiceItem', () => {
    it('accepts valid service item', () => {
      const service: ServiceItem = {
        id: 'test-service',
        title: 'Test Service',
        description: 'Test description',
        price: 'от 10 000 ₽',
        category: 'Test',
        image: 'https://example.com/image.jpg'
      }
      
      expect(service.id).toBe('test-service')
      expect(service.title).toBe('Test Service')
    })
  })

  describe('CallbackRequest', () => {
    it('accepts valid callback request', () => {
      const request: CallbackRequest = {
        id: '123',
        name: 'Иван',
        phone: '+79291234567',
        carModel: 'BMW X5',
        licensePlate: 'А001АА86',
        service: 'Шумоизоляция',
        timestamp: Date.now(),
        status: 'new'
      }
      
      expect(request.status).toBe('new')
      expect(request.name).toBe('Иван')
    })

    it('accepts callback without optional fields', () => {
      const request: CallbackRequest = {
        id: '123',
        name: 'Иван',
        phone: '+79291234567',
        timestamp: Date.now(),
        status: 'completed'
      }
      
      expect(request.carModel).toBeUndefined()
      expect(request.licensePlate).toBeUndefined()
    })
  })

  describe('SiteBlock', () => {
    it('accepts valid site block', () => {
      const block: SiteBlock = {
        id: 'hero',
        name: 'Главный экран',
        enabled: true
      }
      
      expect(block.enabled).toBe(true)
    })
  })

  describe('FAQItem', () => {
    it('accepts valid FAQ item', () => {
      const faq: FAQItem = {
        q: 'Вопрос?',
        a: 'Ответ.'
      }
      
      expect(faq.q).toBe('Вопрос?')
      expect(faq.a).toBe('Ответ.')
    })
  })

  describe('Testimonial', () => {
    it('accepts valid testimonial', () => {
      const testimonial: Testimonial = {
        name: 'Марина',
        car: 'Audi Q7',
        text: 'Отличный сервис!'
      }
      
      expect(testimonial.name).toBe('Марина')
    })
  })

  describe('MoodlightMode', () => {
    it('accepts valid moodlight mode', () => {
      const mode: MoodlightMode = {
        id: 'zen',
        name: 'Zen Mode',
        color: 'rgba(255, 140, 0, 0.4)',
        desc: 'Теплый уют'
      }
      
      expect(mode.color).toContain('rgba')
    })
  })

  describe('ConfiguratorStep', () => {
    it('accepts valid configurator step', () => {
      const step: ConfiguratorStep = {
        id: 'type',
        title: 'Ваш автомобиль',
        options: ['Седан', 'Кроссовер', 'Минивэн']
      }
      
      expect(step.options.length).toBe(3)
    })
  })
})

describe('Status Values', () => {
  it('callback status can be new or completed', () => {
    const statuses: CallbackRequest['status'][] = ['new', 'completed']
    expect(statuses).toContain('new')
    expect(statuses).toContain('completed')
  })
})
