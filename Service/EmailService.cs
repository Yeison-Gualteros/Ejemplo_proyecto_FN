using Contracts;
using Microsoft.Extensions.Options;
using Service.Contracts;
using Shared;
using System.Net;
using System.Net.Mail;

namespace Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILoggerManager _logger;

        public EmailService(IOptions<EmailSettings> options, ILoggerManager logger)
        {
            _settings = options.Value;
            _logger = logger;

            _logger.LogInfo($"🔧 EmailService configurado:");
            _logger.LogInfo($"   SmtpServer: {_settings.SmtpServer}");
            _logger.LogInfo($"   Port: {_settings.Port}");
            _logger.LogInfo($"   SenderEmail: {_settings.SenderEmail}");
            _logger.LogInfo($"   Username: {_settings.Username}");
            _logger.LogInfo($"   Password configurado: {!string.IsNullOrEmpty(_settings.Password)}");
            _logger.LogInfo($"   EnableSsl: {_settings.EnableSsl}");
        }

        public async Task SendPasswordAsync(string toEmail, string username, string password)
        {
            var bodyHtml = $@"
    <html>
    <body style='font-family: Arial, Helvetica, sans-serif; background-color: #f4f6f8; padding: 20px;'>
        <table width='100%' cellpadding='0' cellspacing='0'>
            <tr>
                <td align='center'>
                    <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 6px; padding: 20px;'>
                        <tr>
                            <td style='text-align: center; padding-bottom: 20px;'>
                                <h2 style='color: #2c3e50; margin: 0;'>Acceso a FileNova</h2>
                            </td>
                        </tr>
                        <tr>
                            <td style='color: #333333; font-size: 14px;'>
                                <p>Estimado/a usuario/a,</p>
                                <p>
                                    Se le ha creado una cuenta de acceso a la plataforma <strong>FileNova</strong>.
                                    A continuación, encontrará sus credenciales:
                                </p>
                                <table width='100%' style='margin: 20px 0;'>
                                    <tr>
                                        <td style='padding: 8px; background-color: #f0f0f0; width: 30%;'><strong>Usuario:</strong></td>
                                        <td style='padding: 8px;'>{username}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px; background-color: #f0f0f0;'><strong>Contraseña:</strong></td>
                                        <td style='padding: 8px;'>{password}</td>
                                    </tr>
                                </table>
                                <p>
                                    Por motivos de seguridad, le recomendamos cambiar su contraseña en su primer inicio de sesión.
                                </p>
                                <p>
                                    Si usted no solicitó este acceso o tiene alguna consulta, por favor comuníquese con el administrador del sistema.
                                </p>
                                <p style='margin-top: 30px;'>
                                    Atentamente,<br />
                                    <strong>{_settings.SenderName}</strong>
                                </p>
                            </td>
                        </tr>
                        <tr>
                            <td style='font-size: 12px; color: #777777; padding-top: 20px; border-top: 1px solid #dddddd;'>
                                Este correo es confidencial y está dirigido únicamente al destinatario indicado.
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </body>
    </html>";

            await EnviarCorreoAsync(toEmail, "Credenciales de acceso a FileNova", bodyHtml);
        }

        public async Task EnviarNotificacionAsync(string destinatario, string asunto, string cuerpo)
        {
            await EnviarCorreoAsync(destinatario, asunto, cuerpo);
        }

        public async Task EnviarNotificacionConAdjuntoAsync(string destinatario, string asunto, string cuerpo, byte[] adjunto, string nombreArchivo)
        {
            await EnviarCorreoAsync(destinatario, asunto, cuerpo, adjunto, nombreArchivo);
        }


        private async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo, byte[]? adjunto = null, string? nombreArchivo = null)
        {
            try
            {
                _logger.LogInfo($" Enviando correo:");
                _logger.LogInfo($"   Para: {destinatario}");
                _logger.LogInfo($"   Asunto: {asunto}");
                _logger.LogInfo($"   Servidor SMTP: {_settings.SmtpServer}:{_settings.Port}");
                _logger.LogInfo($"   Usuario: {_settings.Username}");
                _logger.LogInfo($"   SSL: {_settings.EnableSsl}");

                using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 30000
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = asunto,
                    Body = cuerpo,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                mail.To.Add(destinatario);

                if (adjunto != null && adjunto.Length > 0 && !string.IsNullOrEmpty(nombreArchivo))
                {
                    var attachment = new Attachment(new MemoryStream(adjunto), nombreArchivo);
                    mail.Attachments.Add(attachment);
                    _logger.LogInfo($"   Adjunto: {nombreArchivo} ({adjunto.Length} bytes)");
                }

                _logger.LogInfo($"   Conectando al servidor SMTP...");
                await client.SendMailAsync(mail);
                _logger.LogInfo($"Correo enviado exitosamente a {destinatario}");
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError($" Error SMTP enviando a {destinatario}:");
                _logger.LogError($"   StatusCode: {smtpEx.StatusCode}");
                _logger.LogError($"   Mensaje: {smtpEx.Message}");

                if (smtpEx.InnerException != null)
                {
                    _logger.LogError($"   InnerException: {smtpEx.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error general enviando email a {destinatario}:");
                _logger.LogError($"   Tipo: {ex.GetType().Name}");
                _logger.LogError($"   Mensaje: {ex.Message}");
            }
        }
    }
}