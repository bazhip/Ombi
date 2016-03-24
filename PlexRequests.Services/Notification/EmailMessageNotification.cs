﻿#region Copyright
// /************************************************************************
//    Copyright (c) 2016 Jamie Rees
//    File: EmailMessageNotification.cs
//    Created By: Jamie Rees
//   
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
//   
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
//   
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ************************************************************************/
#endregion
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

using NLog;

using PlexRequests.Core;
using PlexRequests.Core.SettingModels;
using PlexRequests.Services.Interfaces;

namespace PlexRequests.Services.Notification
{
    public class EmailMessageNotification : INotification
    {
        public EmailMessageNotification(ISettingsService<EmailNotificationSettings> settings)
        {
            EmailNotificationSettings = settings;
        }

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ISettingsService<EmailNotificationSettings> EmailNotificationSettings { get; }
        private EmailNotificationSettings Settings => GetConfiguration();
        public string NotificationName => "EmailMessageNotification";

        public async Task NotifyAsync(NotificationModel model)
        {
            var configuration = GetConfiguration();
            if (!ValidateConfiguration(configuration))
            {
                return;
            }

            switch (model.NotificationType)
            {
                case NotificationType.NewRequest:
                    await EmailNewRequest(model);
                    break;
                case NotificationType.Issue:
                    await EmailIssue(model);
                    break;
                case NotificationType.RequestAvailable:
                    throw new NotImplementedException();

                case NotificationType.RequestApproved:
                    throw new NotImplementedException();

                case NotificationType.AdminNote:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        private EmailNotificationSettings GetConfiguration()
        {
            var settings = EmailNotificationSettings.GetSettings();
            return settings;
        }

        private bool ValidateConfiguration(EmailNotificationSettings settings)
        {
            if (!settings.Enabled)
            {
                return false;
            }
            if (string.IsNullOrEmpty(settings.EmailHost) || string.IsNullOrEmpty(settings.EmailUsername) || string.IsNullOrEmpty(settings.EmailPassword) || string.IsNullOrEmpty(settings.RecipientEmail) || string.IsNullOrEmpty(settings.EmailPort.ToString()))
            {
                return false;
            }

            return true;
        }

        private async Task EmailNewRequest(NotificationModel model)
        {
            var message = new MailMessage
            {
                IsBodyHtml = true,
                To = { new MailAddress(Settings.RecipientEmail) },
                Body = $"Hello! The user '{model.User}' has requested {model.Title}! Please log in to approve this request. Request Date: {model.DateTime.ToString("f")}",
                From = new MailAddress(Settings.EmailSender),
                Subject = $"Plex Requests: New request for {model.Title}!"
            };

            try
            {
                using (var smtp = new SmtpClient(Settings.EmailHost, Settings.EmailPort))
                {
                    smtp.Credentials = new NetworkCredential(Settings.EmailUsername, Settings.EmailPassword);
                    smtp.EnableSsl = Settings.Ssl;
                    await smtp.SendMailAsync(message).ConfigureAwait(false);
                }
            }
            catch (SmtpException smtp)
            {
                Log.Error(smtp);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private async Task EmailIssue(NotificationModel model)
        {
            var message = new MailMessage
            {
                IsBodyHtml = true,
                To = { new MailAddress(Settings.RecipientEmail) },
                Body = $"Hello! The user '{model.User}' has reported a new issue {model.Body} for the title {model.Title}!",
                From = new MailAddress(Settings.RecipientEmail),
                Subject = $"Plex Requests: New issue for {model.Title}!"
            };

            try
            {
                using (var smtp = new SmtpClient(Settings.EmailHost, Settings.EmailPort))
                {
                    smtp.Credentials = new NetworkCredential(Settings.EmailUsername, Settings.EmailPassword);
                    smtp.EnableSsl = Settings.Ssl;
                    await smtp.SendMailAsync(message).ConfigureAwait(false);
                }
            }
            catch (SmtpException smtp)
            {
                Log.Error(smtp);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}