import { describe, it, expect } from 'vitest'

// Utility functions to test
const formatPhone = (phone: string): string => {
  const digits = phone.replace(/\D/g, '')
  if (digits.length === 11 && digits.startsWith('7')) {
    return `+7 (${digits.slice(1, 4)}) ${digits.slice(4, 7)}-${digits.slice(7, 9)}-${digits.slice(9)}`
  }
  if (digits.length === 10) {
    return `+7 (${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6, 8)}-${digits.slice(8)}`
  }
  return phone
}

const validateEmail = (email: string): boolean => {
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return re.test(email)
}

const validatePhone = (phone: string): boolean => {
  const digits = phone.replace(/\D/g, '')
  return digits.length >= 10 && digits.length <= 11
}

const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text
  return text.slice(0, maxLength - 3) + '...'
}

const formatPrice = (price: number): string => {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: 0
  }).format(price)
}

const slugify = (text: string): string => {
  return text
    .toLowerCase()
    .replace(/[а-яё]/g, (char) => {
      const map: Record<string, string> = {
        'а': 'a', 'б': 'b', 'в': 'v', 'г': 'g', 'д': 'd', 'е': 'e', 'ё': 'yo',
        'ж': 'zh', 'з': 'z', 'и': 'i', 'й': 'y', 'к': 'k', 'л': 'l', 'м': 'm',
        'н': 'n', 'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't', 'у': 'u',
        'ф': 'f', 'х': 'h', 'ц': 'ts', 'ч': 'ch', 'ш': 'sh', 'щ': 'sch', 'ъ': '',
        'ы': 'y', 'ь': '', 'э': 'e', 'ю': 'yu', 'я': 'ya'
      }
      return map[char] || char
    })
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
}

describe('Phone Formatting', () => {
  it('formats 11-digit phone starting with 7', () => {
    expect(formatPhone('79291234567')).toBe('+7 (929) 123-45-67')
  })

  it('formats 10-digit phone', () => {
    expect(formatPhone('9291234567')).toBe('+7 (929) 123-45-67')
  })

  it('handles phone with special characters', () => {
    expect(formatPhone('+7 (929) 123-45-67')).toBe('+7 (929) 123-45-67')
  })

  it('returns original for invalid format', () => {
    expect(formatPhone('123')).toBe('123')
  })
})

describe('Email Validation', () => {
  it('validates correct email', () => {
    expect(validateEmail('test@example.com')).toBe(true)
    expect(validateEmail('user.name@domain.ru')).toBe(true)
  })

  it('rejects invalid email', () => {
    expect(validateEmail('invalid')).toBe(false)
    expect(validateEmail('test@')).toBe(false)
    expect(validateEmail('@domain.com')).toBe(false)
    expect(validateEmail('')).toBe(false)
  })
})

describe('Phone Validation', () => {
  it('validates correct phone', () => {
    expect(validatePhone('+79291234567')).toBe(true)
    expect(validatePhone('89291234567')).toBe(true)
    expect(validatePhone('9291234567')).toBe(true)
  })

  it('rejects invalid phone', () => {
    expect(validatePhone('123')).toBe(false)
    expect(validatePhone('')).toBe(false)
  })
})

describe('Text Truncation', () => {
  it('truncates long text', () => {
    expect(truncateText('Hello World', 8)).toBe('Hello...')
  })

  it('keeps short text unchanged', () => {
    expect(truncateText('Hello', 10)).toBe('Hello')
  })

  it('handles exact length', () => {
    expect(truncateText('Hello', 5)).toBe('Hello')
  })
})

describe('Price Formatting', () => {
  it('formats price in rubles', () => {
    const result = formatPrice(15000)
    expect(result).toContain('15')
    expect(result).toContain('000')
  })

  it('formats zero price', () => {
    const result = formatPrice(0)
    expect(result).toContain('0')
  })
})

describe('Slugify', () => {
  it('converts Russian text to slug', () => {
    expect(slugify('Шумоизоляция')).toBe('shumoizolyatsiya')
  })

  it('handles mixed text', () => {
    expect(slugify('Test Тест 123')).toBe('test-test-123')
  })

  it('removes special characters', () => {
    expect(slugify('Hello, World!')).toBe('hello-world')
  })

  it('handles empty string', () => {
    expect(slugify('')).toBe('')
  })
})
