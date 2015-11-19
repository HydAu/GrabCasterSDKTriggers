// --------------------------------------------------------------------------------------------------
// <copyright file = "EventViewerTrigger.cs" company="Nino Crudele">
//   Copyright (c) 2013 - 2015 Nino Crudele. All Rights Reserved.
// </copyright>
// <summary>
//    Copyright (c) 2013 - 2015 Nino Crudele
//    Blog: http://ninocrudele.me
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License. 
// </summary>
// --------------------------------------------------------------------------------------------------
namespace GrabCaster.SDK.EventViewerTrigger
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Text;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    using Newtonsoft.Json;

    /// <summary>
    /// The event viewer trigger.
    /// </summary>
    [TriggerContract("{843008B6-F4E1-4A29-8082-BDC111EA0E99}", "Event Viwer Trigger", "Intercept Event Viewer Message",
        false, true, false)]
    public class EventViewerTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the event log.
        /// </summary>
        [TriggerPropertyContract("EventLog", "Event Source to monitor")]
        public string EventLog { get; set; }

        /// <summary>
        /// Gets or sets the event message.
        /// </summary>
        [TriggerPropertyContract("EventMessage", "Event Message for the Event Viewer")]
        public string EventMessage { get; set; }

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        public EventActionContext Context { get; set; }

        /// <summary>
        /// Gets or sets the set event action trigger.
        /// </summary>
        public SetEventActionTrigger SetEventActionTrigger { get; set; }

        /// <summary>
        /// Gets or sets the data context.
        /// </summary>
        [TriggerPropertyContract("DataContext", "Trigger Default Main Data")]
        public byte[] DataContext { get; set; }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="setEventActionTrigger">
        /// The set event action trigger.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        [TriggerActionContract("{25F85716-1154-4473-AFFE-F8F4E8AC17A9}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                this.Context = context;
                this.SetEventActionTrigger = setEventActionTrigger;

                var myNewLog = new EventLog { Log = this.EventLog };

                myNewLog.EntryWritten += this.MyOnEntryWritten;
                myNewLog.EnableRaisingEvents = true;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// The my on entry written.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public void MyOnEntryWritten(object source, EntryWrittenEventArgs e)
        {
            var eventViewerMessage = new EventViewerMessage
                                         {
                                             EntryType = e.Entry.EntryType,
                                             MachineName = e.Entry.MachineName,
                                             Message = e.Entry.Message,
                                             Source = e.Entry.Source,
                                             TimeWritten = e.Entry.TimeWritten
                                         };
            var serializedMessage = JsonConvert.SerializeObject(eventViewerMessage);
            this.DataContext = Encoding.UTF8.GetBytes(serializedMessage);
            this.SetEventActionTrigger(this, this.Context);
        }
    }

    /// <summary>
    /// The event viewer message.
    /// </summary>
    [DataContract]
    internal class EventViewerMessage
    {
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        [DataMember]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the machine name.
        /// </summary>
        [DataMember]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the entry type.
        /// </summary>
        [DataMember]
        public EventLogEntryType EntryType { get; set; }

        /// <summary>
        /// Gets or sets the time written.
        /// </summary>
        [DataMember]
        public DateTime TimeWritten { get; set; }
    }
}