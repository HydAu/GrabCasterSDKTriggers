// --------------------------------------------------------------------------------------------------
// <copyright file = "AzureBlobTrigger.cs" company="Nino Crudele">
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
namespace GrabCaster.SDK.AzureBlobTrigger
{
    using System.Diagnostics;
    using System.Text;

    using GrabCaster.Framework.Contracts.Attributes;
    using GrabCaster.Framework.Contracts.Globals;
    using GrabCaster.Framework.Contracts.Triggers;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// The azure blob trigger.
    /// </summary>
    [TriggerContract("{3BADD8A0-211B-4C57-806B-8C0453EB637B}", "Azure Blob Trigger", "Azure Blob Trigger", true, true,
        false)]
    public class AzureBlobTrigger : ITriggerType
    {
        /// <summary>
        /// Gets or sets the storage account.
        /// </summary>
        [TriggerPropertyContract("StorageAccount", "Azure StorageAccount")]
        public string StorageAccount { get; set; }

        /// <summary>
        /// Gets or sets the blob container.
        /// </summary>
        [TriggerPropertyContract("BlobContainer", "Azure Blob Container")]
        public string BlobContainer { get; set; }

        /// <summary>
        /// Gets or sets the blob block reference.
        /// </summary>
        [TriggerPropertyContract("BlobBlockReference", "Azure Blob BlockReference")]
        public string BlobBlockReference { get; set; }

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
        [TriggerActionContract("{1FE5C5DA-F856-458C-8D67-0BF3F5997583}", "Main action", "Main action description")]
        public void Execute(SetEventActionTrigger setEventActionTrigger, EventActionContext context)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(this.StorageAccount);
                var blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve a reference to a container. 
                var container = blobClient.GetContainerReference(this.BlobContainer);

                // Create the container if it doesn't already exist.
                container.CreateIfNotExists();
                container.SetPermissions(
                    new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                var blockBlob = container.GetBlockBlobReference(this.BlobBlockReference);

                try
                {
                    blockBlob.FetchAttributes();
                    var fileByteLength = blockBlob.Properties.Length;
                    var blobContent = new byte[fileByteLength];
                    for (var i = 0; i < fileByteLength; i++)
                    {
                        blobContent[i] = 0x20;
                    }

                    blockBlob.DownloadToByteArray(blobContent, 0);
                    blockBlob.Delete();
                    this.DataContext = blobContent;
                    setEventActionTrigger(this, context);
                }
                catch
                {
                    // ignored
                }
            }
            catch
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
            this.DataContext = Encoding.UTF8.GetBytes(e.Entry.Message);
            this.SetEventActionTrigger(this, this.Context);
        }
    }
}