import { describe, it, expect } from 'vitest'
import { SERVICES, DEFAULT_SITE_DATA, APP_NAME, PHONE, ADDRESS } from '../lib/constants'

describe('Constants', () => {
  it('has valid APP_NAME', () => {
    expect(APP_NAME).toBe('Hq_studio')
  })

  it('has valid PHONE format', () => {
    expect(PHONE).toMatch(/^\+7 \d{3} \d{3}-\d{2}-\d{2}$/)
  })

  it('has valid ADDRESS', () => {
    expect(ADDRESS).toContain('Сургут')
  })
})

describe('Services', () => {
  it('has at least 4 services', () => {
    expect(SERVICES.length).toBeGreaterThanOrEqual(4)
  })

  it('each service has required fields', () => {
    SERVICES.forEach(service => {
      expect(service.id).toBeTruthy()
      expect(service.title).toBeTruthy()
      expect(service.description).toBeTruthy()
      expect(service.price).toBeTruthy()
      expect(service.category).toBeTruthy()
      expect(service.image).toBeTruthy()
    })
  })

  it('service IDs are unique', () => {
    const ids = SERVICES.map(s => s.id)
    const uniqueIds = new Set(ids)
    expect(uniqueIds.size).toBe(ids.length)
  })

  it('prices contain currency symbol', () => {
    SERVICES.forEach(service => {
      expect(service.price).toContain('₽')
    })
  })
})

describe('Default Site Data', () => {
  it('has all required sections', () => {
    expect(DEFAULT_SITE_DATA.appName).toBeTruthy()
    expect(DEFAULT_SITE_DATA.phone).toBeTruthy()
    expect(DEFAULT_SITE_DATA.address).toBeTruthy()
    expect(DEFAULT_SITE_DATA.services).toBeTruthy()
    expect(DEFAULT_SITE_DATA.faqItems).toBeTruthy()
    expect(DEFAULT_SITE_DATA.processSteps).toBeTruthy()
    expect(DEFAULT_SITE_DATA.testimonials).toBeTruthy()
    expect(DEFAULT_SITE_DATA.siteBlocks).toBeTruthy()
  })

  it('has valid FAQ items', () => {
    DEFAULT_SITE_DATA.faqItems.forEach(item => {
      expect(item.q).toBeTruthy()
      expect(item.a).toBeTruthy()
    })
  })

  it('has valid process steps', () => {
    DEFAULT_SITE_DATA.processSteps.forEach(step => {
      expect(step.num).toBeTruthy()
      expect(step.title).toBeTruthy()
      expect(step.text).toBeTruthy()
    })
  })

  it('has valid testimonials', () => {
    DEFAULT_SITE_DATA.testimonials.forEach(testimonial => {
      expect(testimonial.name).toBeTruthy()
      expect(testimonial.car).toBeTruthy()
      expect(testimonial.text).toBeTruthy()
    })
  })

  it('has valid site blocks', () => {
    expect(DEFAULT_SITE_DATA.siteBlocks.length).toBeGreaterThan(0)
    DEFAULT_SITE_DATA.siteBlocks.forEach(block => {
      expect(block.id).toBeTruthy()
      expect(block.name).toBeTruthy()
      expect(typeof block.enabled).toBe('boolean')
    })
  })

  it('site blocks have unique IDs', () => {
    const ids = DEFAULT_SITE_DATA.siteBlocks.map(b => b.id)
    const uniqueIds = new Set(ids)
    expect(uniqueIds.size).toBe(ids.length)
  })

  it('has valid moodlight modes', () => {
    DEFAULT_SITE_DATA.moodlightModes.forEach(mode => {
      expect(mode.id).toBeTruthy()
      expect(mode.name).toBeTruthy()
      expect(mode.color).toBeTruthy()
      expect(mode.desc).toBeTruthy()
    })
  })

  it('has valid configurator steps', () => {
    DEFAULT_SITE_DATA.configuratorSteps.forEach(step => {
      expect(step.id).toBeTruthy()
      expect(step.title).toBeTruthy()
      expect(step.options.length).toBeGreaterThan(0)
    })
  })

  it('form config has required fields', () => {
    expect(typeof DEFAULT_SITE_DATA.formConfig.showCarModel).toBe('boolean')
    expect(typeof DEFAULT_SITE_DATA.formConfig.showLicensePlate).toBe('boolean')
  })
})
