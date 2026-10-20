# Immagine Docker del backend di Rolayther, pensata per Render (vedi docs/DEPLOY.md).
# Due fasi: la prima compila (serve l'SDK, pesante), la seconda tiene solo il necessario per eseguire.

# ---- 1) Compilazione ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Prima solo il file di progetto: se non cambia, Docker riusa i pacchetti già scaricati e le build successive sono più veloci
COPY Rolayther.csproj ./
RUN dotnet restore Rolayther.csproj

COPY . ./
RUN dotnet publish Rolayther.csproj -c Release -o /app/publish --no-restore

# ---- 2) Immagine finale ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# Il programma gira con un utente normale, non come amministratore del container. Gli serve poter scrivere solo in queste
# due cartelle, e solo se per scelta si usano i file su disco (Storage__Provider=Local) o le email di prova
# (Email__Provider=Console): online non succede, ma così un errore di configurazione non blocca tutto il sito.
RUN mkdir -p /app/wwwroot/uploads /app/App_Data \
    && chown -R app:app /app/wwwroot/uploads /app/App_Data
USER app

# Online l'ambiente è sempre "Production" (niente Swagger, log solo sulla console di Render).
# Hosting__ClientIpHeader: dietro Render c'è Cloudflare, che scrive l'IP vero del visitatore in CF-Connecting-IP.
# Serve al limite di tentativi sul login, che altrimenti sarebbe condiviso da tutti (vedi Program.cs).
ENV ASPNETCORE_ENVIRONMENT=Production \
    Hosting__ClientIpHeader=CF-Connecting-IP

# Render dice su quale porta ascoltare con la variabile PORT (di solito 10000); senza, si usa 8080.
# "exec" fa sì che sia dotnet a ricevere lo stop di Render e a chiudersi con calma.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Rolayther.dll"]
