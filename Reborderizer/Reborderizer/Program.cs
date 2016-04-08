using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Reborderizer
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceFolder = @"C:\v3cards";
            string destFolder = @"C:\v3cardsreborderized";


            //string originalImage = Path.Combine(sourceFolder, "2-1B (Too-Onebee) (V)\\image.png");
            //string newImage = Path.Combine(sourceFolder, "2-1B (Too-Onebee) (V)\\image_resaved.png");

            //System.Drawing.Bitmap orig = new System.Drawing.Bitmap(originalImage);
            //System.Drawing.Bitmap nonIndexed = WhiteBorderConverter2.CreateNonIndexedImage(orig);
            //nonIndexed.Save(newImage);


            foreach (var dir in Directory.EnumerateDirectories(sourceFolder))
            {
                
                string folderName = Path.GetFileName(dir);
                string fileName = Path.Combine(dir, "image.png");

                Console.WriteLine("Reborderizing card: " + folderName);

                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fileName);

                try
                {
                    bmp = WhiteBorderConverter2.ToWhiteBorder(bmp, System.Drawing.KnownColor.Black);
                }
                catch (Exception ex)
                {
                    // Problem with the conversion!  Just use the original
                }

                string imageFolder = Path.Combine(destFolder, folderName);
                string destFile = Path.Combine(imageFolder, "image.png");

                Directory.CreateDirectory(imageFolder);

                //bmp.SetResolution(96.0f, 96.0f);
                bmp.Save(destFile);	

            }
        }
    }
}
