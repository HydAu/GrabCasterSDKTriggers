// --------------------------------------------------------------------------------------------------
// <copyright file = "BulksqlServerTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.BULKSQLServerTrigger
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Serialization;
    using GrabCaster.Framework.Contracts.Triggers;

    /// <summary>
    /// The bulksql server trigger.
    /// </summary>
    [TriggerContract("{9A989BD1-C8DE-4FC1-B4BA-02E7D8A4AD76}", "SQL Server Bulk Trigger", "Execute a bulk insert between databases.", false,
        true, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    public class BulksqlServerTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [TriggerPropertyContract("TableName", "TableName")]
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the bulk select query.
        /// </summary>
        [TriggerPropertyContract("BulkSelectQuery", "BulkSelectQuery")]
        public string BulkSelectQuery { get; set; }

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
        [TriggerActionContract("{C55D1D0A-B4B4-4FF0-B41F-38CE0C7A522C}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                this.Context = context;
                this.SetEventActionTrigger = setEventActionTrigger;

                using (var sourceConnection = new SqlConnection(this.ConnectionString))
                {
                    sourceConnection.Open();

                    // Get data from the source table as a SqlDataReader.
                    var commandSourceData = new SqlCommand(this.BulkSelectQuery, sourceConnection);
                    var dataTable = new DataTable();
                    var dataAdapter = new SqlDataAdapter(commandSourceData);
                    dataAdapter.Fill(dataTable);
                    this.DataContext = Serialization.DataTableToByteArray(dataTable);
                    setEventActionTrigger(this, context);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}