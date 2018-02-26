using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal abstract class RemoteFileLoader : FileLoader
    {
        protected static IEnumerable<string> GetUrlReponse(string url)
        {
            var request = WebRequest.Create(url);
            var response = request.GetResponse();

            var dataStream = response.GetResponseStream();

            if (dataStream == null) return Enumerable.Empty<string>();
            
            // Open the stream using a StreamReader for easy access.
            var reader = new StreamReader(dataStream);
            // Read the content.
            var responseFromServer = reader.ReadToEnd();

            return Regex.Split(responseFromServer, @"\r\n").ToList();

        }

        protected RemoteFileLoader(Configuration configuration) : base(configuration)
        {
        }
    }
}