﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AllureCSharpCommons.AllureModel;
using log4net;

namespace AllureCSharpCommons.Utils
{
    public static class AllureResultsUtils
    {
        private static string _resultsPath;

        public static string ResultsPath
        {
            get
            {
                return _resultsPath ?? (_resultsPath = "");
            }
            set
            {
                _resultsPath = value;
            }
        }

        private static readonly Object AttachmentsLock = new Object();
        private static readonly ILog Log = LogManager.GetLogger(typeof (Allure));

        public static long TimeStamp
        {
            get { return (long) (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds; }
        }

        public static string GenerateUid()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GenerateSha256(byte[] data)
        {
            SHA256Managed crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(data);
            return crypto.Aggregate(hash, (current, bit) => current + bit.ToString("x2"));
        }

        public static string TestSuitePath
        {
            get { return ResultsPath + GenerateUid() + "-testsuite.xml"; }
        }

        public static attachment WriteAttachmentSafely(byte[] attachment, string title, string type)
        {
            try
            {
                return string.IsNullOrEmpty(type)
                    ? WriteAttachment(attachment, title)
                    : WriteAttachment(attachment, title, type);
            }
            catch (Exception ex)
            {
                return WriteAttachmentWithErrorMessage(ex, title);
            }
        }

        public static attachment WriteAttachmentWithErrorMessage(Exception exception, string title)
        {
            string message = exception.Message;
            try
            {
                return WriteAttachment(Encoding.UTF8.GetBytes(message), title);
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Can't write attachment {0}", title), ex);
            }
            return new attachment();
        }

        public static attachment WriteAttachment(byte[] attachment, string title, string type)
        {
            string path = GenerateSha256(attachment) + "-attachment." + MimeTypes.ToExtension(type);
            attachmenttype atype;
            Enum.TryParse(type, true, out atype);
            if (!File.Exists(path))
            {
                if (!type.Contains("image"))
                {
                    using (StreamWriter writer = File.AppendText(path))
                    {
                        lock (AttachmentsLock)
                        {
                            writer.Write(Encoding.UTF8.GetString(attachment));
                        }
                    }
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(attachment))
                    {
                        Image image = Image.FromStream(ms);
                        lock (AttachmentsLock)
                        {
                            image.Save(path);
                        }
                    }
                } 
            }
            return new attachment()
            {
                title = title,
                source = path,
                type = type,
                size = attachment.Length
            };
        }

        public static attachment WriteAttachment(byte[] attachment, string title)
        {
            return null;
        }
    }
}