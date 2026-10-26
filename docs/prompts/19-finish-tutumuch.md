# Finish TuTuMuch: all things young girls, pitch video music, diagrams and a green verify

Run this prompt from the repository root on a machine with full internet access. It finishes the work left
after TuTuMuch was cloned from 12L, re-branded and given the marketing page. Read `AGENTS.md` first; it is
binding.

## Where things stand

TuTuMuch is a clone of 12L (an open marketplace for men's US 12 shoes and men's L clothing), turned into a
marketplace for girls. What is done:

- Rename: solution `src/backend/TuTuMuch.sln`, namespaces `TuTuMuch.*`, product name "TuTuMuch", Angular
  application `projects/tutumuch`, path aliases `@tutumuch/*`. The component selector prefix stays 12L's
  `tl` (`tl-*`, `tlButton`). Order numbers are `TM-48302`.
- Domain today: four clothing categories in
  `src/backend/TuTuMuch.Domain/Listings/CategoryCatalog.cs`: Dresses (Dresses, Skirts, Tutus, Rompers),
  Tops (Shirts, T-shirts, Sweaters, Hoodies), Outerwear (Jackets, Coats), Bottoms (Pants, Leggings,
  Shorts). The single accepted size is `Girls'` (constant `CategoryCatalog.Girls`, size tag label
  `Girls`), backed by the `CK_Listings_Size` check constraint (`Size = 'Girls'''`).
- Look: the Giddy design system with the TuTuMuch brand palette (Raspberry `#D6336C`, Lilac `#9B7FD4`,
  Blush `#FDE2EA`, Sunny `#F6C64B`, Cream `#FFF8F5`, Ink `#3A2233`). The three token files
  (`src/frontend/projects/components/src/styles/tokens.css`, `docs/design-system/tokens/tokens.css`,
  `docs/mocks/assets/tokens.css`) are Giddy's `tokens.css` followed by a "TuTuMuch brand palette" block
  and a "TuTuMuch layout tokens" block. Change colours only in those blocks, the same in all three files.
- Logo: the live Gingham wordmark (Titan One, self-hosted in
  `src/frontend/projects/tutumuch/public/fonts/`) in `src/frontend/projects/components/src/lib/logo/`.
  The favicon and app icons come from the brand kit's raspberry "T" mark.
- Marketing: the `marketing` library (`src/frontend/projects/marketing/`, page `/welcome`), with
  requirements `L1-020` and `L2-078` to `L2-083`, ADR general/0005 and platform/0011, and the design in
  `docs/detailed-designs/platform/marketing-page/`.
- Pitch video: `docs/mocks/_build/video/build_video.py` builds the 30-second video into
  `src/frontend/projects/tutumuch/public/media/tutumuch-pitch.*` and `docs/mocks/assets/media/`. The
  committed video is narrated (Kokoro `af_bella`) but has **no music**.
- Verified before this prompt: the backend tests (449), lint, `npm run build:all`, the Playwright suite
  (208 tests; one known intermittent failure, see Part 4), and the design-system, contrast and mock checkers.

## What is left (the scope of this prompt)

1. Part 1: widen TuTuMuch from girls' clothing to **all things young girls**.
2. Part 2: add the background music to the pitch video and finish it.
3. Part 3: re-render every PlantUML diagram (all PNGs still show "12L"; the marketing-page diagrams have no
   PNG at all).
4. Part 4: fix the intermittent Playwright test and get `npm run verify` fully green.
5. Part 5: bring docs, estimates and the changelog in line, then commit, push and open a pull request.

## Finish the whole job in one run. Do not stop. (CRITICAL)

- You are authorised to take this all the way through Parts 1 to 5: requirements, ADR, mocks, design
  system, design docs, code, tests, media, diagrams, green verify, a pushed branch and a pull request. Go
  through every part in one continuous run. Do not pause between parts to report, ask for approval or wait
  for "continue".
- Do not end your turn until the "Done means" list at the bottom is fully true. A partial result, a plan,
  or "the next steps would be..." is not done.
