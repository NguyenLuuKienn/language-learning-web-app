# Media uploads

Admin users can send a `multipart/form-data` request to `POST /api/v1/media` with a `file` field. The response contains a URL for `GET /api/v1/media/{id}` that can be stored in a vocabulary item's `audioUrl` or `imageUrl`.

Allowed types are PNG, JPEG, GIF and WebP images (up to 5 MiB), and MP3 and WAV audio (up to 10 MiB). The server checks the declared MIME type, actual byte count and file signature, then assigns its own GUID filename. SVG, HTML, executables and other formats are rejected. The download endpoint sends the validated MIME type and `X-Content-Type-Options: nosniff`.

Files default to `storage/uploads` relative to the API working directory. Set `Media:StoragePath` to a persistent directory in deployment, and back up that directory separately from MySQL. Uploads are public to anyone with their URL, so do not upload private content.
When deployed behind a reverse proxy, set `Media:PublicBaseUrl` to the public API origin (including any path prefix) so upload responses contain usable URLs.
