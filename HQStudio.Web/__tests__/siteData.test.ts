import { describe, it, expect } from 'vitest'
import { DEFAULT_SITE_DATA, SERVICES, STATS, APP_NAME, PHONE, ADDRESS } from '../lib/constants'

describe('Constants', () => {
  describe('APP_NAME', () => {
    it('is defined and non-empty', () => {
      expect(APP_NAME).toBeDefined()
      expect(APP_NAME.length).toBeGreaterThan(0)
    })
  })

  describe('PHONE', () => {
    it('is in correct format', () => {
      expect(PHONE).toMatch(/^\+7/)
    })
  })

  describe('ADDRESS', () => {
    it('contains city name', () => {
      expect(ADDRESS).toContain('Сургут')
    })
  })

  describe('STATS', () => {
    it('has valid rating', () => {
      expect(STATS.rating).toBeGreaterThanOrEqual(0)
      expect(STATS.rating).toBeLessThanOrEqual(5)
    })

    it('has positive count', () => {
      expect(STATS.count).toBeGreaterThan(0)
    })
  })
})

describe('SERVICES', () => {
  it('has at least one service', () => {
    expect(SERVICES.length).toBeGreaterThan(0)
  })

  it('each service has required fields', () => {
    SERVICES.forEach(service => {
      expect(service.id).toBeDefined()
      expect(service.title).toBeDefined()
      expect(service.description).toBeDefined()
      expect(service.price).toBeDefined()
      expect(service.category).toBeDefined()
    })
  })

  it('each service has unique id', () => {
    const ids = SERVICES.map(s => s.id)
    const uniqueIds = new Set(ids)
    expect(uniqueIds.size).toBe(ids.length)
  })

  it('prices are in correct format', () => {
    SERVICES.forEach(service => {
      expect(service.price).toMatch(/₽/)
    })
  })
})