- Where `AGENTS.md` says "stop and ask", treat this prompt as confirmation in advance for everything in
  scope here. Choose the simplest option that fits the existing architecture, record it (ADR or the
  design's open questions), and keep going.
- When a test, build or command fails, find the root cause and fix it. Never weaken, skip or delete a test
  to make it pass.
- Only stop for a real blocker you cannot resolve yourself, such as a download that needs a login. Finish
  everything else first, then report the blocker clearly.
- Keep a todo list so nothing is lost if your context is summarised.

## Rules (binding)

- Follow `AGENTS.md` exactly: folder layout, the components library is mandatory
  (`docs/screen-composition.md` is binding), backend tests never touch a real database, and Playwright
  never runs the backend.
- Keep 12L's structure: every journey, screen, component and the `tl` prefix stay. Do not rename projects
  or folders.
- Out of scope stays out of scope (moderation, writing reviews, data export, load tests, frontend
  performance testing).
- Mocks: edit only `docs/mocks/_build/*.py`, then run `python docs/mocks/_build/build.py` and
  `python .claude/skills/writing-html-mocks/scripts/check_mocks.py docs/mocks --write`. Never hand-edit
  generated HTML.
- Run every Python script with `python -B -I` so no `__pycache__` lands in the repo.
- Copy rule (ADR general/0005): everything is for girls; never promise boys' or adult items or sizes.
- Requirement IDs are stable. Retired numbers stay retired. New requirements take the next free number.

## Part 1: All things young girls

TuTuMuch sells new and gently loved **clothes, shoes, dance gear, sports equipment, cameras and tech,
toys and crafts: all things young girls**. Today it accepts only four clothing categories.

### 1.1 Decide and record

Write ADR `docs/adr/general/0006-all-things-young-girls.md`. It widens the catalogue to the categories
below and keeps the size lock's mechanism as an **audience lock**: every listing is for girls. Keep
`CategoryCatalog.Girls` (`Girls'`) as the one accepted value and the `Girls` size tag on every card. The
seller's confirmation changes from "the label shows girls' sizing" to "this item is for girls", because
cameras and bikes have no clothing label. Record the open question: do real sizes (2T to 16, shoe sizes)
come later as a separate attribute? That is deferred, not built here.

| Category (route) | Subcategories |
|---|---|
| Clothing (`/clothing`) | Dresses, Tops, Bottoms, Outerwear, Pajamas, Swimwear |
| Shoes (`/shoes`) | Sneakers, Boots, Sandals, Dress shoes |
| Dance (`/dance`) | Tutus, Leotards, Dance shoes, Dance bags |
| Sports (`/sports`) | Soccer, Volleyball, Gymnastics, Swimming, Bikes and scooters |
| Cameras and tech (`/cameras-and-tech`) | Cameras, Headphones, Tablet accessories |
| Toys and crafts (`/toys-and-crafts`) | Dolls, Arts and crafts, Games and puzzles, Books |

Six categories with 26 subcategories. The current Dresses, Tops, Outerwear and Bottoms become
subcategories of Clothing; skirts, tutus and rompers move into Clothing → Dresses or Dance → Tutus.
Categories are code, not configuration (ADR comment in `CategoryCatalog.cs`); keep it that way.

### 1.2 Requirements

Update `docs/specs/L1.md` and `docs/specs/L2.md`:

- `L1-003` and the intro: all things young girls, not only clothing.
- `L2-011`: the table above, and the counts in its acceptance criteria (6 categories, 26 subcategories).
- `L2-012` (fixed size on the sell form), `L2-013` (size lock) and `L2-014` (confirmation): audience
  wording. The confirmation checkbox reads "This item is for girls", and the error reads "Confirm this
  item is for girls".
- `L2-022` (category pages and their routes), `L2-023`/`L2-024` (search and filter examples) and
  `L2-075` (seed: at least 8 Active listings in each of the six categories, 40 to 80 in total, so
  48 to 80 now).
- `docs/journeys.md`: wording only.

### 1.3 Mocks and design system

- `docs/mocks/_build/data.py`, `mk.py` (`NAV`, `nav_href`, `size_of`, `size_long`), `screens_catalog.py`,
  `screens_selling.py`, `screens_system.py`, `screens_marketing.py`: six categories, girls' items across
  all of them, the new confirmation copy and a new catalog hero. The current hero is "Girls' clothing.
  *Nothing else.*" Use "Everything for girls. *Nothing else.*", and the lead "Clothes, dance and sports
  gear, cameras, toys and more, from families whose girls have outgrown them." Add item art to
  `docs/mocks/assets/items/` for the new categories (sneakers, leotard, soccer ball, camera, headphones,
  doll, paint set...) in the existing flat style. Rebuild and check.
- The top bar must fit six category links without horizontal scroll at 320px (the Playwright shell tests
  check this). If it does not fit, follow the mocks' small-screen pattern (menu sheet) and keep desktop
  links. Update `docs/design-system/components/top-bar.html`, `chip.html`, `fixed-size.html` and
  `size-tag.html` examples, and `docs/screen-composition.md`.
- Run `python -B -I .claude/skills/extracting-design-systems/scripts/check_design_system.py docs/design-system`
  and `check_contrast.py docs/design-system/tokens/tokens.css`; both must pass.

### 1.4 Backend

- `src/backend/TuTuMuch.Domain/Listings/CategoryCatalog.cs`: the new table, all with `Girls`.
- `src/backend/TuTuMuch.Domain/Listings/Listing.cs`: the `SIZE_CONFIRMATION_REQUIRED` message becomes
  "Confirm this item is for girls"; check the `SIZE_NOT_ACCEPTED` message reads well for every category.
- `CK_Listings_Size` stays `Size = 'Girls'''` (no migration needed). If you change any persisted shape,
  add an EF Core migration with the CLI. Never hand-edit the model snapshot.
- `src/backend/TuTuMuch.Application/Catalog/CatalogCriteria.cs`: category and subcategory slugs for the
  new names ("Bikes and scooters" becomes `bikes-and-scooters`).
- Email footer in `src/backend/TuTuMuch.Application/Notifications/EmailTemplates.cs`: "the marketplace
  for all things girls" or similar.
- Demo seed `src/backend/TuTuMuch.Cli/DemoCatalogue.cs`: 8 personas (keep TutuTessa and the others),
  48 to 80 listings with at least 8 per category, girls' brands for each category (for example Carter's,
  Hanna Andersson and Mini Boden for clothes; Stride Rite and Native for shoes; Capezio and Bloch for
  dance; Adidas Kids and Speedo for sports; Fujifilm Instax, Kodak and JLab for cameras and tech; Melissa
  & Doug, Crayola and LEGO for toys and crafts). Add each brand to `Selling:SuggestedBrands` in
  `src/backend/TuTuMuch.Api/appsettings.json`. Use realistic prices, keep the mix of conditions, the Fair
  items with flaws and the free-shipping cases.
