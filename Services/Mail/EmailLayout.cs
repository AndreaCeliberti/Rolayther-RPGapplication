using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Rolayther.Services.Mail
{
    public sealed record RenderedEmail(string Subject, string Html, string Text);

    // Valori comuni a tutte le email: indirizzi del sito, se si può rispondere, validità del link di conferma
    public sealed record EmailContext(string FrontendUrl, string AssetsUrl, bool CanReply, int TokenDays)
    {
        public static EmailContext From(EmailOptions options) =>
            new(options.FrontendUrl, options.AssetsUrl, options.CanReply, options.ConfirmationTokenDays);

        public string Url(string path) => FrontendUrl + path;
    }

    // ---------------------------------------------------------------------------------------------
    // Modello del contenuto: ogni email è una sequenza di "blocchi". Lo stesso modello produce sia
    // l'HTML (tabelle e stili in linea, come richiedono i programmi di posta) sia il testo semplice.
    // ---------------------------------------------------------------------------------------------
    internal abstract record EmailBlock;

    internal sealed record Inline(string Text, bool Bold = false);

    internal sealed record ParagraphBlock(IReadOnlyList<Inline> Parts) : EmailBlock;

    internal sealed record DetailRow(string Label, string Value, string? Href = null);

    internal sealed record DetailsBlock(IReadOnlyList<DetailRow> Rows) : EmailBlock;

    // Citazione con etichetta (es. il messaggio dell'amministratore o il motivo del rifiuto)
    internal sealed record QuoteBlock(string Label, string Text, string Accent) : EmailBlock;

    // Riquadro di avviso evidenziato
    internal sealed record CalloutBlock(string Title, string Text, string Accent) : EmailBlock;

    internal sealed record StepsBlock(string Title, IReadOnlyList<string> Items, string Accent) : EmailBlock;

    internal sealed record ButtonBlock(string Label, string Url) : EmailBlock;

    // "Il pulsante non funziona? Copia questo indirizzo"
    internal sealed record LinkFallbackBlock(string Url) : EmailBlock;

    // Testo piccolo e discreto
    internal sealed record NoteBlock(IReadOnlyList<Inline> Parts) : EmailBlock;

    internal sealed record EmailContent(
        string Subject,
        string Preheader,
        string IconFile,       // file in public/email del frontend, es. "ico-confirm.png"
        string Accent,         // colore dell'icona, dell'etichetta e dei filetti (esadecimale)
        string Eyebrow,        // etichetta sopra al titolo
        string Title,
        IReadOnlyList<EmailBlock> Blocks,
        string FooterReason);  // "Ricevi questa email perché..."

    internal static class EmailTheme
    {
        public const string PageBg = "#0a0710";
        public const string CardBg = "#120d1f";
        public const string CardBorder = "#2a2040";
        public const string Panel = "#1a1330";
        public const string PanelBorder = "#2e2347";
        public const string Divider = "#2a2040";
        public const string TextStrong = "#f3f0ff";
        public const string TextBody = "#cfc7e6";
        public const string TextMuted = "#9d93b8";
        public const string TextFaint = "#6f6688";
        public const string Link = "#a78bfa";
        public const string Button = "#7c3aed";
        public const string ButtonBorder = "#6d28d9";

        public const string Sans = "Inter,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif";
        public const string Serif = "Georgia,'Times New Roman',Times,serif";

        // Colori d'accento per tipo di email (tutti molto luminosi: leggibili su sfondo scuro)
        public const string Violet = "#a78bfa";
        public const string Emerald = "#34d399";
        public const string Rose = "#fb7185";
        public const string Cyan = "#22d3ee";
        public const string Blue = "#60a5fa";
        public const string Gold = "#fbbf24";
    }

    internal static class EmailLayout
    {
        public static RenderedEmail Render(EmailContent content, EmailContext ctx) =>
            new(content.Subject, BuildHtml(content, ctx), BuildText(content, ctx));

        // Accetta solo indirizzi http(s) assoluti; tutto il resto (javascript:, data:, relativi...) viene scartato.
        // Restituisce l'indirizzo normalizzato (caratteri speciali già codificati) oppure null.
        public static string? SafeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var trimmed = url.Trim();
            if (trimmed.Length > 2000) return null;

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

            return uri.AbsoluteUri;
        }

        private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        // Testo con ritorni a capo -> HTML (dopo aver codificato i caratteri speciali)
        private static string EMultiline(string? value) =>
            Regex.Replace(E(value), @"\r\n|\r|\n", "<br>");

        // =========================================================================================
        // HTML
        // =========================================================================================
        private const string HeadStyles = @"
body,table,td,a{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;}
table,td{mso-table-lspace:0pt;mso-table-rspace:0pt;}
img{-ms-interpolation-mode:bicubic;border:0;outline:none;text-decoration:none;}
body{margin:0!important;padding:0!important;width:100%!important;background-color:#0a0710;}
a{color:#a78bfa;}
@media only screen and (max-width:620px){
  .container{width:100%!important;max-width:100%!important;}
  .outer{padding:0!important;}
  .card{border-radius:0!important;border-left-width:0!important;border-right-width:0!important;}
  .px{padding-left:22px!important;padding-right:22px!important;}
  .h1{font-size:26px!important;line-height:33px!important;}
  .hero-icon{width:84px!important;height:84px!important;}
  .detail-label{width:36%!important;}
}
";

        private static string BuildHtml(EmailContent c, EmailContext ctx)
        {
            var assets = ctx.AssetsUrl + "/email/";
            var sb = new StringBuilder(12_000);

            sb.Append("<!DOCTYPE html>\n");
            sb.Append("<html lang=\"it\" xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\">\n<head>\n");
            sb.Append("<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\n");
            sb.Append("<meta name=\"color-scheme\" content=\"dark\">\n");
            sb.Append("<meta name=\"supported-color-schemes\" content=\"dark\">\n");
            sb.Append("<meta name=\"format-detection\" content=\"telephone=no, date=no, address=no, email=no, url=no\">\n");
            sb.Append("<title>").Append(E(c.Subject)).Append("</title>\n");
            sb.Append("<!--[if mso]><noscript><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml></noscript><![endif]-->\n");
            sb.Append("<style type=\"text/css\">").Append(HeadStyles).Append("</style>\n");
            sb.Append("</head>\n");

            sb.Append($"<body bgcolor=\"{EmailTheme.PageBg}\" style=\"margin:0;padding:0;background-color:{EmailTheme.PageBg};\">\n");

            // Anteprima mostrata nella lista della posta (nascosta nel corpo); i caratteri invisibili in coda
            // evitano che il client ci accodi pezzi del testo vero e proprio
            sb.Append("<div style=\"display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;\">")
              .Append(E(c.Preheader))
              .Append(string.Concat(Enumerable.Repeat("&#8204;&nbsp;", 90)))
              .Append("</div>\n");

            sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{EmailTheme.PageBg}\" style=\"background-color:{EmailTheme.PageBg};\">\n");
            sb.Append("<tr><td align=\"center\" class=\"outer\" style=\"padding:28px 12px;\">\n");

            // ----- scheda centrale
            sb.Append($"<table role=\"presentation\" class=\"container card\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{EmailTheme.CardBg}\" ")
              .Append($"style=\"width:600px;max-width:600px;background-color:{EmailTheme.CardBg};border:1px solid {EmailTheme.CardBorder};border-radius:18px;overflow:hidden;\">\n");

            // banner con il logo
            sb.Append("<tr><td style=\"padding:0;line-height:0;font-size:0;\">")
              .Append($"<img src=\"{E(assets)}header.jpg\" width=\"600\" height=\"160\" alt=\"ROLAYTHER\" ")
              .Append($"style=\"display:block;width:100%;max-width:600px;height:auto;border:0;font-family:{EmailTheme.Serif};font-size:30px;line-height:60px;letter-spacing:8px;color:#d9d4ea;text-align:center;background-color:{EmailTheme.CardBg};\">")
              .Append("</td></tr>\n");

            // icona, etichetta e titolo
            sb.Append("<tr><td align=\"center\" class=\"px\" style=\"padding:4px 44px 0 44px;\">\n");
            sb.Append($"<img class=\"hero-icon\" src=\"{E(assets)}{E(c.IconFile)}\" width=\"96\" height=\"96\" alt=\"\" style=\"display:block;margin:0 auto 16px auto;width:96px;height:96px;border:0;\">\n");
            sb.Append($"<div style=\"font-family:{EmailTheme.Sans};font-size:12px;line-height:16px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:{c.Accent};\">{E(c.Eyebrow)}</div>\n");
            sb.Append($"<h1 class=\"h1\" style=\"margin:10px 0 0 0;font-family:{EmailTheme.Serif};font-size:30px;line-height:38px;font-weight:700;color:{EmailTheme.TextStrong};\">{E(c.Title)}</h1>\n");
            sb.Append($"<table role=\"presentation\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:18px auto 0 auto;\"><tr><td width=\"56\" height=\"3\" bgcolor=\"{c.Accent}\" style=\"width:56px;height:3px;font-size:0;line-height:0;background-color:{c.Accent};border-radius:2px;\">&nbsp;</td></tr></table>\n");
            sb.Append("</td></tr>\n");

            // contenuto
            sb.Append("<tr><td class=\"px\" style=\"padding:26px 44px 6px 44px;\">\n");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">\n");
            foreach (var block in c.Blocks)
                AppendBlockHtml(sb, block);
            sb.Append("</table>\n</td></tr>\n");

            // filetto
            sb.Append("<tr><td class=\"px\" style=\"padding:10px 44px 0 44px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>")
              .Append($"<td height=\"1\" style=\"height:1px;font-size:0;line-height:0;border-top:1px solid {EmailTheme.Divider};\">&nbsp;</td></tr></table></td></tr>\n");

            // piè di pagina
            sb.Append("<tr><td align=\"center\" class=\"px\" style=\"padding:26px 44px 34px 44px;\">\n");
            sb.Append($"<img src=\"{E(assets)}emblem.png\" width=\"56\" height=\"56\" alt=\"Rolayther\" style=\"display:block;margin:0 auto 12px auto;width:56px;height:56px;border:0;border-radius:12px;\">\n");
            sb.Append($"<div style=\"font-family:{EmailTheme.Serif};font-size:15px;line-height:20px;letter-spacing:4px;text-transform:uppercase;color:{EmailTheme.TextBody};\">Rolayther</div>\n");
            sb.Append($"<div style=\"padding-top:6px;font-family:{EmailTheme.Sans};font-size:13px;line-height:20px;color:{EmailTheme.TextMuted};\">Sessioni di gioco di ruolo, Master e giocatori in un solo posto.</div>\n");
            sb.Append($"<div style=\"padding-top:14px;font-family:{EmailTheme.Sans};font-size:13px;line-height:20px;color:{EmailTheme.TextMuted};\">")
              .Append($"<a href=\"{E(ctx.FrontendUrl)}/\" style=\"color:{EmailTheme.Link};text-decoration:none;\">Il sito</a>")
              .Append($" &nbsp;&middot;&nbsp; <a href=\"{E(ctx.FrontendUrl)}/sessions\" style=\"color:{EmailTheme.Link};text-decoration:none;\">Sessioni</a>")
              .Append($" &nbsp;&middot;&nbsp; <a href=\"{E(ctx.FrontendUrl)}/masters\" style=\"color:{EmailTheme.Link};text-decoration:none;\">Master</a></div>\n");
            sb.Append($"<div style=\"padding-top:16px;font-family:{EmailTheme.Sans};font-size:12px;line-height:18px;color:{EmailTheme.TextFaint};\">{E(c.FooterReason)}</div>\n");
            sb.Append("</td></tr>\n");

            sb.Append("</table>\n");

            // sotto la scheda
            sb.Append($"<table role=\"presentation\" class=\"container\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:600px;max-width:600px;\"><tr><td align=\"center\" style=\"padding:16px 24px 8px 24px;font-family:{EmailTheme.Sans};font-size:12px;line-height:18px;color:{EmailTheme.TextFaint};\">")
              .Append("Questa è un'email automatica di Rolayther.</td></tr></table>\n");

            sb.Append("</td></tr></table>\n</body>\n</html>\n");
            return sb.ToString();
        }

        private static void AppendBlockHtml(StringBuilder sb, EmailBlock block)
        {
            switch (block)
            {
                case ParagraphBlock p:
                    sb.Append($"<tr><td style=\"padding:0 0 16px 0;font-family:{EmailTheme.Sans};font-size:16px;line-height:26px;color:{EmailTheme.TextBody};\">")
                      .Append(InlineHtml(p.Parts)).Append("</td></tr>\n");
                    break;

                case NoteBlock n:
                    sb.Append($"<tr><td style=\"padding:0 0 16px 0;font-family:{EmailTheme.Sans};font-size:13px;line-height:21px;color:{EmailTheme.TextMuted};\">")
                      .Append(InlineHtml(n.Parts)).Append("</td></tr>\n");
                    break;

                case DetailsBlock d:
                    AppendDetails(sb, d);
                    break;

                case QuoteBlock q:
                    sb.Append("<tr><td style=\"padding:2px 0 20px 0;\">")
                      .Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>")
                      .Append($"<td width=\"4\" bgcolor=\"{q.Accent}\" style=\"width:4px;background-color:{q.Accent};font-size:0;line-height:0;\">&nbsp;</td>")
                      .Append($"<td bgcolor=\"{EmailTheme.Panel}\" style=\"background-color:{EmailTheme.Panel};padding:16px 20px;border-radius:0 12px 12px 0;\">")
                      .Append($"<div style=\"font-family:{EmailTheme.Sans};font-size:11px;line-height:16px;font-weight:700;letter-spacing:2px;text-transform:uppercase;color:{q.Accent};\">{E(q.Label)}</div>")
                      .Append($"<div style=\"padding-top:8px;font-family:{EmailTheme.Sans};font-size:16px;line-height:25px;color:{EmailTheme.TextStrong};\">{EMultiline(q.Text)}</div>")
                      .Append("</td></tr></table></td></tr>\n");
                    break;

                case CalloutBlock co:
                    sb.Append("<tr><td style=\"padding:2px 0 20px 0;\">")
                      .Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{EmailTheme.Panel}\" style=\"background-color:{EmailTheme.Panel};border:1px solid {co.Accent};border-radius:12px;\"><tr>")
                      .Append("<td style=\"padding:16px 20px;\">")
                      .Append($"<div style=\"font-family:{EmailTheme.Sans};font-size:15px;line-height:22px;font-weight:700;color:{co.Accent};\">{E(co.Title)}</div>")
                      .Append($"<div style=\"padding-top:6px;font-family:{EmailTheme.Sans};font-size:14px;line-height:22px;color:{EmailTheme.TextBody};\">{EMultiline(co.Text)}</div>")
                      .Append("</td></tr></table></td></tr>\n");
                    break;

                case StepsBlock s:
                    AppendSteps(sb, s);
                    break;

                case ButtonBlock b:
                    AppendButton(sb, b);
                    break;

                case LinkFallbackBlock l:
                    var url = SafeUrl(l.Url);
                    if (url != null)
                    {
                        sb.Append($"<tr><td align=\"center\" style=\"padding:0 0 22px 0;font-family:{EmailTheme.Sans};font-size:12px;line-height:19px;color:{EmailTheme.TextMuted};\">")
                          .Append("Il pulsante non funziona? Copia e incolla questo indirizzo nel browser:<br>")
                          .Append($"<a href=\"{E(url)}\" style=\"color:{EmailTheme.Link};word-break:break-all;text-decoration:underline;\">{E(url)}</a>")
                          .Append("</td></tr>\n");
                    }
                    break;
            }
        }

        private static void AppendDetails(StringBuilder sb, DetailsBlock d)
        {
            sb.Append("<tr><td style=\"padding:2px 0 22px 0;\">");
            sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{EmailTheme.Panel}\" style=\"background-color:{EmailTheme.Panel};border:1px solid {EmailTheme.PanelBorder};border-radius:14px;\">");
            sb.Append("<tr><td style=\"padding:6px 20px;\">");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");

            for (var i = 0; i < d.Rows.Count; i++)
            {
                var row = d.Rows[i];
                var border = i == 0 ? string.Empty : $"border-top:1px solid {EmailTheme.Divider};";
                var href = SafeUrl(row.Href);

                sb.Append("<tr>");
                sb.Append($"<td class=\"detail-label\" width=\"34%\" valign=\"top\" style=\"width:34%;padding:12px 12px 12px 0;{border}font-family:{EmailTheme.Sans};font-size:12px;line-height:20px;letter-spacing:1px;text-transform:uppercase;color:{EmailTheme.TextMuted};\">{E(row.Label)}</td>");
                sb.Append($"<td valign=\"top\" style=\"padding:12px 0;{border}font-family:{EmailTheme.Sans};font-size:15px;line-height:22px;font-weight:600;color:{EmailTheme.TextStrong};word-break:break-word;\">");
                if (href != null)
                    sb.Append($"<a href=\"{E(href)}\" style=\"color:{EmailTheme.Link};text-decoration:underline;word-break:break-all;\">{E(row.Value)}</a>");
                else
                    sb.Append(E(row.Value));
                sb.Append("</td></tr>");
            }

            sb.Append("</table></td></tr></table></td></tr>\n");
        }

        private static void AppendSteps(StringBuilder sb, StepsBlock s)
        {
            sb.Append("<tr><td style=\"padding:2px 0 14px 0;\">");
            sb.Append($"<div style=\"padding-bottom:12px;font-family:{EmailTheme.Sans};font-size:12px;line-height:16px;font-weight:700;letter-spacing:2px;text-transform:uppercase;color:{s.Accent};\">{E(s.Title)}</div>");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");

            for (var i = 0; i < s.Items.Count; i++)
            {
                sb.Append("<tr>");
                sb.Append($"<td width=\"40\" valign=\"top\" style=\"width:40px;padding:0 0 12px 0;\">")
                  .Append($"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr><td width=\"28\" height=\"28\" align=\"center\" valign=\"middle\" bgcolor=\"{s.Accent}\" ")
                  .Append($"style=\"width:28px;height:28px;background-color:{s.Accent};border-radius:14px;font-family:{EmailTheme.Sans};font-size:13px;line-height:28px;font-weight:800;color:#0b0712;text-align:center;\">{i + 1}</td></tr></table></td>");
                sb.Append($"<td valign=\"top\" style=\"padding:3px 0 12px 0;font-family:{EmailTheme.Sans};font-size:15px;line-height:23px;color:{EmailTheme.TextBody};\">{E(s.Items[i])}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table></td></tr>\n");
        }

        // Pulsante "a prova di Outlook": in Outlook per Windows (motore di Word) si usa una forma VML,
        // in tutti gli altri programmi un normale collegamento con sfondo e angoli arrotondati.
        private static void AppendButton(StringBuilder sb, ButtonBlock b)
        {
            var url = SafeUrl(b.Url);
            if (url == null) return;

            var label = E(b.Label);
            var href = E(url);
            var width = Math.Clamp(b.Label.Length * 10 + 72, 220, 440);

            sb.Append("<tr><td align=\"center\" style=\"padding:8px 0 22px 0;\">");
            sb.Append("<table role=\"presentation\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>");
            sb.Append($"<td align=\"center\" bgcolor=\"{EmailTheme.Button}\" style=\"background-color:{EmailTheme.Button};background-image:linear-gradient(135deg,#8b5cf6,#6d28d9);border:1px solid {EmailTheme.ButtonBorder};border-radius:12px;\">");
            sb.Append($"<!--[if mso]><v:roundrect xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:w=\"urn:schemas-microsoft-com:office:word\" href=\"{href}\" style=\"height:50px;v-text-anchor:middle;width:{width}px;\" arcsize=\"22%\" strokecolor=\"{EmailTheme.ButtonBorder}\" fillcolor=\"{EmailTheme.Button}\"><w:anchorlock/><center style=\"color:#ffffff;font-family:Arial,sans-serif;font-size:16px;font-weight:bold;\">{label}</center></v:roundrect><![endif]-->");
            sb.Append("<!--[if !mso]><!-- -->");
            sb.Append($"<a href=\"{href}\" target=\"_blank\" style=\"display:inline-block;padding:15px 36px;font-family:{EmailTheme.Sans};font-size:16px;line-height:20px;font-weight:700;color:#ffffff;text-decoration:none;border-radius:12px;\">{label}</a>");
            sb.Append("<!--<![endif]-->");
            sb.Append("</td></tr></table></td></tr>\n");
        }

        private static string InlineHtml(IReadOnlyList<Inline> parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Bold)
                    sb.Append($"<strong style=\"color:{EmailTheme.TextStrong};font-weight:700;\">{E(part.Text)}</strong>");
                else
                    sb.Append(E(part.Text));
            }
            return sb.ToString();
        }

        // =========================================================================================
        // Testo semplice (alternativa per i programmi che non mostrano l'HTML, e per i filtri antispam)
        // =========================================================================================
        private static string BuildText(EmailContent c, EmailContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ROLAYTHER");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine();
            sb.AppendLine(c.Eyebrow.ToUpperInvariant());
            sb.AppendLine(c.Title);
            sb.AppendLine();

            foreach (var block in c.Blocks)
            {
                switch (block)
                {
                    case ParagraphBlock p:
                        sb.AppendLine(InlineText(p.Parts)).AppendLine();
                        break;

                    case NoteBlock n:
                        sb.AppendLine(InlineText(n.Parts)).AppendLine();
                        break;

                    case DetailsBlock d:
                        foreach (var row in d.Rows)
                        {
                            sb.Append("  ").Append(row.Label).Append(": ").Append(row.Value);
                            var href = SafeUrl(row.Href);
                            if (href != null && !string.Equals(href, row.Value, StringComparison.OrdinalIgnoreCase))
                                sb.Append(" (").Append(href).Append(')');
                            sb.AppendLine();
                        }
                        sb.AppendLine();
                        break;

                    case QuoteBlock q:
                        sb.AppendLine(q.Label.ToUpperInvariant() + ":");
                        foreach (var line in q.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
                            sb.Append("> ").AppendLine(line);
                        sb.AppendLine();
                        break;

                    case CalloutBlock co:
                        sb.AppendLine("! " + co.Title);
                        sb.AppendLine("  " + co.Text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\n  "));
                        sb.AppendLine();
                        break;

                    case StepsBlock s:
                        sb.AppendLine(s.Title.ToUpperInvariant() + ":");
                        for (var i = 0; i < s.Items.Count; i++)
                            sb.Append("  ").Append(i + 1).Append(". ").AppendLine(s.Items[i]);
                        sb.AppendLine();
                        break;

                    case ButtonBlock b:
                        var url = SafeUrl(b.Url);
                        if (url != null)
                        {
                            sb.AppendLine(b.Label + ":");
                            sb.AppendLine(url);
                            sb.AppendLine();
                        }
                        break;

                    case LinkFallbackBlock:
                        // già incluso sopra, con il pulsante
                        break;
                }
            }

            sb.AppendLine("--");
            sb.AppendLine(c.FooterReason);
            sb.AppendLine("Rolayther - " + ctx.FrontendUrl);
            return sb.ToString();
        }

        private static string InlineText(IReadOnlyList<Inline> parts) =>
            string.Concat(parts.Select(p => p.Text));
    }
}
