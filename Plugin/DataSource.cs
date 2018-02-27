// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSource.cs" company="KriaSoft LLC">
//   Copyright © 2013 Konstantin Tarkus, KriaSoft LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace AmiBroker.Plugin
{
    using System;

    using Models;

    public abstract class DataSource
    {
        internal DataSource(string databasePath, IntPtr mainWnd)
        {
            DatabasePath = databasePath;
            MainWnd = mainWnd;
        }

        internal string DatabasePath { get; set; }

        internal Mode Mode { get; set; } = Mode.Online;

        /// <summary>
        /// Gets the pointer to AmiBroker's main window.
        /// </summary>
        public IntPtr MainWnd { get; }

        /// <summary>
        /// Gets AmiBroker's OLE automation object.
        /// </summary>
        public dynamic Broker { get; private set; }

        public abstract Quotation[] GetQuotes(string ticker, Periodicity periodicity, int size);
    }
}
