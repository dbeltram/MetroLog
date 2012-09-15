﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Xaml;

namespace MetroLog
{
    /// <summary>
    /// Handles application suspension.
    /// </summary>
    /// <remarks>
    /// This class maintains a list of the managers and tracks application suspension. When the app is suspended,
    /// any discovered targets tht implement ISuspendNotify are called.
    /// </remarks>
    internal class LazyFlushManager
    {
        private ILogManager Owner { get; set; }
        private List<ILazyFlushable> Clients { get; set; }
        private ThreadPoolTimer Timer { get; set; }
        private object _lock = new object();

        private static Dictionary<ILogManager, LazyFlushManager> Owners { get; set; }

        private LazyFlushManager(ILogManager owner)
        {
            this.Owner = owner;
            this.Owner.LoggerCreated += Owner_LoggerCreated;
            
            // clients...
            this.Clients = new List<ILazyFlushable>();

            // timer...
            Timer = ThreadPoolTimer.CreatePeriodicTimer(async (args) =>
            {
                await this.LazyFlushAsync();

            }, TimeSpan.FromMinutes(2));
        }

        void Owner_LoggerCreated(object sender, ILoggerEventArgs e)
        {
            lock(_lock)
            {
                foreach (var target in ((ILoggerQuery)e.Logger).GetTargets())
                {
                    if (target is ILazyFlushable && !(this.Clients.Contains((ILazyFlushable)target)))
                        this.Clients.Add((ILazyFlushable)target);
                }
            }
        }

        static LazyFlushManager()
        {
            Owners = new Dictionary<ILogManager, LazyFlushManager>();
            Application.Current.Suspending += Current_Suspending;
        }

        private static async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            await FlushAllAsync();
        }

        internal static async Task FlushAllAsync()
        {
            var tasks = new List<Task>();
            foreach (var manager in Owners.Values)
                tasks.Add(manager.LazyFlushAsync());

            // wait...
            await Task.WhenAll(tasks);
        }

        private async Task LazyFlushAsync()
        {
            List<ILazyFlushable> toNotify = null;
            lock (_lock)
                toNotify = new List<ILazyFlushable>(this.Clients);

            // walk...
            if (toNotify.Any())
            {
                var context = this.Owner.GetWriteContext();
                var tasks = new List<Task>();
                foreach (var client in toNotify)
                    tasks.Add(client.LazyFlushAsync(context));

                // wait...
                await Task.WhenAll(tasks);
            }
        }

        internal static void Initialize(ILogManager manager)
        {
            Owners[manager] = new LazyFlushManager(manager);
        }
    }
}