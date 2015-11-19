// --------------------------------------------------------------------------------------------------
// <copyright file = "SqlServerTrigger.cs" company="Nino Crudele">
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