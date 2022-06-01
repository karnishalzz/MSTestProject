using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace MSTestApp.Utilities
{
    public class EmailSender
    {
        #region Constructor

        private string _host = "smtp.gmail.com",
            _usrname = "karnish.net.test@gmail.com",
            _password = "tjJC:N4-SASV:b3";
        private int _port = 587;
        private bool _isSSL = true;

        public EmailSender()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["MailUsername"] != null)
                _usrname = System.Configuration.ConfigurationManager.AppSettings["MailUsername"].ToString();
            if (System.Configuration.ConfigurationManager.AppSettings["MailPassword"] != null)
                _password = System.Configuration.ConfigurationManager.AppSettings["MailPassword"].ToString();
            if (System.Configuration.ConfigurationManager.AppSettings["MailHost"] != null)
                _host = System.Configuration.ConfigurationManager.AppSettings["MailHost"].ToString();
            if (System.Configuration.ConfigurationManager.AppSettings["MailPort"] != null)
                _port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MailPort"].ToString());
            if (System.Configuration.ConfigurationManager.AppSettings["MailSSL"] != null)
                _isSSL = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["MailSSL"].ToString());
        }
        public EmailSender(string host, int port, string usrname, string password, bool isSSL)
        {
            _host = host;
            _port = port;
            _usrname = usrname;
            _password = password;
            _isSSL = isSSL;
        }

        #endregion Constructor

        public async Task<string> SendAsync(string senderName, string to, string cc, List<string> bcc, string subject, string body)
        {
            return await SendAsync(PrepareMessage(senderName, to, cc, bcc, subject, body));
        }

        private MailMessage PrepareMessage(string senderName, string to, string cc, List<string> bcc, string subject, string body)
        {
            var msg = new MailMessage();
            try
            {
                if (string.IsNullOrEmpty(senderName))
                    msg.From = new MailAddress(_usrname);
                else
                    msg.From = new MailAddress(_usrname, senderName);
            }
            catch (Exception e)
            {
                throw new Exception("Incorrect configuration settings. " + e.Message);
            }

            if (!string.IsNullOrEmpty(to))
            {
                var splittedTo = to.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < splittedTo.Length; i++)
                {
                    try
                    {
                        //msg.To.Add(new MailAddress(splittedTo[i]));
                        msg.To.Add(new MailAddress(splittedTo[i]));
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            else
                throw new Exception("No email address in to field.");

            if (msg.To.Count == 0)
                throw new Exception("No valid email address in to field.");
            if (!string.IsNullOrEmpty(cc))
            {
                
            }
            if (bcc!=null)
            {
               
            }
          
         
            msg.Subject = subject;
            msg.Body = body;
            msg.IsBodyHtml = true;
            msg.BodyEncoding = Encoding.Default;
            msg.SubjectEncoding = Encoding.Default;
            msg.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            var htmlView = AlternateView.CreateAlternateViewFromString(body, new ContentType("text/html"));
            msg.AlternateViews.Add(htmlView);
            return msg;
        }

        private async Task<string> SendAsync(MailMessage msg)
        {
            SmtpClient smtp = null;
            try
            {
                smtp = GetSmtpClient(_host, _port, _usrname, _password, _isSSL);
                await smtp.SendMailAsync(msg);
                return "OK";
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (smtp != null)
                    smtp.Dispose();
            }
        }

        private SmtpClient GetSmtpClient(string host, int port, string username, string password, bool isSSl)
        {
            var smtp = new SmtpClient();
            try
            {
                smtp = new SmtpClient
                {
                    Host = host,
                    Port = port,
                    EnableSsl = isSSl,
                    Timeout = 100000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };
            }
            catch
            {
                throw new Exception("Incorrect configuration settings.");
            }
            return smtp;
        }
    }
}
