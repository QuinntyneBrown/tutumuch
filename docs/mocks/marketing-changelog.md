# Marketing page mock: change log

Bold changes to the `/welcome` mock (`pages/welcome/`), newest first.

| Date | Change | Files |
| --- | --- | --- |
| 2026-10-30 | Closet marquee: a "Fresh in the closet" band under the hero, with a live dot and two rows of real listings (photo, title, price) scrolling in opposite directions. Hovering or focusing a row pauses it. Reduced motion turns each row into a plain horizontal scroller. Every tile opens a listing, and See everything goes to the catalog. Inspired by 21st.dev marquee components. | `_build/screens_marketing.py`, `assets/ui.css` |
| 2026-10-30 | Header logo bigger: the wordmark is 2.5rem tall (was 1.75rem); the mobile mark is 2.25rem. Applies to every page's topbar. | `assets/ui.css` |
| 2026-10-30 | Tulle field: a canvas behind the hero where tulle petals drift up and twirl around the pointer, and a tap throws sparkles. Colours are Giddy palette tokens, so dark mode works. Reduced motion paints one still frame, and the animation pauses when the hero is offscreen. | `_build/screens_marketing.py`, `assets/mock.js`, `assets/ui.css` |
