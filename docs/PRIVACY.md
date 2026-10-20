# Privacy e consenso alla registrazione

Chi si iscrive a Rolayther può leggere l'informativa sulla privacy e deve dare il consenso al trattamento dei dati
personali. Il sistema registra **quando** l'ha dato e **quale versione** del documento aveva davanti.

## Come funziona

1. Nelle finestre di registrazione (Player e richiesta Master) c'è una casella obbligatoria: «Ho letto l'informativa
   sulla privacy e acconsento al trattamento dei miei dati personali». Il link apre la pagina `/privacy` del sito in
   una nuova scheda, così il modulo già compilato non si perde. La stessa pagina è raggiungibile dal piè di pagina.
2. Il sito invia `privacyConsent: true` e `privacyPolicyVersion` (la versione mostrata) insieme ai dati di registrazione.
3. Il backend **rifiuta la registrazione senza consenso** (`400`, codice `PrivacyConsentRequired`), anche se qualcuno
   salta la casella del sito e chiama l'API direttamente.
4. Con il consenso, sull'account (tabella `AspNetUsers`) si salvano `PrivacyConsentAt` (data e ora in UTC) e
   `PrivacyPolicyVersion`.

**Quando il consenso resta vuoto (`NULL`):**

- account creati da un Admin dalla dashboard: il consenso può darlo solo l'interessato, quindi l'Admin non lo dà al
  suo posto (se la richiesta lo porta comunque, il server lo ignora);
- account iscritti prima che la casella esistesse.

## Cambiare il testo dell'informativa

Il documento sta nel frontend: la pagina è `src/pages/PrivacyPolicy.jsx` e i dati che solo il titolare conosce
(nome, email di contatto, tempi di conservazione...) stanno in `src/config/privacy.js`. Finché mancano, la pagina
si presenta come bozza.

Quando il testo cambia in modo che conti per chi si iscrive, aggiorna `PRIVACY_POLICY_VERSION` in quel file
(formato data `AAAA-MM-GG`, al massimo 20 caratteri): da quel momento le nuove iscrizioni registrano la versione nuova.
Chi si è iscritto prima resta registrato con la versione che aveva visto.

## Vedere chi ha acconsentito

```sql
-- consensi registrati, dal più recente
SELECT Email, PrivacyConsentAt, PrivacyPolicyVersion
FROM AspNetUsers
WHERE PrivacyConsentAt IS NOT NULL
ORDER BY PrivacyConsentAt DESC;

-- account senza consenso registrato
SELECT Email, CreatedAt FROM AspNetUsers WHERE PrivacyConsentAt IS NULL;
```

## Aggiornare il database

La migrazione `AddPrivacyConsent` aggiunge le due colonne a `AspNetUsers`. In sviluppo si applica da sola
all'avvio del backend. Su un database online **non** parte da sola: esegui `dotnet ef database update` (applica tutte
le migrazioni che mancano, comprese quelle precedenti come `AddMasterApprovalStatus`) oppure, senza strumenti, questo
script, che va bene solo se le migrazioni precedenti sono già state applicate:

```sql
ALTER TABLE [AspNetUsers] ADD [PrivacyConsentAt] datetime2 NULL;
ALTER TABLE [AspNetUsers] ADD [PrivacyPolicyVersion] nvarchar(20) NULL;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261020083000_AddPrivacyConsent', N'8.0.22');
```

## Ordine di pubblicazione

Metti online **prima il frontend, poi il backend**. Il frontend nuovo funziona anche con il backend vecchio (che ignora
i campi in più). Il backend nuovo, invece, rifiuta le registrazioni che non portano il consenso: con il frontend
vecchio nessuno riuscirebbe a registrarsi finché non viene pubblicato anche quello.

## Cosa non fa (ancora)

- Non chiede il consenso agli utenti già iscritti né a quelli creati da un Admin (per esempio al primo accesso).
- Se l'informativa cambia, non chiede di nuovo il consenso a chi si era già iscritto.
- Non controlla l'età: la data di nascita viene raccolta, ma nessuna soglia minima blocca la registrazione.
- Il ritiro del consenso e la cancellazione dei dati si chiedono al contatto indicato nell'informativa; non c'è ancora
  una funzione sul sito.

## Dove sta il codice

| Cosa | Dove |
|---|---|
| Controllo del consenso (e eccezione per l'Admin) | `Controllers/AuthController.cs` (`CheckPrivacyConsent`) |
| Dati della richiesta di registrazione | `Models/DTOs/Request/RegisterPlayerRequestDto.cs`, `RegisterMasterRequestDto.cs`, `IPrivacyConsentRequest.cs` |
| Salvataggio di data e versione | `Services/AuthService.cs` (`ApplyPrivacyConsent`) |
| Colonne sull'account | `Models/Entities/ApplicationUser.cs`, migrazione `Migrations/20261020083000_AddPrivacyConsent.cs` |
| Casella, pagina e testo dell'informativa (frontend) | `src/components/auth/PrivacyConsentCheck.jsx`, `src/pages/PrivacyPolicy.jsx`, `src/config/privacy.js` |
