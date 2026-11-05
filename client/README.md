# Quiz Proctoring System: web app

React 19 + Vite + TypeScript. Runs on http://localhost:3001 and sends `/api` requests to the API on
http://localhost:3000 (see `vite.config.ts`).

```powershell
npm install
npm run dev      # development server
npm run build    # type check and production build into dist/
```

- `src/api`: HTTP client (JWT, error messages) and the API's response types
- `src/contexts`: signed-in user, selected semester and rule settings
- `src/components`: app shell, quiz card with seat strips, candidate picker, slot rail, timetable grid, forms
- `src/pages/admin` and `src/pages/ta`: one file per screen
- `src/lib/pdfCalendar.ts`: reads the university calendar PDF with pdf.js
- `src/styles/app.css`: the whole visual system (colour tokens at the top)
