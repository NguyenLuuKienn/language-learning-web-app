/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Mali"', '"Itim"', '"M PLUS Rounded 1c"', '"Nunito"', 'sans-serif'],
      }
    },
  },
  plugins: [],
}
