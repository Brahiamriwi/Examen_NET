using Examen_NET.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace Examen_NET.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task<(bool success, string message)> SendAppointmentConfirmation(Appointment appointment)
        {
            try
            {
                // Validar que el paciente tenga email
                if (appointment.Patient == null || string.IsNullOrEmpty(appointment.Patient.Email))
                {
                    return (false, "El paciente no tiene correo electrónico registrado");
                }

                // Formatear datos
                var appointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm");
                var appointmentDate = appointment.AppointmentDate.ToString("dd/MM/yyyy");

                // Crear el mensaje
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress(appointment.Patient.FullName, appointment.Patient.Email));
                message.Subject = "Confirmación de Cita Médica - Hospital San Vicente";

                // Cuerpo del correo en HTML
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
                            .content {{ background-color: #f8f9fa; padding: 20px; margin: 20px 0; }}
                            .info-row {{ margin: 10px 0; }}
                            .label {{ font-weight: bold; }}
                            .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>🏥 Hospital San Vicente</h1>
                            </div>
                            <div class='content'>
                                <h2>Confirmación de Cita Médica</h2>
                                <p>Estimado/a <strong>{appointment.Patient.FullName}</strong>,</p>
                                <p>Su cita médica ha sido confirmada exitosamente.</p>
                                
                                <div class='info-row'>
                                    <span class='label'>👨‍⚕️ Médico:</span> Dr. {appointment.Doctor?.FullName}
                                </div>
                                <div class='info-row'>
                                    <span class='label'>🏥 Especialidad:</span> {appointment.Doctor?.Specialty}
                                </div>
                                <div class='info-row'>
                                    <span class='label'>📅 Fecha:</span> {appointmentDate}
                                </div>
                                <div class='info-row'>
                                    <span class='label'>🕐 Hora:</span> {appointmentTime}
                                </div>
                                
                                <p style='margin-top: 20px;'><strong>⚠️ Importante:</strong> Por favor, llegue 10 minutos antes de su cita.</p>
                            </div>
                            <div class='footer'>
                                <p>Este es un correo automático, por favor no responder.</p>
                                <p>&copy; 2025 Hospital San Vicente - Sistema de Gestión de Citas</p>
                            </div>
                        </div>
                    </body>
                    </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                // Enviar el correo
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                // Log en consola
                Console.WriteLine($"✅ Correo enviado a: {appointment.Patient.Email}");

                return (true, "Correo enviado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al enviar correo: {ex.Message}");
                return (false, $"Error al enviar correo: {ex.Message}");
            }
        }
    }
}