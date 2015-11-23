// --------------------------------------------------------------------------------------------------
// <copyright file = "FileTrigger.cs" company="Nino Crudele">
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