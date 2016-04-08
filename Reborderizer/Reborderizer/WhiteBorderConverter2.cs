using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bitmap = System.Drawing.Bitmap;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Rectangle = System.Drawing.Rectangle;

namespace Reborderizer
{
    static class WhiteBorderConverter2
    {

        static bool[,] isBlack = null;
        static bool[,] wasChecked = null;
        static bool[,] alreadyAdded = null;
        static Color[,] pixels = null;
        static System.Drawing.Size bmpSize;



        public static Bitmap CreateNonIndexedImage(Bitmap src)
        {
            
            Bitmap newBmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            newBmp.SetResolution(src.HorizontalResolution, src.VerticalResolution);
            using (Graphics gfx = Graphics.FromImage(newBmp))
            {
                gfx.DrawImage(src, 0, 0);
            }
             
            return newBmp;
        }

        public static Bitmap ToWhiteBorder(System.Drawing.Bitmap bmpOriginal, System.Drawing.KnownColor knownColor)
        {
            System.Drawing.Color subColor = GetCompatibleColor(knownColor);

            Bitmap untouchedImage = CreateNonIndexedImage(bmpOriginal);
            Bitmap bmp = CreateNonIndexedImage(bmpOriginal);
            System.Drawing.Size size = bmp.Size;

            isBlack = new bool[bmp.Width, bmp.Height];
            wasChecked = new bool[bmp.Width, bmp.Height];
            alreadyAdded = new bool[bmp.Width, bmp.Height];
            bool[,] isContiguous = new bool[bmp.Width, bmp.Height];
            pixels = new Color[bmp.Width, bmp.Height];
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    isBlack[x, y] = false;
                    wasChecked[x, y] = false;
                    alreadyAdded[x, y] = false;
                    pixels[x, y] = bmp.GetPixel(x, y);
                }
            }
            bmpSize = bmp.Size;



            List<Point> continguousEdges = new List<Point>();
            Point startPoint = new Point(5, 5);
            continguousEdges.Add(startPoint);
            Point topRight = new Point(bmp.Width - 5, 5);
            continguousEdges.Add(topRight);
            Point bottomRight = new Point(bmp.Width - 5, bmp.Height - 5);
            continguousEdges.Add(bottomRight);

