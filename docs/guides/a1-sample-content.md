# English A1 sample content

Issue #19 includes a published English A1 course with one lesson and ten vocabulary words. The lesson viewer displays the words and can pronounce them through the browser's speech synthesis when no audio file is provided.

The sample is opt-in so the API can start in Development without a database connection. After configuring MySQL and applying migrations, set `SampleContent__SeedA1=true` in the API process environment and start the API in Development. The seeder runs once per startup, reuses an existing English language, and skips words already present. The course appears in `/courses` with level `A1`. Its lesson ID is `a1000000-0000-4000-8000-000000000020`.

The seeder activates the existing English language if it was inactive. Do not enable this setting in an environment where that change is unwanted.
