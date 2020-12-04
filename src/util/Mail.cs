using System;

using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using MailKit.Security;
using System.IO;
using System.Collections.Generic;
using dotenv.net.Utilities;

namespace Utils
{
    class Mail{
        public void EnviarEmail(string titulo, string corpo, List<string> destinatarios){

            EnvReader envReader = new EnvReader();
            var message = new MimeMessage();
            var body = new BodyBuilder();
            var email = envReader.GetStringValue("EMAIL");
            var pass = envReader.GetStringValue("PASS");

            foreach (var d in destinatarios)
            {
			    message.To.Add (new MailboxAddress ( d, d));   
            }
			
            message.Subject = titulo;
            body.TextBody = corpo;       

			message.Body = body.ToMessageBody();
            
			using var client = new SmtpClient ();
            client.CheckCertificateRevocation = false;
            client.Connect ("smtp.zoho.com", 465, SecureSocketOptions.SslOnConnect);

            message.From.Add (new MailboxAddress ("Sincronizador  -  Appharma", "sistemas@approachmobile.company"));
            client.Authenticate (email, pass);

            client.Send (message);
            client.Disconnect (true);
			
        }

    }

}