- Seed photos `src/backend/TuTuMuch.Cli/SeedPhotos/{Category}/`: one folder per category, at least 6
  square 1024×1024 JPEGs (quality about 82, under 400 KB each) per category. The current images are
  original illustrations generated with Python Pillow (flat pastel shapes on a soft background). Make
  the new ones the same way, or use photos whose licence allows redistribution, and list every file in
  `SeedPhotos/ATTRIBUTION.md` (the CLI tests require that). Do not commit the generator into `docs/` or
  `src/` unless AGENTS.md allows the folder.
- Backend tests (`tests/TuTuMuch.*.Tests/`): update every category, subcategory, count and message
  assertion. Start with `CategoryCatalogTests`, `CatalogueTests`, `ListingDraftTests`,
  `PublishListingTests`, `SearchAndFilterCatalogTests`, `BrowseCatalogTests` and `SeedListingsTests`,
  then search for the rest (`grep -rn "Dresses\|Outerwear\|Bottoms\|girls' sizing" tests`). Add tests for
  a non-clothing category (a Cameras and tech listing publishes with `Girls'`; a wrong subcategory is
  refused).

### 1.5 Frontend

- `src/frontend/projects/catalog/src/lib/catalog/catalog-query.ts` (`CATEGORIES`),
  `catalog.routes.ts` (one route per category, titled "{Category} · TuTuMuch"),
  `catalog-page.html` (the new hero), and
  `src/frontend/projects/components/src/lib/top-bar/top-bar.ts` (nav links).
