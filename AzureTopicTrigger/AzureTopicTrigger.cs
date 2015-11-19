// --------------------------------------------------------------------------------------------------
// <copyright file = "AzureTopicTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.AzureTopicTrigger
{
    using System;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// The azure topic trigger.
    /// </summary>
    [TriggerContract("{D56A660E-2BBE-4705-BA2E-E89BBE0689DB}", "Azure Topic Trigger", "Azure Topic Trigger", false, true,
        false)]
    public class AzureTopicTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        [TriggerPropertyContract("ConnectionString", "Azure ConnectionString")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the topic path.
        /// </summary>
        [TriggerPropertyContract("TopicPath", "TopicPath")]
        public string TopicPath { get; set; }

        /// <summary>
        /// Gets or sets the messages filter.
        /// </summary>
        [TriggerPropertyContract("MessagesFilter", "MessagesFilter")]
        public string MessagesFilter { get; set; }

        /// <summary>
        /// Gets or sets the subscription name.
        /// </summary>
        [TriggerPropertyContract("SubscriptionName", "SubscriptionName")]
        public string SubscriptionName { get; set; }

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
        [TriggerActionContract("{EB36D04B-7491-46EF-B27F-6F07E2F31D48}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                var namespaceManager = NamespaceManager.CreateFromConnectionString(this.ConnectionString);

                if (!namespaceManager.TopicExists(this.TopicPath))
                {
                    namespaceManager.CreateTopic(this.TopicPath);
                }

                var sqlFilter = new SqlFilter(this.MessagesFilter);

                if (!namespaceManager.SubscriptionExists(this.TopicPath, this.SubscriptionName))
                {
                    namespaceManager.CreateSubscription(this.TopicPath, this.SubscriptionName, sqlFilter);
                }

                var subscriptionClientHigh = SubscriptionClient.CreateFromConnectionString(
                    this.ConnectionString,
                    this.TopicPath,
                    this.SubscriptionName);

                // Configure the callback options
                var options = new OnMessageOptions { AutoComplete = false, AutoRenewTimeout = TimeSpan.FromMinutes(1) };

                // Callback to handle received messages
                subscriptionClientHigh.OnMessage(
                    message =>
                        {
                            try
                            {
                                // Remove message from queue
                                message.Complete();
                                this.DataContext = message.GetBody<byte[]>();
                                setEventActionTrigger(this, context);
                            }
                            catch (Exception)
                            {
                                // Indicates a problem, unlock message in queue
                                message.Abandon();
                            }
                        },
                    options);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}