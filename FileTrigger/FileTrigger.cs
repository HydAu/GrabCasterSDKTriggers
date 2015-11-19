// --------------------------------------------------------------------------------------------------
// <copyright file = "FileTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.FileTrigger
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;
    using GrabCaster.Framework.Storage;

    /// <summary>
    /// The file trigger.
    /// </summary>
    [TriggerContract("{3C62B951-C353-4899-8670-C6687B6EAEFC}", "FileTrigger", "Get the content from file in a specific directory or shared forlder.", false, true, false)]
    public class FileTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the regex file pattern.
        /// </summary>
        [TriggerPropertyContract("RegexFilePattern", "File pattern, could be a reular expression")]
        public string RegexFilePattern { get; set; }

        /// <summary>
        /// Gets or sets the polling time.
        /// </summary>
        [TriggerPropertyContract("PollingTime", "Polling time.")]
        public int PollingTime { get; set; }

        /// <summary>
        /// Gets or sets the done extension name.
        /// </summary>
        [TriggerPropertyContract("DoneExtensionName", "Rename extension file received.")]
        public string DoneExtensionName { get; set; }

        /// <summary>
        /// Gets or sets the input directory.
        /// </summary>
        [TriggerPropertyContract("InputDirectory", "Input Directory location")]
        public string InputDirectory { get; set; }

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
        [TriggerActionContract("{58EEAFEF-CF6A-44C3-9BB9-81EFD680CA36}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                while (true)
                {
                    var reg = new Regex(this.RegexFilePattern);
                    if (Directory.GetFiles(this.InputDirectory, "*.txt").Where(path => reg.IsMatch(path)).ToList().Any())
                    {
                        var files =
                            Directory.GetFiles(this.InputDirectory, "*.txt").Where(path => reg.IsMatch(path)).ToList();
                        foreach (var file in files)
                        {
                            var data = File.ReadAllBytes(file);
                            PersistentProvider.PersistMessage(context);
                            File.Delete(Path.ChangeExtension(file, this.DoneExtensionName));
                            File.Move(file, Path.ChangeExtension(file, this.DoneExtensionName));
                            this.DataContext = data;
                            setEventActionTrigger(this, context);
                        }

                    }

                    Thread.Sleep(this.PollingTime);
                }
            }
            catch (Exception)
            {
                setEventActionTrigger(this, null);
            }
        }
    }
}