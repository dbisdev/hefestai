/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        primary: "#25f46a",
        "primary-dim": "#1a853d",
        "background-dark": "#050a06",
        "surface-dark": "#0d1a10",
        danger: "#ef4444",
      },
      fontFamily: {
        display: ["'VT323'", "monospace"],
        mono: ["'Share Tech Mono'", "monospace"],
        sans: ["'Space Grotesk'", "sans-serif"],
      },
      animation: {
        'glitch-out': 'glitch-out 0.4s forwards ease-in',
        'glitch-in': 'glitch-in 0.5s forwards ease-out',
      },
      keyframes: {
        'glitch-out': {
          '0%': { transform: 'scale(1)', opacity: '1', filter: 'brightness(1) contrast(1)' },
          '20%': { transform: 'scale(1.05, 0.95) skewX(5deg)', filter: 'brightness(2) contrast(1.5)' },
          '40%': { transform: 'scale(0.95, 1.1) skewX(-5deg)', opacity: '0.8' },
          '100%': { transform: 'scale(1, 0)', opacity: '0', filter: 'brightness(5) contrast(2)' },
        },
        'glitch-in': {
          '0%': { transform: 'scale(1.2, 0)', opacity: '0', filter: 'brightness(5)' },
          '50%': { transform: 'scale(0.98, 1.05)', opacity: '0.7', filter: 'brightness(2)' },
          '70%': { transform: 'scale(1.02, 0.98)', opacity: '0.9' },
          '100%': { transform: 'scale(1)', opacity: '1', filter: 'brightness(1)' },
        },
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
