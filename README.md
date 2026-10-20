# Rolayther

Rolayther è una web app con backend ASP.NET Core (Identity + JWT) e frontend React (Vite + React-Bootstrap).  
Il progetto gestisce sessioni di gioco, profili Player/Master e una dashboard Admin con operazioni CRUD.

## Stack
- Backend: ASP.NET Core, Entity Framework Core, ASP.NET Identity, JWT Bearer, Swagger
- Database: PostgreSQL (online su Supabase)
- File caricati: Supabase Storage (in sviluppo, su disco)
- Frontend: React (Vite), Axios, React-Bootstrap

## Requisiti
- .NET SDK 10 (con Visual Studio 2026; altrimenti da [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download))
- PostgreSQL (oppure un progetto gratuito su Supabase: vedi docs/DEPLOY.md)
- Node.js LTS

## Struttura repository 
Branches:
- `master/` (backend)
- `main/` (frontend)

## Documentazione
- [Mettere il sito online (gratis)](docs/DEPLOY.md): Netlify, Render, Supabase, Brevo
- [Email](docs/EMAIL.md): conferma registrazione, notifiche, Brevo
- [Privacy e consenso](docs/PRIVACY.md)
