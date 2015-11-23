// --------------------------------------------------------------------------------------------------
// <copyright file = "SqlServerTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.SQLServerTrigger
{
    using System;
    using System.Data.SqlClient;
    using System.Text;
    using System.Xml;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    /// <summary>
    /// The SQL server trigger.
    /// </summary>
    [TriggerContract("{7920EE0F-CAC8-4ABB-82C2-1C69351EDD28}", "Sql Server Trigger", "Execute a Sql query or stored procedure.",
        true, true, false)]
    public class SqlServerTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the SQL query.
        /// </summary>
        [TriggerPropertyContract("SqlQuery", "Select Command [Select * from or EXEC Stored precedure name]")]
        public string SqlQuery { get; set; }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        [TriggerPropertyContract("ConnectionString", "ConnectionString")]
        public string ConnectionString { get; set; }

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
        [TriggerActionContract("{7BA7B689-6A1D-4FF6-87B3-720F9A723FB8}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                this.Context = context;
                this.SetEventActionTrigger = setEventActionTrigger;

                using (var myConnection = new SqlConnection(this.ConnectionString))
                {
                    var selectCommand = new SqlCommand(this.SqlQuery, myConnection);
                    myConnection.Open();
                    XmlReader readerResult = null;
                    try
                    {
                        readerResult = selectCommand.ExecuteXmlReader();
                        readerResult.Read();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (readerResult.EOF)
                    {
                        return;
                    }

                    var xdoc = new XmlDocument();
                    xdoc.Load(readerResult);
                    if (xdoc.OuterXml != string.Empty)
                    {
                        this.DataContext = Encoding.UTF8.GetBytes(xdoc.OuterXml);
                        myConnection.Close();
                        setEventActionTrigger(this, context);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}