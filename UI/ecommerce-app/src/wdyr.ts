// This file is imported BEFORE React in main.tsx (dev builds only).
// It monkey-patches React to log unnecessary re-renders to the console.
// Zero production impact — the dynamic import never runs in prod.

import React from 'react';

if (import.meta.env.DEV) {
  const { default: whyDidYouRender } = await import('@welldone-software/why-did-you-render');
  whyDidYouRender(React, {
    trackAllPureComponents: true, // automatically track all React.memo components
    logOnDifferentValues: true, // log when prev and next props/state differ
  });
}

export {};
