using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Xps.Packaging;

// This code is from http://khason.net/blog/converting-fixeddocument-xpsdocument-too-to-flowdocument/

namespace TextViewerFind
{
    public static class Helper
    {
        public static string StipAttributes(this string srs, params string[] attributes)
        {
            return Regex.Replace(srs,
                string.Format(@"{0}(?:\s*=\s*(""[^""]*""|[^\s>]*))?",
                string.Join("|", attributes)),
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public static string ReplaceAttribute(this string srs, string attributeName, string replacementValue)
        {
            return Regex.Replace(srs,
                string.Format(@"{0}(?:\s*=\s*(""[^""]*""|[^\s>]*))?", attributeName),
                string.Format("{0}=\"{1}\"", attributeName, replacementValue),
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public static string ReplaceAttribute(this string srs, string attributeName, string attributeValue, string replacementValue)
        {
            return Regex.Replace(srs,
                string.Format(@"{0}(?:\s*=\s*(""[^""]{1}""|[^\s>]*))?", attributeName,attributeValue),
                string.Format("{0}=\"{1}\"", attributeName, replacementValue),
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public static string GetFileName(this Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                var chunks = uri.OriginalString.Split('/');
                return chunks[chunks.Length - 1];
            }
            return uri.Segments[uri.Segments.Length - 1];
        }

        public static void SaveToDisk(this XpsFont font, string path)
        {
            using (var stm = font.GetStream())
            {
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    var dta = new byte[stm.Length];
                    stm.Read(dta, 0, dta.Length);
                    if (font.IsObfuscated)
                    {
                        var guid = new Guid(font.Uri.GetFileName().Split('.')[0]).ToString("N");
                        var guidBytes = new byte[16];
                        for (var i = 0; i < guidBytes.Length; i++)
                        {
                            guidBytes[i] = Convert.ToByte(guid.Substring(i * 2, 2), 16);
                        }

                        for (var i = 0; i < 32; i++)
                        {
                            var gi = guidBytes.Length - (i % guidBytes.Length) - 1;
                            dta[i] ^= guidBytes[gi];
                        }
                    }
                    fs.Write(dta, 0, dta.Length);
                }
            }
        }



    }
}
