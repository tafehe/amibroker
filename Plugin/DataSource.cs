// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSource.cs" company="KriaSoft LLC">
//   Copyright © 2013 Konstantin Tarkus, KriaSoft LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Documents;

namespace AmiBroker.Plugin
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;

    using Models;

    public class DataSource
    {
        public DataSource(string databasePath, IntPtr mainWnd)
        {
            this.DatabasePath = databasePath;
            this.MainWnd = mainWnd;
        }

        public string DatabasePath { get; set; }

        /// <summary>
        /// Gets the pointer to AmiBroker's main window.
        /// </summary>
        public IntPtr MainWnd { get; private set; }

        /// <summary>
        /// Gets AmiBroker's OLE automation object.
        /// </summary>
        public dynamic Broker { get; private set; }

        public virtual Quotation[] GetQuotes(string ticker, Periodicity periodicity, int limit, Quotation[] existingQuotes)
        {
           
            // TODO: Return the list of quotes for the specified ticker.
            return new Quotation[] { };
        }
    }
}
