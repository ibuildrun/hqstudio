import { describe, it, expect } from 'vitest'

// Validation utilities for forms

const validateRequired = (value: string | undefined | null): boolean => {
  return value !== undefined && value !== null && value.trim().length > 0
}

const validateMinLength = (value: string, minLength: number): boolean => {
  return value.length >= minLength
}

const validateMaxLength = (value: string, maxLength: number): boolean => {
  return value.length <= maxLength
}

const validateEmail = (email: string): boolean => {
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return re.test(email)
}

const validatePhone = (phone: string): boolean => {
  const digits = phone.replace(/\D/g, '')
  return digits.length >= 10 && digits.length <= 11
}

const validatePrice = (price: string): boolean => {
  // Формат: "от X ₽" или "X ₽" или просто число
  const pricePattern = /^(от\s+)?[\d\s]+\s*₽?$/
  return pricePattern.test(price.trim())
}

const validateUrl = (url: string): boolean => {
  try {
    new URL(url)
    return true
  } catch {
    return false
  }
}

const validateCarPlate = (plate: string): boolean => {
  // Российский формат: А123БВ777 или А123БВ77
  // Используем Unicode для русских букв
  const platePattern = /^[АВЕКМНОРСТУХABEKMHOPCTYX]\d{3}[АВЕКМНОРСТУХABEKMHOPCTYX]{2}\d{2,3}$/i
  return platePattern.test(plate.replace(/\s/g, ''))
}

const sanitizeInput = (input: string): string => {
  return input
    .replace(/<[^>]*>/g, '') // Remove HTML tags
    .replace(/[<>]/g, '')    // Remove remaining angle brackets
    .trim()
}

const normalizePhone = (phone: string): string => {
  const digits = phone.replace(/\D/g, '')
  if (digits.length === 11 && digits.startsWith('8')) {
    return '7' + digits.slice(1)
  }
  if (digits.length === 10) {
    return '7' + digits
  }
  return digits
}

describe('Required Validation', () => {
  it('validates non-empty string', () => {
    expect(validateRequired('hello')).toBe(true)
    expect(validateRequired('  hello  ')).toBe(true)
  })

  it('rejects empty values', () => {
    expect(validateRequired('')).toBe(false)
    expect(validateRequired('   ')).toBe(false)
    expect(validateRequired(null)).toBe(false)
    expect(validateRequired(undefined)).toBe(false)
  })
})

describe('Length Validation', () => {
  it('validates minimum length', () => {
    expect(validateMinLength('hello', 3)).toBe(true)
    expect(validateMinLength('hi', 3)).toBe(false)
    expect(validateMinLength('', 0)).toBe(true)
  })

  it('validates maximum length', () => {
    expect(validateMaxLength('hello', 10)).toBe(true)
    expect(validateMaxLength('hello world', 5)).toBe(false)
    expect(validateMaxLength('', 5)).toBe(true)
  })
})

describe('Email Validation', () => {
  it('validates correct emails', () => {
    expect(validateEmail('test@example.com')).toBe(true)
    expect(validateEmail('user.name@domain.ru')).toBe(true)
    expect(validateEmail('user+tag@example.org')).toBe(true)
  })

  it('rejects invalid emails', () => {
    expect(validateEmail('invalid')).toBe(false)
    expect(validateEmail('test@')).toBe(false)
    expect(validateEmail('@domain.com')).toBe(false)
    expect(validateEmail('test @example.com')).toBe(false)
    expect(validateEmail('')).toBe(false)
  })
})

describe('Phone Validation', () => {
  it('validates correct phones', () => {
    expect(validatePhone('+79291234567')).toBe(true)
    expect(validatePhone('89291234567')).toBe(true)
    expect(validatePhone('9291234567')).toBe(true)
    expect(validatePhone('+7 (929) 123-45-67')).toBe(true)
  })

  it('rejects invalid phones', () => {
    expect(validatePhone('123')).toBe(false)
    expect(validatePhone('123456789')).toBe(false)
    expect(validatePhone('')).toBe(false)
  })
})

describe('Price Validation', () => {
  it('validates correct price formats', () => {
    expect(validatePrice('от 15 000 ₽')).toBe(true)
    expect(validatePrice('15000 ₽')).toBe(true)
    expect(validatePrice('от 5000')).toBe(true)
    expect(validatePrice('1000')).toBe(true)
  })

  it('rejects invalid price formats', () => {
    expect(validatePrice('бесплатно')).toBe(false)
    expect(validatePrice('$100')).toBe(false)
    expect(validatePrice('')).toBe(false)
  })
})

describe('URL Validation', () => {
  it('validates correct URLs', () => {
    expect(validateUrl('https://example.com')).toBe(true)
    expect(validateUrl('http://localhost:3000')).toBe(true)
    expect(validateUrl('https://example.com/path?query=1')).toBe(true)
  })

  it('rejects invalid URLs', () => {
    expect(validateUrl('not-a-url')).toBe(false)
    expect(validateUrl('example.com')).toBe(false)
    expect(validateUrl('')).toBe(false)
  })
})

describe('Car Plate Validation', () => {
  it('validates correct Russian plates', () => {
    expect(validateCarPlate('A123BC777')).toBe(true)
    expect(validateCarPlate('A123BC77')).toBe(true)
    expect(validateCarPlate('a123bc777')).toBe(true)
    expect(validateCarPlate('A 123 BC 777')).toBe(true)
  })

  it('rejects invalid plates', () => {
    expect(validateCarPlate('ABC123')).toBe(false)
    expect(validateCarPlate('12345')).toBe(false)
    expect(validateCarPlate('')).toBe(false)
  })
})

describe('Input Sanitization', () => {
  it('removes HTML tags', () => {
    expect(sanitizeInput('<script>alert("xss")</script>')).toBe('alert("xss")')
    expect(sanitizeInput('<b>bold</b>')).toBe('bold')
  })

  it('removes angle brackets', () => {
    expect(sanitizeInput('test < > test')).toBe('test  test')
  })

  it('trims whitespace', () => {
    expect(sanitizeInput('  hello  ')).toBe('hello')
  })

  it('handles normal text', () => {
    expect(sanitizeInput('Normal text')).toBe('Normal text')
  })
})

describe('Phone Normalization', () => {
  it('normalizes 8-prefix to 7', () => {
    expect(normalizePhone('89291234567')).toBe('79291234567')
  })

  it('adds 7 prefix to 10-digit', () => {
    expect(normalizePhone('9291234567')).toBe('79291234567')
  })

  it('keeps 11-digit with 7 prefix', () => {
    expect(normalizePhone('79291234567')).toBe('79291234567')
  })

  it('removes non-digits', () => {
    expect(normalizePhone('+7 (929) 123-45-67')).toBe('79291234567')
  })
})
