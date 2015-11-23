// --------------------------------------------------------------------------------------------------
// <copyright file = "EventArrivedEventArgs.cs" company="Nino Crudele">
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

    using Core.Eventing;

    public sealed class EventArrivedEventArgs : EventArgs
    {
        // Keep this event small.
        /// <summary>
        /// Initializes a new instance of the <see cref="EventArrivedEventArgs"/> class.
        /// </summary>
        /// <param name="error">
        /// The error.
        /// </param>
        internal EventArrivedEventArgs(Exception error)
            : this(0 /*eventId*/, new PropertyBag())
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArrivedEventArgs"/> class.
        /// </summary>
        /// <param name="eventId">
        /// The event id.
        /// </param>
        /// <param name="properties">
        /// The properties.
        /// </param>
        internal EventArrivedEventArgs(ushort eventId, PropertyBag properties)
        {
            this.EventId = eventId;
            this.Properties = properties;
        }

        /// <summary>
        /// Gets the event id.
        /// </summary>
        public ushort EventId { get; }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public PropertyBag Properties { get; }

        /// <summary>
        /// Gets the error.
        /// </summary>
        public Exception Error { get; }
    }
}