/** @type {import('tailwindcss').Config} */
const path = require('path');
module.exports = {
  content: [
    path.resolve(__dirname, 'src/Components/**/*.{razor,cshtml,html}'),
    path.resolve(__dirname, 'src/wwwroot/**/*.html'),
    path.resolve(__dirname, 'src/Pages/**/*.{razor,html}')
  ],
  theme: {
    extend: {
      colors: {
        page: { DEFAULT: '#f6f2eb', dark: '#1c1814' },
        surface: { DEFAULT: '#f0ebe2', 2: '#e8e0d4', dark: '#2a2520', 'dark-2': '#353028' },
        shelf: { DEFAULT: '#d4c5a9', dark: '#5a4d38' },
        ink: { DEFAULT: '#2c2416', dark: '#ede4d4' },
        muted: { DEFAULT: '#9a9080', dark: '#8f8878' },
        brass: { DEFAULT: '#b8863a', dark: '#d4a65a' },
        danger: { DEFAULT: '#b34a3a', dark: '#d46a5a' },
        success: { DEFAULT: '#4a7c5a', dark: '#6a9c7a' },
      },
      fontFamily: {
        display: ['Fraunces', 'Georgia', 'serif'],
        body: ['Figtree', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', '"SF Mono"', 'monospace'],
      },
      fontSize: {
        '2xs': ['0.6875rem', { lineHeight: '1rem' }],
      },
      keyframes: {
        'spine-tilt': {
          '0%': { transform: 'translateY(0) rotate(0deg)' },
          '100%': { transform: 'translateY(-3px) rotate(0.8deg)' },
        },
        'spine-settle': {
          '0%': { transform: 'translateY(-3px) rotate(0.8deg)' },
          '100%': { transform: 'translateY(0) rotate(0deg)' },
        },
        'fade-in': {
          '0%': { opacity: '0', transform: 'translateY(4px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-in': {
          '0%': { opacity: '0', transform: 'translateX(-12px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
      },
      animation: {
        'spine-tilt': 'spine-tilt 0.2s ease-out forwards',
        'spine-settle': 'spine-settle 0.25s ease-in forwards',
        'fade-in': 'fade-in 0.3s ease-out',
        'slide-in': 'slide-in 0.3s ease-out',
      },
    }
  },
  plugins: []
};
