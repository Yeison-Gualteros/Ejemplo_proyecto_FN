using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public interface IEmailService
    {
        Task SendPasswordAsync(string toEmail, string username, string password);

        Task EnviarNotificacionAsync(string destinatario, string asunto, string cuerpo);
        Task EnviarNotificacionConAdjuntoAsync(string destinatario, string asunto, string cuerpo, byte[] adjunto, string nombreArchivo);

    }
}
