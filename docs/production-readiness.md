# Production readiness

Updated: 2026-10-24. This is a release gate based on the current repository, not a claim that every runtime path has been tested.

## Blockers before launch

- [ ] **Database upgrades:** Replace startup `EnsureCreated()` plus ad hoc `LegacySchemaUpgrade` calls with one versioned migration path. Apply it to a copy of the existing MySQL database, verify row counts and foreign keys, and rehearse backup and rollback. The new vocabulary and card detail columns have only been verified by build and SQLite integration tests.
- [ ] **Existing learning data:** Migrate existing vocabulary and deck records without recreating cards or changing IDs used by progress and SRS. Re-importing an entire course is not a safe data migration. Check that course material JSON, vocabulary rows, deck cards and learner progress agree after the upgrade.
- [ ] **Content quality:** Source and review pronunciation, part of speech and additional senses before claiming full dictionary coverage. In the A2 JSON, 767 of 768 lesson cards lack phonetics, 767 use the generic `vocabulary` label, and none has more than one verified meaning. The normalized `meanings` arrays currently contain the existing translation. Keep provenance and usage rights for imported dictionary data.
- [ ] **Dictionary identity:** Establish canonical entries and senses independent of lessons. The current dictionary searches lesson vocabulary and groups duplicates in the popup; identical words still occupy separate DB rows. Make saving a dictionary word to SRS idempotent on the server and test concurrent saves. Add indexes and measure search latency on a realistic corpus; the current substring query calls `ToLower()` on DB columns.
- [ ] **Release flow:** Run browser-to-API-to-DB checks against a production-like MySQL instance for login/logout, learner level access, course import and lesson edit, dictionary search, deck/SRS, placement review, audio upload/playback, writing review and progress. Include empty, malformed and oversized JSON and permission bypass attempts. Record the actual results before launch.
- [ ] **Certificate and interview content:** Have qualified editors author and review IELTS/TOEIC practice banks and profession-specific interview rubrics, including answer keys and audio rights. The JSON examples are demonstrations, not validated exam banks. Check timed submission, recording playback, manual grading and archived attempts on a production-like MySQL database. Do not show official IELTS bands or scaled TOEIC scores until each exam form has a verified conversion rule and score interpretation.
- [ ] **Secrets and public transport:** Set production JWT and admin bootstrap secrets outside the repository, rotate any deployed sample values, configure HTTPS at the public reverse proxy, and verify secure cookie, CSRF, CORS and proxy-header behavior on the real domain. Check the existing production database for demo accounts.
- [ ] **Operations:** Add a readiness endpoint that checks DB connectivity, structured error logging and alerts for 5xx, slow queries, upload failures and migration failures. Verify automated backups and a restore drill. Set storage retention and size limits for recordings and uploads.

## Decide before enabling the feature

- [ ] **Google sign-in:** Configure the OAuth Web Client ID and domain, implement and test server-side ID-token verification and account linking. Credentials are not configured yet, so do not advertise Google login as available.
- [ ] **Automated speaking/writing grading:** Current placement speaking and writing rely on review. If automatic grading is required, choose a provider, validate its accuracy for each language and level, document human appeal, and add consent/retention rules for recordings and submitted writing.
- [ ] **Dictionary coverage:** Decide whether the product promises a general dictionary or only course vocabulary. A general dictionary needs licensed source data, a sense-level model, pronunciation variants, examples and editorial tooling.
- [ ] **Excel import:** JSON import is present. Excel curriculum import requires a defined workbook schema and must pass the same backend validation and transactional save path before it can be advertised.

## Final release checks

- [ ] Measure representative page and API latency with production-sized courses, decks and media. Check pagination, query plans and N+1 behavior; set performance budgets.
- [ ] Verify all supported language and role combinations in the UI, including validation messages, loading/empty/error states, keyboard operation and mobile layout.
- [ ] Verify public landing-page metadata and crawlability; keep authenticated learning and admin routes out of search indexing.
- [ ] Run release builds, automated tests and a smoke test after deployment; document the deployment and rollback owner.
