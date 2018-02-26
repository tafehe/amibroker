// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Plugin.cs" company="KriaSoft LLC">
//   Copyright © 2013 Konstantin Tarkus, KriaSoft LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using AmiBroker.Plugin.Providers.Stooq;

namespace AmiBroker.Plugin
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;

    using Controls;
    using Models;

    using RGiesecke.DllExport;

    /// <summary>
    /// Standard implementation of a typical AmiBroker plug-ins.
    /// </summary>
    public class Plugin
    {
        /// <summary>
        /// Plugin status code
        /// </summary>
        internal static StatusCode Status { get; set; } = StatusCode.OK;

        /// <summary>
        /// Default encoding
        /// </summary>
        static Encoding encoding = Encoding.GetEncoding("windows-1252"); // TODO: Update it based on your preferences

        static DataSource DataSource;

        /// <summary>
        /// WPF user control which is used to display right-click context menu.
        /// </summary>
        static RightClickMenu RightClickMenu;

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void GetPluginInfo(ref PluginInfo pluginInfo)
        {
            pluginInfo.Name = "http://stooq.pl data Plug-in";
            pluginInfo.Vendor = "KEPISO.PL";
            pluginInfo.Type = PluginType.Data;
            pluginInfo.Version = 100512; // v10.5.12
            pluginInfo.IDCode = new PluginID("STUQ");
            pluginInfo.Certificate = 0;
            pluginInfo.MinAmiVersion = 5600000; // v5.60
            pluginInfo.StructSize = Marshal.SizeOf((PluginInfo)pluginInfo);
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void Init()
        {
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void Release()
        {
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe void Notify(PluginNotification* notification)
        {
            switch (notification->Reason)
            {
                case PluginNotificationReason.DatabaseLoaded:
                    DataSource = new StooqDataSource(
                        databasePath: Marshal.PtrToStringAnsi(notification->DatabasePath),
                        mainWnd: notification->MainWnd
                    );
                    RightClickMenu = new RightClickMenu(DataSource);
                    break;
                case PluginNotificationReason.DatabaseUnloaded:
                    DataSource.DatabasePath = null;
                    break;
                case PluginNotificationReason.StatusRightClick:
                    RightClickMenu.ContextMenu.IsOpen = true;
                    break;
                case PluginNotificationReason.SettingsChange:
                    break;
            }
        }

        /// <summary>
        /// GetQuotesEx function is functional equivalent fo GetQuotes but
        /// handles new Quotation format with 64 bit date/time stamp and floating point volume/open int
        /// and new Aux fields
        /// it also takes pointer to context that is reserved for future use (can be null)
        /// Called by AmiBroker 5.27 and above 
        /// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe int GetQuotesEx(string ticker, Periodicity periodicity, int lastValid, int size, Quotation* quotes, GQEContext* context)
        {
            try
            {
                Debug.WriteLine(
                    $"GetQuotesEx(ticker: {ticker}, periodicity: {periodicity}, lastValid: {lastValid}, size: {size} ..."
                );

                var newQuotes = DataSource.GetQuotes(ticker, periodicity, size);

                // return 'lastValid + 1' if no updates are found and you want to keep all existing records
                if (!newQuotes.Any()) return lastValid + 1;
                
                for (var i = 0; i < newQuotes.Length; i++) Copy(quotes, newQuotes, i);

                return newQuotes.Length;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + Environment.NewLine + e.ToString());
                Debug.WriteLine(e.Message + Environment.NewLine + e.ToString());

                throw e;
            }
        }

        private static unsafe void Copy(Quotation* destination, Quotation[] source, int i)
        {
            destination[i].DateTime = source[i].DateTime;
            destination[i].Price = source[i].Price;
            destination[i].Open = source[i].Open;
            destination[i].High = source[i].High;
            destination[i].Low = source[i].Low;
            destination[i].Volume = source[i].Volume;
            destination[i].OpenInterest = source[i].OpenInterest;
            destination[i].AuxData1 = source[i].AuxData1;
            destination[i].AuxData2 = source[i].AuxData2;
        }

        public unsafe delegate void* Alloc(uint size);

        ///// <summary>
        ///// GetExtra data is optional function for retrieving non-quotation data
        ///// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static AmiVar GetExtraData(string ticker, string name, int arraySize, Periodicity periodicity, Alloc alloc)
        {
            return new AmiVar();
        }

        /// <summary>
        /// GetSymbolLimit function is optional, used only by real-time plugins
        /// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static int GetSymbolLimit()
        {
            return 10000;
        }

        /// <summary>
        /// GetStatus function is optional, used mostly by few real-time plugins
        /// </summary>
        /// <param name="statusPtr">A pointer to <see cref="AmiBrokerPlugin.PluginStatus"/></param>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void GetStatus(IntPtr statusPtr)
        {
            switch (Status)
            {
                case StatusCode.OK:
                    SetStatus(statusPtr, StatusCode.OK, Color.LightGreen, "OK", "Connected");
                    break;
                case StatusCode.Wait:
                    SetStatus(statusPtr, StatusCode.Wait, Color.LightBlue, "WAIT", "Trying to connect...");
                    break;
                case StatusCode.Error:
                    SetStatus(statusPtr, StatusCode.Error, Color.Red, "ERR", "An error occured");
                    break;
                default:
                    SetStatus(statusPtr, StatusCode.Unknown, Color.LightGray, "Ukno", "Unknown status");
                    break;
            }
        }

        #region Helper Functions

        /// <summary>
        /// Notify AmiBroker that new streaming data arrived
        /// </summary>
        static void NotifyStreamingUpdate()
        {
            NativeMethods.SendMessage(DataSource.MainWnd, 0x0400 + 13000, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Update status of the plugin
        /// </summary>
        /// <param name="statusPtr">A pointer to <see cref="AmiBrokerPlugin.PluginStatus"/></param>
        static void SetStatus(IntPtr statusPtr, StatusCode code, Color color, string shortMessage, string fullMessage)
        {
            Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 4), (int)code);
            Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 8), color.R);
            Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 9), color.G);
            Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 10), color.B);

            var msg = encoding.GetBytes(fullMessage);

            for (int i = 0; i < (msg.Length > 255 ? 255 : msg.Length); i++)
            {
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 12 + i), msg[i]);
            }

            Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 12 + msg.Length), 0x0);

            msg = encoding.GetBytes(shortMessage);

            for (int i = 0; i < (msg.Length > 31 ? 31 : msg.Length); i++)
            {
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 268 + i), msg[i]);
            }

            Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 268 + msg.Length), 0x0);
        }

        #endregion

        #region AmiBroker Method Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int GetStockQtyDelegate();

        private static GetStockQtyDelegate GetStockQty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int SetCategoryNameDelegate(int category, int item, string name);

        private static SetCategoryNameDelegate SetCategoryName;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate string GetCategoryNameDelegate(int category, int item);

        private static GetCategoryNameDelegate GetCategoryName;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int SetIndustrySectorDelegate(int industry, int sector);

        private static SetIndustrySectorDelegate SetIndustrySector;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int GetIndustrySectorDelegate(int industry);

        private static GetIndustrySectorDelegate GetIndustrySector;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        public delegate IntPtr AddStockDelegate(string ticker); // returns a pointer to StockInfo

        private static AddStockDelegate AddStock;

        #endregion
    } 
}
