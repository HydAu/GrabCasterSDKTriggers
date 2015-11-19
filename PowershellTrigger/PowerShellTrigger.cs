// --------------------------------------------------------------------------------------------------
// <copyright file = "PowerShellTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.PowershellTrigger
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Management.Automation;
    using System.Text;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    /// <summary>
    /// TODO The power shell trigger.
    /// </summary>
    [TriggerContract("{18BB5E65-23A2-4743-8773-32F039AA3D16}", "PowerShell Trigger", 
        "Execute a trigger write in Powerhell script", true, true, false)]
    public class PowerShellTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the script.
        /// </summary>
        [TriggerPropertyContract("Script", "Script to execute")]
        public string Script { get; set; }

        /// <summary>
        /// Gets or sets the script file.
        /// </summary>
        [TriggerPropertyContract("ScriptFile", "Script from file")]
        public string ScriptFile { get; set; }

        /// <summary>
        /// Gets or sets the message properties.
        /// </summary>
        [EventPropertyContract("MessageProperties", "MessageProperties")]
        public string MessageProperties { get; set; }

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
        /// <exception cref="Exception">
        /// </exception>
        [TriggerActionContract("{78B0F3C0-96D6-4DF6-83CD-C282FB6C6D54}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            var script = string.Empty;
            script = this.ScriptFile != string.Empty ? File.ReadAllText(this.ScriptFile) : this.Script;

            var powerShellScript = PowerShell.Create();
            powerShellScript.AddScript(script);

            // foreach (var prop in MessageProperties)
            // {
            // powerShellScript.AddParameter(prop.Key, prop.Value);
            // }
            powerShellScript.AddParameter("DataContext", this.DataContext);
            powerShellScript.Invoke();
            if (powerShellScript.HadErrors)
            {
                var sb = new StringBuilder();
                foreach (var error in powerShellScript.Streams.Error)
                {
                    sb.AppendLine(error.Exception.Message);
                }

                throw new Exception(sb.ToString());
            }

            var outVar = powerShellScript.Runspace.SessionStateProxy.PSVariable.GetValue("DataContext");
            if (outVar != null && outVar.ToString() != string.Empty)
            {
                try
                {
                    var po = (PSObject)outVar;
                    var logEntry = po.BaseObject as EventLogEntry;
                    if (logEntry != null)
                    {
                        var ev = logEntry;
                        this.DataContext = Encoding.UTF8.GetBytes(ev.Message);
                    }
                    else
                    {
                        this.DataContext = Encoding.UTF8.GetBytes(outVar.ToString());
                    }

                    if (this.DataContext.Length != 0)
                    {
                        setEventActionTrigger(this, context);
                    }
                }
                catch
                {
                    // if multiple pso
                    var results = (object[])outVar;
                    foreach (var pos in results)
                    {
                        var po = (PSObject)pos;
                        var logEntry = po.BaseObject as EventLogEntry;
                        if (logEntry != null)
                        {
                            var ev = logEntry;
                            this.DataContext = Encoding.UTF8.GetBytes(ev.Message);
                        }
                        else
                        {
                            this.DataContext = Encoding.UTF8.GetBytes(outVar.ToString());
                        }

                        if (this.DataContext.Length != 0)
                        {
                            setEventActionTrigger(this, context);
                        }
                    }
                }
            }
        }
    }
}