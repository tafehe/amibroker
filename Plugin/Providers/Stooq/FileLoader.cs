using System;
using System.Collections.Generic;
using System.IO;

namespace AmiBroker.Plugin.Providers.Stooq
{
    internal abstract class FileLoader
    {
        protected readonly Configuration Configuration;

        protected FileLoader(Configuration configuration)
        {
            Configuration = configuration;
        }

        internal abstract IEnumerable<string> Load();

        internal void SaveFile(IEnumerable<string> fileContent)
        {
            Configuration.LastDownloadRun = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Configuration.LastEntryInFile = ContentValidator.GetLastDate(fileContent);
            
            File.WriteAllLines(Configuration.TickerFile, fileContent);

            Configuration.Save();
        }
    }
}