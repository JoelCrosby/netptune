import type { Config } from 'tailwindcss';
import colors from 'tailwindcss/colors';
import defaultTheme from 'tailwindcss/defaultTheme';
import plugin from 'tailwindcss/plugin';

const config: Config = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', ...defaultTheme.fontFamily.sans],
      },
      colors: {
        brand: colors.indigo,
      },
    },
  },
  plugins: [
    plugin(({ addVariant }) => {
      addVariant('link-active', ['&.active', '&>.active']);
    }),
  ],
};

export default config;