describe('DEFAULT_SITE_DATA', () => {
  describe('Basic Info', () => {
    it('has app name', () => {
      expect(DEFAULT_SITE_DATA.appName).toBe(APP_NAME)
    })

    it('has phone', () => {
      expect(DEFAULT_SITE_DATA.phone).toBe(PHONE)
    })

    it('has address', () => {
      expect(DEFAULT_SITE_DATA.address).toBe(ADDRESS)
    })

    it('has owner name', () => {
      expect(DEFAULT_SITE_DATA.ownerName).toBeDefined()
      expect(DEFAULT_SITE_DATA.ownerName.length).toBeGreaterThan(0)
    })
  })

  describe('Hero Section', () => {
    it('has hero image URL', () => {
      expect(DEFAULT_SITE_DATA.heroImage).toMatch(/^https?:\/\//)
    })

    it('has hero title', () => {
      expect(DEFAULT_SITE_DATA.heroTitle).toBeDefined()
    })

    it('has hero subtitle', () => {
      expect(DEFAULT_SITE_DATA.heroSubtitle).toBeDefined()
    })
  })

  describe('Services', () => {
    it('has services array', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.services)).toBe(true)
      expect(DEFAULT_SITE_DATA.services.length).toBeGreaterThan(0)
    })
  })

  describe('FAQ Items', () => {
    it('has FAQ items', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.faqItems)).toBe(true)
    })

    it('each FAQ has question and answer', () => {
      DEFAULT_SITE_DATA.faqItems.forEach(item => {
        expect(item.q).toBeDefined()
        expect(item.a).toBeDefined()
        expect(item.q.length).toBeGreaterThan(0)
        expect(item.a.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Process Steps', () => {
    it('has process steps', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.processSteps)).toBe(true)
      expect(DEFAULT_SITE_DATA.processSteps.length).toBeGreaterThan(0)
    })

    it('each step has required fields', () => {
      DEFAULT_SITE_DATA.processSteps.forEach(step => {
        expect(step.num).toBeDefined()
        expect(step.title).toBeDefined()
        expect(step.text).toBeDefined()
      })
    })

    it('steps are numbered sequentially', () => {
      const nums = DEFAULT_SITE_DATA.processSteps.map(s => parseInt(s.num))
      for (let i = 1; i < nums.length; i++) {
        expect(nums[i]).toBeGreaterThan(nums[i - 1])
      }
    })
  })

  describe('Showcase Items', () => {
    it('has showcase items', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.showcaseItems)).toBe(true)
    })

    it('each item has required fields', () => {
      DEFAULT_SITE_DATA.showcaseItems.forEach(item => {
        expect(item.title).toBeDefined()
        expect(item.desc).toBeDefined()
        expect(item.img).toMatch(/^https?:\/\//)
      })
    })
  })

  describe('Testimonials', () => {
    it('has testimonials', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.testimonials)).toBe(true)
    })

    it('each testimonial has required fields', () => {
      DEFAULT_SITE_DATA.testimonials.forEach(t => {
        expect(t.name).toBeDefined()
        expect(t.car).toBeDefined()
        expect(t.text).toBeDefined()
      })
    })
  })

  describe('Moodlight Modes', () => {
    it('has moodlight modes', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.moodlightModes)).toBe(true)
      expect(DEFAULT_SITE_DATA.moodlightModes.length).toBeGreaterThan(0)
    })

    it('each mode has required fields', () => {
      DEFAULT_SITE_DATA.moodlightModes.forEach(mode => {
        expect(mode.id).toBeDefined()
        expect(mode.name).toBeDefined()
        expect(mode.color).toBeDefined()
        expect(mode.desc).toBeDefined()
      })
    })

    it('each mode has unique id', () => {
      const ids = DEFAULT_SITE_DATA.moodlightModes.map(m => m.id)
      const uniqueIds = new Set(ids)
      expect(uniqueIds.size).toBe(ids.length)
    })
  })

  describe('Configurator Steps', () => {
    it('has configurator steps', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.configuratorSteps)).toBe(true)
    })

    it('each step has options', () => {
      DEFAULT_SITE_DATA.configuratorSteps.forEach(step => {
        expect(step.id).toBeDefined()
        expect(step.title).toBeDefined()
        expect(Array.isArray(step.options)).toBe(true)
        expect(step.options.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Site Blocks', () => {
    it('has site blocks', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.siteBlocks)).toBe(true)
      expect(DEFAULT_SITE_DATA.siteBlocks.length).toBeGreaterThan(0)
    })

    it('each block has required fields', () => {
      DEFAULT_SITE_DATA.siteBlocks.forEach(block => {
        expect(block.id).toBeDefined()
        expect(block.name).toBeDefined()
        expect(typeof block.enabled).toBe('boolean')
      })
    })

    it('each block has unique id', () => {
      const ids = DEFAULT_SITE_DATA.siteBlocks.map(b => b.id)
      const uniqueIds = new Set(ids)
      expect(uniqueIds.size).toBe(ids.length)
    })

    it('has hero block enabled by default', () => {
      const heroBlock = DEFAULT_SITE_DATA.siteBlocks.find(b => b.id === 'hero')
      expect(heroBlock).toBeDefined()
      expect(heroBlock!.enabled).toBe(true)
    })

    it('has contact block enabled by default', () => {
      const contactBlock = DEFAULT_SITE_DATA.siteBlocks.find(b => b.id === 'contact')
      expect(contactBlock).toBeDefined()
      expect(contactBlock!.enabled).toBe(true)
    })
  })

  describe('Form Config', () => {
    it('has form config', () => {
      expect(DEFAULT_SITE_DATA.formConfig).toBeDefined()
    })

    it('has boolean flags', () => {
      expect(typeof DEFAULT_SITE_DATA.formConfig.showCarModel).toBe('boolean')
      expect(typeof DEFAULT_SITE_DATA.formConfig.showLicensePlate).toBe('boolean')
    })
  })

  describe('Gallery', () => {
    it('has gallery image URL', () => {
      expect(DEFAULT_SITE_DATA.galleryImage).toMatch(/^https?:\/\//)
    })

    it('has gallery features', () => {
      expect(Array.isArray(DEFAULT_SITE_DATA.galleryFeatures)).toBe(true)
      expect(DEFAULT_SITE_DATA.galleryFeatures.length).toBeGreaterThan(0)
    })
  })
})

describe('Data Integrity', () => {
  it('all image URLs are valid', () => {
    const imageUrls = [
      DEFAULT_SITE_DATA.heroImage,
      DEFAULT_SITE_DATA.galleryImage,
      DEFAULT_SITE_DATA.moodlightImage,
      ...DEFAULT_SITE_DATA.showcaseItems.map(i => i.img),
      ...DEFAULT_SITE_DATA.services.map(s => s.image)
    ]

    imageUrls.forEach(url => {
      expect(url).toMatch(/^https?:\/\//)
    })
  })

  it('no empty strings in required fields', () => {
    expect(DEFAULT_SITE_DATA.appName.trim()).not.toBe('')
    expect(DEFAULT_SITE_DATA.phone.trim()).not.toBe('')
    expect(DEFAULT_SITE_DATA.address.trim()).not.toBe('')
    expect(DEFAULT_SITE_DATA.heroTitle.trim()).not.toBe('')
  })
})
