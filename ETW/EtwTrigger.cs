// --------------------------------------------------------------------------------------------------
// <copyright file = "EtwTrigger.cs" company="Nino Crudele">
//   Copyright (c) 2013 - 2015 Nino Crudele. All Rights Reserved.
// </copyright>
// <summary>
// The MIT License (MIT)
// 
// Copyright (c) 2013 - 2015 Nino Crudele
// Blog: http://ninocrudele.me
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </summary>
// --------------------------------------------------------------------------------------------------
namespace GrabCaster.SDK.ETW
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    using Core.Eventing;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    using Timer = System.Timers.Timer;

    /// <summary>
    /// The etw trigger.
    /// </summary>
    [TriggerContract("{753B071D-FD3D-443F-8368-0727CA8BE84E}", "ETW Trigger", "Intercept ETW Message", false, true,
        false)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    public class EtwTrigger : ITriggerType
    {
        /// <summary>
        /// The lock slim eh queue.
        /// </summary>
        public static LockSlimEhQueue<byte[]> LockSlimEhQueue { get; } = null;

        [TriggerPropertyContract("EtwProvider", "Event Source to monitor")]
        public string EtwProvider { get; set; }

        public EventActionContext Context { get; set; }

        public SetEventActionTrigger SetEventActionTrigger { get; set; }

        [TriggerPropertyContract("DataContext", "Trigger Default Main Data")]
        public byte[] DataContext { get; set; }

        [TriggerActionContract("{C7D6B7CC-7F65-4616-8902-72680148A57B}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                this.SetEventActionTrigger = setEventActionTrigger;
                this.Context = context;

                var sprovider = this.EtwProvider;
                var rewriteProviderId = new Guid("13D5F7EF-9404-47ea-AF13-85484F09F2A7");
                //lockSlimEHQueue = new LockSlimEHQueue<byte[]>(1, 1, SetEventActionTrigger, context, this);
                using (var watcher = new EventTraceWatcher(sprovider, rewriteProviderId))
                {
                    watcher.EventArrived += delegate
                        {
                            //DataContext = Encoding.UTF8.GetBytes(e.Properties["EventData"].ToString());
                            //lockSlimEHQueue.Enqueue(DataContext);
                            setEventActionTrigger(this, context);
                        };

                    // Start listening
                    watcher.Start();

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    public class LockSlimEhQueue<T> : ConcurrentQueue<T>
        where T : class
    {
        protected int CaptureLimit;

        protected EtwTrigger EtwTrigger;

        protected EventActionContext EventActionContext;

        protected ReaderWriterLockSlim Locker;

        protected int OnPublishExecuted;

        //EH
        protected SetEventActionTrigger SetEventActionTrigger;

        protected int TimeLimit;

        public Timer InternalTimer;

        public LockSlimEhQueue(
            int capLimit,
            int timeLimit,
            SetEventActionTrigger setEventActionTrigger,
            EventActionContext eventActionContext,
            EtwTrigger eTwTrigger)
        {
            this.Init(capLimit, timeLimit);
            this.EtwTrigger = eTwTrigger;
            this.SetEventActionTrigger = setEventActionTrigger;
            this.EventActionContext = eventActionContext;
        }

        public event Action<List<T>> OnPublish = delegate { };

        //Write the item
        public new virtual void Enqueue(T item)
        {
            //put in queue
            base.Enqueue(item);
            //If > caplimit the publish
            if (this.Count >= this.CaptureLimit)
            {
                this.Publish();
            }
        }

        private void Init(int capLimit, int timeLimit)
        {
            this.CaptureLimit = capLimit;
            this.TimeLimit = timeLimit;
            this.Locker = new ReaderWriterLockSlim();
            this.InitTimer();
        }

        protected virtual void InitTimer()
        {
            this.InternalTimer = new Timer { AutoReset = false, Interval = this.TimeLimit * 1000 };
            this.InternalTimer.Elapsed += (s, e) => { this.Publish(); };
            this.InternalTimer.Start();
        }

        protected virtual void Publish()
        {
            var task = new Task(
                () =>
                    {
                        var itemsToLog = new List<T>();
                        try
                        {
                            if (Interlocked.CompareExchange(ref this.OnPublishExecuted, 1, 0) > 0)
                            {
                                return;
                            }

                            //if (IsPublishing())
                            //    return;

                            this.InternalTimer.Stop();

                            T item;
                            while (this.TryDequeue(out item))
                            {
                                itemsToLog.Add(item);
                                this.SetEventActionTrigger(this.EtwTrigger, this.EventActionContext);
                            }
                        }
                        catch (ThreadAbortException)
                        {
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        finally
                        {
                            Interlocked.Decrement(ref this.OnPublishExecuted);
                            this.OnPublish(itemsToLog);
                            this.CompletePublishing();
                        }
                    });
            task.Start();
        }

        public bool IsPublishing()
        {
            return (Interlocked.CompareExchange(ref this.OnPublishExecuted, 1, 0) > 0);
        }

        private void CompletePublishing()
        {
            this.InternalTimer.Start();
            Interlocked.Decrement(ref this.OnPublishExecuted);
        }
    }
}