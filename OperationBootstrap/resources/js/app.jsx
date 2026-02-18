import './bootstrap';
import { createInertiaApp } from '@inertiajs/react';
import { createRoot } from 'react-dom/client';

createInertiaApp({
  resolve: (name) => {
    const pages = import.meta.glob('./Pages/**/*.jsx');
    const page = pages[`./Pages/${name}.jsx`];

    if (!page) {
      throw new Error(`Inertia page not found: ./Pages/${name}.jsx`);
    }

    return page();
  },
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />);
  },
});