- `src/frontend/projects/listings/src/lib/listing-form/listing-rules.ts` (`confirmLabel`, the confirm
  error) and the sell form.
- `src/frontend/projects/catalog/src/lib/listing-detail/listing-detail-page.html` ("More girls'
  clothing" becomes "More for girls").
- Marketing copy (`src/frontend/projects/marketing/src/lib/welcome/`) already names clothes, dance,
  sports and cameras. Check it against the final categories.
- Playwright: update `src/frontend/e2e/fixtures.ts` (`categories`, catalog cards) and every spec that
  names a category, the hero or the confirmation text, starting with `sell.spec.ts`,
  `browse-catalog.spec.ts`, `shell.spec.ts`, `marketing-page.spec.ts`,
  `search-and-filter-catalog.spec.ts`, `view-listing-detail.spec.ts`,
  `edit-and-withdraw-listing.spec.ts` and `upload-listing-photos.spec.ts`. Add one end-to-end check that
  a non-clothing category page (for example `/cameras-and-tech`) lists cards with the `Girls` tag.

### 1.6 Docs in the same change

- `docs/detailed-designs/listings/create-listing/`, `catalog/browse-catalog/`,
  `catalog/search-and-filter-catalog/`, `catalog/view-listing-detail/`,
  `platform/seed-demo-data/` (README and `.puml` text), and `docs/estimates/` (rows and totals stay
  consistent).
- `README.md` overview table, `AGENTS.md` intro paragraph (it still says "girls' (children's) clothing:
  dresses, tops, outerwear and bottoms"), `CONTRIBUTING.md` focus paragraph.
- The video script's slide tags (`build_video.py`, `SLIDES[2]`) should match the categories.

## Part 2: Pitch video music

1. Download "Upbeat Ukulele Kids" by Cold_Fire from
   https://pixabay.com/music/happy-childrens-tunes-upbeat-ukulele-kids-187874/ (Pixabay Content Licence,
   free for commercial use, no attribution required). The page needs a normal browser; if a login wall or
   bot check blocks you, report it as the blocker and finish everything else.
2. Cut the excerpt:
   `ffmpeg -ss 4 -t 34 -i upbeat-ukulele-kids-187874.mp3 -af "afade=t=out:st=32:d=2" -b:a 160k docs/mocks/_build/video/music/upbeat-ukulele-kids-excerpt.mp3`.
   Listen to the first seconds and pick a start that begins on the beat if 4 s does not.
3. Install the build's Python dependencies: `pip install playwright kokoro-onnx onnxruntime
   huggingface_hub numpy`, then `python -m playwright install chromium`. The Kokoro model (about 330 MB)
   downloads into `docs/mocks/_build/video/kokoro/`, which is git-ignored.
4. If Part 1 changed the slide tags, update `SLIDES` and `LINES` in `build_video.py`. Keep the 30-second
   target. Captions must match the narration.
5. Build: `python -I docs/mocks/_build/video/build_video.py`. It must print "narrated, with music".
6. Verify: `ffprobe` shows 1920×1080 H.264 + AAC, about 30.0 s, and WebM VP9 + Opus. Check the music is
   audible but ducked under the voice (`ffmpeg -i ... -af volumedetect`), and spot-check frames at the
   caption times.
7. Update `docs/mocks/_build/video/ATTRIBUTION.md` (remove "no music yet") and `README.md`, and in
   `docs/detailed-designs/platform/marketing-page/README.md` remove the interim-video note. Rebuild so the
   media folders get the new ATTRIBUTION. In `docs/estimates/platform/marketing-page.md`, set the
   marketing page to 100% and recompute the totals.
8. Commit the excerpt MP3 (it is small and makes the build repeatable), the media and the docs.

## Part 3: Diagrams

1. Install Graphviz and a recent `plantuml.jar` (one that bundles the C4 standard library). Set
   `PLANTUML_JAR` if it is not on `PATH`.
2. Render every diagrams folder:
   `for d in $(find docs/detailed-designs -type d -name diagrams); do python -B -I .claude/skills/writing-software-design-documents/scripts/render_puml.py "$d"; done`.
3. Check that every `.puml` now has a matching image, that no image still shows "12L" or men's sizes, and
   that the marketing-page README embeds its two diagrams. Remove the "needs re-rendering" note.

## Part 4: Green verify

1. Use Node 22.22.3 or later (the Angular CLI refuses older), .NET SDK 10.0.402 or later, and
   `npx playwright install chromium`.
2. Fix the intermittent failure in `src/frontend/e2e/tests/search-and-filter-catalog.spec.ts`,
   "under 768px Filters opens a full-screen sheet with a live count, Clear and apply". It fails about one
   run in three at `expect(counts.at(-1)?.getAll('condition')).toEqual(['new-with-tags'])`: the last
   count request seen is not always the one for the ticked condition. Find the root cause (debounce or
   request ordering in the filter sheet's live count) and fix the code or make the test wait for the
   right request. Do not add retries or sleeps. Run it 10 times with `--repeat-each 10` to prove it.
3. Run `npm run verify` in `src/frontend`: backend restore, format, build with warnings as errors, all
   backend tests, Prettier and ESLint, `build:all` and the full Playwright suite. Everything must pass.
   Also run `npm run build:marketing`.
4. Run the mock, design-system and contrast checkers again.

## Part 5: Wrap up

- `CHANGELOG.md`: entries for the wider catalogue, the finished video and the rendered diagrams; fix any
  counts that changed (mocks and screens).
- `docs/estimates/README.md` totals equal the sum of the subsystem tables.
- Grep for leftovers and fix product hits:
  `git grep -nIiP "men's|men’s|US 12|size L\b|12 and L|girls' sizing|Girls’ clothing\. Nothing else" -- . ':!docs/adr/general/0005-marketing-wording.md'`.
  References to 12L as the clone's origin, in AGENTS.md and the token comments, are fine.
- Commit in logical steps (ADR and requirements, mocks and design system, backend, frontend, seed data,
  video, diagrams). Push the branch and open a pull request that summarises each part and lists anything
  deferred.

## Deferred (do not build)

- Real sizes (2T to 16, shoe sizes) as a listing attribute and filter: the open question in the new ADR.
- Switching body and heading fonts to the brand kit's Fraunces and DM Sans: Giddy's type stays unless the
  owner decides otherwise.
- Using the brand kit character "Tilly Tutu" illustrations on the site.
- Production deployment: `.github/workflows/deploy.yml` stays manual-only until the owner creates the
  `tutumuch.kids` host and the `DEPLOY_SSH_KEY`, `DEPLOY_HOST` and `SITE_HOST` secrets
  (`docs/deployment.md`).

## Done means

- [ ] ADR general/0006 exists; L1/L2, journeys, designs, estimates, README, AGENTS.md and CONTRIBUTING
      describe the six categories and the "for girls" confirmation.
- [ ] `CategoryCatalog` holds the six categories and 26 subcategories, all `Girls'`; the API, sell form,
      catalog routes, nav and filters use them; the seed has 48 to 80 listings, at least 8 per category,
      with committed, attributed photos in six category folders.
- [ ] Mocks rebuilt and checked (0 errors); design-system and contrast checkers pass.
- [ ] The pitch video is narrated and has music; ATTRIBUTION, README, design and estimate no longer call
      it interim; the marketing Playwright tests pass.
- [ ] Every `.puml` has a rendered image with no "12L" text.
- [ ] The intermittent search-and-filter test passes 10 times in a row.
- [ ] `npm run verify` and `npm run build:marketing` pass with no failures.
- [ ] Branch pushed and pull request open, with deferred items listed.
