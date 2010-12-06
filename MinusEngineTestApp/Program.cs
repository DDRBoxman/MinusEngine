using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BiasedBit.MinusEngine;
using System.Threading;

namespace BiasedBit.MinusEngineTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // This whole API is made to be completely asynchronous.
            // There are no blocking calls, hence the usage of event handling delegates.
            // Looks ugly in this console example but it's perfect for UI based applications.
            
            // The call that triggers the program is the near the end of this method
            // (the rest is pretty much setup to react to events)

            // create the API
            MinusApi api = new MinusApi("dummyApiKey");

            // Prepare the items to be uploaded
            String[] items =
            {
                @"C:\Users\bruno\Desktop\clown.png",
                @"C:\Users\bruno\Desktop\small.png"
            };
            IList<String> uploadedItems = new List<String>(items.Length);

            // create a couple of things we're going to need between requests
            CreateGalleryResult galleryCreated = null;

            // set up the listeners for CREATE
            api.CreateGalleryFailed += delegate(MinusApi sender, Exception e)
            {
                // don't do anything else...
                Console.WriteLine("Failed to create gallery..." + e.Message);
            };
            api.CreateGalleryComplete += delegate(MinusApi sender, CreateGalleryResult result)
            {
                // gallery created, trigger upload of the first file
                galleryCreated = result;
                Console.WriteLine("Gallery created! " + result);
                Thread.Sleep(1000);
                Console.WriteLine("Uploading files...");
                api.UploadItem(result.EditorId, result.Key, items[0]);
            };

            // set up the listeners for UPLOAD
            api.UploadItemFailed += delegate(MinusApi sender, Exception e)
            {
                // don't do anything else...
                Console.WriteLine("Upload failed: " + e.Message);
            };
            api.UploadItemComplete += delegate(MinusApi sender, UploadItemResult result)
            {
                // upload complete, either trigger another upload or save the gallery if all files have been uploaded
                Console.WriteLine("Upload successful: " + result);
                uploadedItems.Add(result.Id);
                if (uploadedItems.Count == items.Length)
                {
                    // if all the elements are uploaded, then save the gallery
                    Console.WriteLine("All uploads complete, saving gallery...");
                    api.SaveGallery("testGallery", galleryCreated.EditorId, galleryCreated.Key, uploadedItems.ToArray());
                }
                else
                {
                    // otherwise just keep uploading
                    Console.WriteLine("Uploading item " + (uploadedItems.Count + 1));
                    api.UploadItem(galleryCreated.EditorId, galleryCreated.Key, items[uploadedItems.Count]);
                }
            };

            // set up the listeners for SAVE
            api.SaveGalleryFailed += delegate(MinusApi sender, Exception e)
            {
                Console.WriteLine("Failed to save gallery... " + e.Message);
            };
            api.SaveGalleryComplete += delegate(MinusApi sender)
            {
                Console.WriteLine("Gallery saved! You can now access it at http://min.us/m" + galleryCreated.ReaderId);
            };


            // this is the call that actually triggers the whole program
            api.CreateGallery();
            Thread.Sleep(40000); // 40 seconds should be enough...
        }
    }
}
