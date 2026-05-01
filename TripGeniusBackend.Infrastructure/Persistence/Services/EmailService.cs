using Resend;
using TripGeniusBackend.Application.Interfaces;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;


public class EmailService : IEmailService
{
    private readonly IResend _resend;
    
    public EmailService(IResend resend)
    {
        _resend = resend;
    }

  public async Task SendEmailAsync(string to, string subject, string content, string actionUrl, string actionLabel)
  {
      var html = $@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
      <html xmlns=""http://www.w3.org/1999/xhtml"">
      <head>
      <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
      <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
      </head>
      <body style=""margin:0;padding:0;background-color:#0c110d;"">
      <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color:#0c110d;"">
        <tr>
          <td align=""center"" style=""padding:32px 12px;"">
            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""580"" style=""background-color:#111a12;border:1px solid #2a4029;border-radius:16px;"">

              <!-- Header -->
              <tr>
                <td align=""center"" style=""padding:32px 40px 24px 40px;background-color:#0e160f;border-radius:16px 16px 0 0;border-bottom:1px solid #1e3020;"">
                  <div style=""display:inline-block;background-color:#1a6013;padding:8px 20px;border-radius:100px;"">
                    <span style=""color:#f3fff1;font-family:Georgia,serif;font-size:20px;font-weight:bold;letter-spacing:1px;"">✦ TripGenius</span>
                  </div>
                </td>
              </tr>

              <!-- Icon -->
              <tr>
                <td align=""center"" style=""padding:36px 40px 0 40px;"">
                  <table border=""0"" cellpadding=""0"" cellspacing=""0"">
                    <tr>
                      <td align=""center"" style=""width:64px;height:64px;background-color:#1a3d1c;border:2px solid #2d6b30;border-radius:50%;"">
                        <span style=""font-size:28px;line-height:64px;display:block;"">🌿</span>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>

              <!-- Title -->
              <tr>
                <td align=""center"" style=""padding:20px 50px 8px 50px;"">
                  <h1 style=""margin:0;color:#f3fff1;font-family:Georgia,serif;font-size:26px;font-weight:bold;line-height:1.3;"">{subject}</h1>
                </td>
              </tr>

              <!-- Body -->
              <tr>
                <td align=""center"" style=""padding:12px 50px 32px 50px;"">
                  <p style=""margin:0;color:#8aad89;font-family:Helvetica,Arial,sans-serif;font-size:15px;line-height:1.7;text-align:center;"">{content}</p>
                </td>
              </tr>

              <!-- CTA Button -->
              <tr>
                <td align=""center"" style=""padding:0 50px 36px 50px;"">
                  <table border=""0"" cellpadding=""0"" cellspacing=""0"">
                    <tr>
                      <td align=""center"" bgcolor=""#1a6013"" style=""border-radius:100px;border:1px solid #4a9e45;"">
                        <a href=""{actionUrl}""
                           target=""_blank""
                           style=""display:inline-block;padding:14px 40px;color:#ffffff;text-decoration:none;font-family:Helvetica,Arial,sans-serif;font-size:15px;font-weight:bold;letter-spacing:0.5px;border-radius:100px;"">
                          {actionLabel}
                        </a>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>

              <!-- Divider -->
              <tr>
                <td style=""padding:0 40px;"">
                  <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr><td style=""border-top:1px solid #1e3020;font-size:1px;line-height:1px;"">&nbsp;</td></tr>
                  </table>
                </td>
              </tr>

              <!-- Fallback URL -->
              <tr>
                <td align=""center"" style=""padding:24px 50px;"">
                  <p style=""margin:0 0 6px 0;color:#4d6b4c;font-family:Helvetica,Arial,sans-serif;font-size:12px;"">If the button doesn't work, copy this link:</p>
                  <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:11px;"">
                    <a href=""{actionUrl}"" style=""color:#4a9e45;word-break:break-all;text-decoration:underline;"">{actionUrl}</a>
                  </p>
                </td>
              </tr>

              <!-- Footer -->
              <tr>
                <td align=""center"" style=""padding:20px 40px 28px 40px;background-color:#0c110d;border-radius:0 0 16px 16px;border-top:1px solid #1e3020;"">
                  <p style=""margin:0 0 6px 0;color:#3d5c3c;font-family:Helvetica,Arial,sans-serif;font-size:11px;"">© 2026 TripGenius. All rights reserved.</p>
                  <p style=""margin:0;font-family:Helvetica,Arial,sans-serif;font-size:11px;"">
                    <a href=""#"" style=""color:#4d6b4c;text-decoration:underline;"">Unsubscribe</a>
                    <span style=""color:#3d5c3c;""> &nbsp;·&nbsp; </span>
                    <a href=""#"" style=""color:#4d6b4c;text-decoration:underline;"">Privacy Policy</a>
                  </p>
                </td>
              </tr>

            </table>
          </td>
        </tr>
      </table>
      </body>
      </html>";

      var message = new EmailMessage();
      message.From = "contact@tripgenius.online";
      message.To.Add(to);
      message.Subject = subject;
      message.HtmlBody = html;

      var result = await _resend.EmailSendAsync(message);
      Console.WriteLine(result.Success);
  }
}