            bool pointAdded = true;
            while (continguousEdges.Count > 0)
            {
                pointAdded = false;

                if (continguousEdges.Count > 0)
                {
                    Point point = continguousEdges[0];

                    // Check in each direction

                    //left
                    if (!AlreadyAdded(point.X - 1, point.Y, ref bmp) && IsBlack(point.X - 1, point.Y, ref bmp))
                    {
                        alreadyAdded[point.X - 1, point.Y] = true;
                        continguousEdges.Add(new Point(point.X - 1, point.Y));
                        pointAdded = true;
                    }

                    //right
                    if (!AlreadyAdded(point.X + 1, point.Y, ref bmp) && IsBlack(point.X + 1, point.Y, ref bmp))
                    {
                        alreadyAdded[point.X + 1, point.Y] = true;
                        continguousEdges.Add(new Point(point.X + 1, point.Y));
                        pointAdded = true;
                    }

                    //Up
                    if (!AlreadyAdded(point.X, point.Y - 1, ref bmp) && IsBlack(point.X, point.Y - 1, ref bmp))
                    {
                        alreadyAdded[point.X, point.Y - 1] = true;
                        continguousEdges.Add(new Point(point.X, point.Y - 1));
                        pointAdded = true;
                    }

                    //Down
                    if (!AlreadyAdded(point.X, point.Y + 1, ref bmp) && IsBlack(point.X, point.Y + 1, ref bmp))
                    {
                        alreadyAdded[point.X, point.Y + 1] = true;
                        continguousEdges.Add(new Point(point.X, point.Y + 1));
                        pointAdded = true;
                    }

                    foreach (var pt in continguousEdges)
                    {
                        if ((pt.X == point.X) && (pt.Y == point.Y))
                        {
                            continguousEdges.Remove(pt);
                            break;
                        }

                    }
                }
            }

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    if (alreadyAdded[x, y])
                    {
                        isBlack[x, y] = true;
                    }
                }
            }


            int xMiddle = bmp.Width / 2 + 50;
            // Find top of bottom border (we want to invert the bottom)
            int innerBottomBorder = bmp.Height;
            for (int y = bmp.Height - 1; y > bmp.Height / 2; y--)
            {
                if (isBlack[xMiddle, y])
                {
                    innerBottomBorder = y;
                }
                else
                {
                    //break;
                }
            }
            int innerTopBorder = 0;
            for (int y = 1; y < bmp.Height / 2; y++)
            {
                if (isBlack[xMiddle, y])
                {
                    innerTopBorder = y;
                }
                else
                {
                    //break;
                }
            }

            int innerLeft = 0;
            int innerRight = bmp.Width;
            // Find the inner-left and inner-right border
            for (int x = 2; x < bmp.Width / 2; x++)
            {
                if (isBlack[x, 25])
                {
                    innerLeft = x;
                }
                else
                {
                    //break;
                }
            }

            // Find the inner-left and inner-right border
            for (int x = bmp.Width-1; x > bmp.Width/2; x--)
            {
                if (isBlack[x, 25])
                {
                    innerRight = x;
                }
                else
                {
                    //break;
                }
            }





            System.Drawing.Color drawingColor = subColor;

            System.Drawing.Bitmap whitedOut = bmp;

            // Detect system cards and force a 15-pixel border
            if ((innerBottomBorder < bmp.Height-1) && (innerTopBorder > 0) && (innerLeft > 0) && (innerRight < bmp.Width))
            {
                int defaultBorder = 15;
                int maxBorder = 25;
                if ((innerBottomBorder < (bmp.Height - maxBorder)) ||
                    (innerTopBorder > maxBorder) ||
                    (innerLeft > maxBorder) ||
                    (innerRight < bmp.Width - maxBorder))
                {
                    // System card detected!  Add 4 borders of 15 pixels each!
                    innerBottomBorder = bmp.Height - defaultBorder;
                    innerTopBorder = defaultBorder;
                    innerLeft = defaultBorder;
                    innerRight = bmp.Width - defaultBorder;
                }
            }

            /*
            // Invert the bottom-border and then sub-out all of the other pixels
            for (int x = 0; x < whitedOut.Width; x++)
            {
                for (int y = 0; y < whitedOut.Height; y++)
                {
                    if (y >= innerBottomBorder)
                    {
                        Color oldPixel = whitedOut.GetPixel(x, y);
                        whitedOut.SetPixel(x, y, Color.FromArgb(255 - oldPixel.R, 255 - oldPixel.G, 255 - oldPixel.B));
                    }


                    if ((x <= innerLeft) || (x >= innerRight) || (y <= innerTopBorder) || (y >= innerBottomBorder))
                    {
                        if (isBlack[x, y])
                        {
                            whitedOut.SetPixel(x, y, drawingColor);
                        }
                    }
                }
            }
            */

            /*
            // If not turning white, remove any white pixels
            if (subColor != System.Drawing.Color.White)
            {
                // Remove any white pixels
                for (int x = 0; x < whitedOut.Width; x++)
                {
                    for (int y = 0; y < whitedOut.Height; y++)
                    {
                        if (y >= innerBottomBorder)
                        {
                            int whiteThreshold = 150;
                            Color oldPixel = whitedOut.GetPixel(x, y);
                            if ((oldPixel.R > whiteThreshold) && (oldPixel.G > whiteThreshold) && (oldPixel.B > whiteThreshold))
                            {
                                whitedOut.SetPixel(x, y, drawingColor);
                            }
                        }
                    }
                }
            }
            */


            // Blank out the entire inside of the card
            for (int x = innerLeft-1; x <= innerRight+1; x++)
            {
                for (int y = innerTopBorder; y <= innerBottomBorder; y++)
                {
                    whitedOut.SetPixel(x, y, drawingColor);
                }
            }


            System.Drawing.Rectangle innerImageRect = new System.Drawing.Rectangle(innerLeft, innerTopBorder, innerRight - innerLeft, innerBottomBorder - innerTopBorder);
            System.Drawing.Bitmap innerImage = new Bitmap(innerImageRect.Width, innerImageRect.Height);
            innerImage.SetResolution(untouchedImage.HorizontalResolution, untouchedImage.VerticalResolution);
            using (Graphics g = Graphics.FromImage(innerImage))
            {
                g.DrawImage(untouchedImage, 0, 0, innerImageRect, System.Drawing.GraphicsUnit.Pixel);
            }

            
            //points[0] = new System.Drawing.PointF(15, 15);
            //points[1] = new System.Drawing.PointF(335, 15);
            //points[2] = new System.Drawing.PointF(15, 475);


            // Border should be 4% of the inner-image size
            // innerWidth+0.04%*2 = newWidth;
            // New image needs to be the inner-image-rect + 14 pixel border on all sides
            float widthHeightRatio =  (float)innerImageRect.Width / (float)innerImageRect.Height;
            float borderTop = innerImageRect.Width * 0.048f;
            //float borderSide = borderTop * widthHeightRatio;
            float borderSide = borderTop;
            int newWidth = innerImageRect.Width + (int)(borderSide * 2.0f);
            int newHeight = innerImageRect.Height + (int)(borderTop * 2.0f);


            //float widthHeightRatio =  (float)innerImageRect.Width / (float)innerImageRect.Height;
            //System.Drawing.PointF[] points = new System.Drawing.PointF[3];
            //points[0] = new System.Drawing.PointF(borderSize, borderSize);
            //points[1] = new System.Drawing.PointF(newWidth - borderSize, borderSize);
            //points[2] = new System.Drawing.PointF(borderSize, (newWidth - (borderSize * 2.0f)) / widthHeightRatio + borderSize);


            // First, scale the image UP to the new size
            System.Drawing.Bitmap scaledUp = new System.Drawing.Bitmap(newWidth, newHeight);
            using (System.Drawing.Graphics graphics = Graphics.FromImage(scaledUp))
            {
                graphics.DrawImage(whitedOut, new System.Drawing.PointF[3]
                                                    {new System.Drawing.PointF(0,0), 
                                                     new System.Drawing.PointF(newWidth,0),
                                                     new System.Drawing.PointF(0,newHeight)});
            }

            // Next, re-draw the original inner-image on top of the whited-out version
            using (System.Drawing.Graphics graphics2 = Graphics.FromImage(scaledUp))
            {
                //graphics.DrawImage(untouchedImage, points, innerImageRect, System.Drawing.GraphicsUnit.Pixel);
                //graphics2.DrawImage(innerImage, new System.Drawing.PointF(borderSide, borderTop));
                graphics2.DrawImage(innerImage, 
                    new System.Drawing.RectangleF(new Point((int)borderSide, (int)borderTop), innerImage.Size),
                    new System.Drawing.RectangleF(new Point(0, 0), innerImage.Size),
                    System.Drawing.GraphicsUnit.Pixel);

                innerImage.Save("c:\\temp\\InnerOnly.png");
            }

            whitedOut = scaledUp;
            
            /*
            // Draw a slight black border around the whole image for cutting purposes
            for (int x = 0; x < whitedOut.Width; x++)
            {
                for (int y = 0; y < whitedOut.Height; y++)
                {
                    if ((y == whitedOut.Height - 1) || (x == whitedOut.Width - 1) ||
                        (y == 0) || (x == 0))
                    {
                        whitedOut.SetPixel(x, y, Color.FromArgb(255, 200, 200, 200));
                    }
                }
            }
             */
            


            return whitedOut;

        }



        static bool AlreadyAdded(int x, int y, ref System.Drawing.Bitmap bmp)
        {

            if ((x < 0) || (y < 0) || (x >= bmpSize.Width) || (y >= bmpSize.Height))
            {
                return true;
            }

            return alreadyAdded[x, y];
        }


        static bool IsBlack(int x, int y, ref System.Drawing.Bitmap bmp)
        {
            if ((x < 0) || (y < 0) || (x >= bmpSize.Width) || (y >= bmpSize.Height))
            {
                return false;
            }
            if (isBlack[x, y]) return true;

            int threshold = 50;
            //System.Drawing.Color clr = bmp.GetPixel(x, y);
            Color clr = pixels[x, y];
            if ((clr.R < threshold) && (clr.G < threshold) && (clr.B < threshold))
            {
                return true;
            }
            return false;
        }


        static Point GetBlackNextToChecked(ref System.Drawing.Bitmap bmp)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {

                    if (!wasChecked[x, y])
                    {
                        if (isCheckedAndBlack(x, y - 1) || isCheckedAndBlack(x + 1, y) ||
                            isCheckedAndBlack(x, y + 1) || (isCheckedAndBlack(x - 1, y)))
                        {
                            if (IsBlack(x, y, ref bmp))
                            {
                                return new Point(x, y);
                            }
                        }
                    }
                }
            }

            return new Point(-1, -1);
        }


        static bool isCheckedAndBlack(int x, int y)
        {
            if ((x < 0) || (y < 0) || (x >= bmpSize.Width) || (y >= bmpSize.Height))
            {
                return false;
            }

            return (isBlack[x, y]);

        }

        public static System.Drawing.Color GetCompatibleColor(System.Drawing.KnownColor color)
        {
            System.Drawing.Color drColor = GetCompatibleColorInternal(color);

            iTextSharp.text.BaseColor tsColor = new iTextSharp.text.BaseColor(drColor.R, drColor.G, drColor.B);


            return GetCompatibleColorInternal(System.Drawing.Color.FromArgb(tsColor.R, tsColor.G, tsColor.B));
        }

        static System.Drawing.Color GetCompatibleColorInternal(System.Drawing.KnownColor color)
        {
            System.Drawing.Color drColor = System.Drawing.Color.FromKnownColor(color);
            return GetCompatibleColorInternal(drColor);
        }


        static System.Drawing.Color GetCompatibleColorInternal(System.Drawing.Color color)
        {
            iTextSharp.text.BaseColor tsColor = new iTextSharp.text.BaseColor(color);

            return System.Drawing.Color.FromArgb(tsColor.R, tsColor.G, tsColor.B);
        }


        static public void FixCorners(ref Bitmap bmp, System.Drawing.KnownColor knownColor)
        {

            System.Drawing.Color color = GetCompatibleColor(knownColor);

            // Draw in 15-pixel colors in each of the borders
            int width = bmp.Width;
            int height = bmp.Height;
            System.Drawing.Color cornColor = color;

            int cornerSize = 7;
            for (int x = 0; x <= cornerSize; x++)
            {
                for (int y = cornerSize; y >=0 ; y--)
                {
                    // Top-Left
                    bmp.SetPixel(x, y, cornColor);

                    // Top-right
                    bmp.SetPixel(width - 1 - x, y, cornColor);

                    // Bottom-Left
                    bmp.SetPixel(x, height - 1 - y, cornColor);

                    // Bottom-Right
                    bmp.SetPixel(width - 1 - x, height - 1 - y, cornColor);
                }
            }

            int forceBorderSize = 2;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (
                        // Top
                        ((y < forceBorderSize)) ||
                        ((x < forceBorderSize)) ||
                        ((x > width - 1 - forceBorderSize)) ||
                        ((y > height - 1 - forceBorderSize)))
                    {
                        bmp.SetPixel(x, y, cornColor);
                    }
                }
            }

        }
    }

    
}
