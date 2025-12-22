/**
 * Форматирует номер телефона в формат +7 (XXX) XXX-XX-XX
 */
export function formatPhone(phone?: string): string {
  if (!phone) return '';

  const digits = phone.replace(/\D/g, '');
  if (digits.length === 0) return phone;

  let normalized = digits;
  if (normalized.startsWith('8') && normalized.length === 11) {
    normalized = '7' + normalized.slice(1);
  }
  if (normalized.length === 10) {
    normalized = '7' + normalized;
  }

  if (normalized.length === 11 && normalized.startsWith('7')) {
    return `+7 (${normalized.slice(1, 4)}) ${normalized.slice(4, 7)}-${normalized.slice(7, 9)}-${normalized.slice(9, 11)}`;
  }

  if (normalized.length > 10) {
    return '+' + normalized;
  }

  return phone;
}

/**
 * Форматирует номер телефона в реальном времени при вводе
 */
export function formatPhoneAsYouType(input: string): string {
  if (!input) return '';

  const digits = input.replace(/\D/g, '');
  if (digits.length === 0) return '';

  let normalized = digits;
  if (normalized.startsWith('8')) {
    normalized = '7' + normalized.slice(1);
  }

  if (normalized.length === 1) return `+${normalized}`;
  if (normalized.length >= 2 && normalized.length <= 4) {
    return `+${normalized[0]} (${normalized.slice(1)}`;
  }
  if (normalized.length >= 5 && normalized.length <= 7) {
    return `+${normalized[0]} (${normalized.slice(1, 4)}) ${normalized.slice(4)}`;
  }
  if (normalized.length >= 8 && normalized.length <= 9) {
    return `+${normalized[0]} (${normalized.slice(1, 4)}) ${normalized.slice(4, 7)}-${normalized.slice(7)}`;
  }
  if (normalized.length >= 10 && normalized.length <= 11) {
    return `+${normalized[0]} (${normalized.slice(1, 4)}) ${normalized.slice(4, 7)}-${normalized.slice(7, 9)}-${normalized.slice(9)}`;
  }

  return formatPhone(input);
}

/**
 * React hook для форматирования телефона в input
 */
export function usePhoneFormatter() {
  const handlePhoneChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatPhoneAsYouType(e.target.value);
    e.target.value = formatted;
  };

  const handlePhoneKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    const allowedKeys = ['Backspace', 'Delete', 'Tab', 'Enter', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];
    if (!allowedKeys.includes(e.key) && !/^[0-9]$/.test(e.key)) {
      e.preventDefault();
    }
  };

  const handlePhoneBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    if (e.target.value) {
      e.target.value = formatPhone(e.target.value);
    }
  };

  return {
    onChange: handlePhoneChange,
    onKeyDown: handlePhoneKeyDown,
    onBlur: handlePhoneBlur
  };
}
