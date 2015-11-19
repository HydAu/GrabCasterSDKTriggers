﻿// --------------------------------------------------------------------------------------------------
// <copyright file = "RfidTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.RfidTrigger
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    using Newtonsoft.Json;

    using Phidgets;
    using Phidgets.Events;

    /// <summary>
    /// The RIFD trigger.
    /// </summary>
    [TriggerContract("{782B745E-1F6F-440A-A209-E250A1EA5013}", "RFID Trigger", "Read Rfid Tag from a Pidget device",
        false, true, false)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    public class RfidTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the event message.
        /// </summary>
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
        [TriggerActionContract("{76438098-8811-4F14-825A-F0B8AB932465}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                this.Context = context;
                this.SetEventActionTrigger = setEventActionTrigger;

                var rfid = new RFID(); // Declare an RFID object

                // initialize our Phidgets RFID reader and hook the event handlers
                rfid.Attach += RfidAttach;
                rfid.Detach += RfidDetach;
                rfid.Error += this.RfidError;

                rfid.Tag += this.RfidTag;
                rfid.TagLost += RfidTagLost;
                rfid.open();

                // Wait for a Phidget RFID to be attached before doing anything with 
                // the object
                rfid.waitForAttachment();

                // turn on the antenna and the led to show everything is working
                rfid.Antenna = true;
                rfid.LED = true;
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Attach event handler. display the serial number of the attached RFID phidget
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void RfidAttach(object sender, AttachEventArgs e)
        {
            // NOP
        }

        /// <summary>
        /// Detach event handler. display the serial number of the detached RFID phidget
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void RfidDetach(object sender, DetachEventArgs e)
        {
            // NOP
        }

        /// <summary>
        /// Print the tag code for the tag that was just lost
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void RfidTagLost(object sender, TagEventArgs e)
        {
            // NOP
        }

        /// <summary>
        /// Error event handler. display the error description string
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void RfidError(object sender, ErrorEventArgs e)
        {
            this.DataContext = Encoding.UTF8.GetBytes(e.Description);
            this.SetEventActionTrigger(this, this.Context);
        }

        /// <summary>
        /// Print the tag code of the scanned tag
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void RfidTag(object sender, TagEventArgs e)
        {
            var rfidTag = new RfidTag { TagId = e.Tag, BankId = "4433EB52-240A-44CC-8A3B-B6673E1E0B31" };

            var tagString = JsonConvert.SerializeObject(rfidTag);

            this.DataContext = Encoding.UTF8.GetBytes(tagString);
            this.SetEventActionTrigger(this, this.Context);
        }
    }

    /// <summary>
    /// The rfid tag.
    /// </summary>
    [DataContract]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    public class RfidTag
    {
        /// <summary>
        /// Gets or sets the tag id.
        /// </summary>
        [DataMember]
        public string TagId { get; set; }

        /// <summary>
        /// Gets or sets the bank id.
        /// </summary>
        [DataMember]
        public string BankId { get; set; }
    }
}