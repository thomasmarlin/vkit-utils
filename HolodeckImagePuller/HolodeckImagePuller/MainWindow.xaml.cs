using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void myGoButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(myOutputFolder.Text) ||
                String.IsNullOrWhiteSpace(myHolotableFolder.Text))
            {
                MessageBox.Show("You must enter valid folders above!");
                return;
            }


            Dictionary<string, string> darkCards = GetHolotableCards(myHolotableFolder.Text, myHolotableFolder.Text + "\\darkside.cdf");
            Dictionary<string, string> lightCards = GetHolotableCards(myHolotableFolder.Text, myHolotableFolder.Text + "\\lightside.cdf");

            // For each card, see if there is a full-image card available
            string[] allFiles = Directory.GetFiles(myHolotableFolder.Text + "\\cards\\starwars", "*.*", SearchOption.AllDirectories);

            darkCards = ReplaceWithFullImages(ref darkCards, ref allFiles);
            darkCards = GetFriendlyNameCards(ref darkCards);

            lightCards = ReplaceWithFullImages(ref lightCards, ref allFiles);
            lightCards = GetFriendlyNameCards(ref lightCards);

            // Get a unique list of cards, adding " (Light)" or " (Dark)" suffixes if needed
            Dictionary<string, string> allCards = GetUniqueCardList(ref darkCards, ref lightCards);


            // Next, output all of the cards to the output folder:
            foreach (var file in allCards)
            {
                //string fileNameNoExtension = System.IO.Path.GetFileNameWithoutExtension(file.Key);


                string cardFolder = System.IO.Path.Combine(myOutputFolder.Text, file.Key);
                string cardPath = System.IO.Path.Combine(cardFolder, "image.png");
                Directory.CreateDirectory(cardFolder);

                File.Copy(file.Value, cardPath, true);
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(cardPath);
                if (bmp.Width > bmp.Height)
                {
                    string flippedName = cardPath.Replace(".png", "_FLIPPED.png"); 
                    bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                    bmp.Save(flippedName);
                    File.Delete(cardPath);
                    File.Move(flippedName, cardPath);
                }
                bmp.Dispose();
            }

        }

        static string RemoveQuotes(string original)
        {
            string removed = original;

            while (removed[0] == '\\' || removed[0] == '"')
            {
                removed = removed.Substring(1);
            }

            while (removed[removed.Length-1] == '\\' || removed[removed.Length-1] == '"')
            {
                removed = removed.Substring(0, removed.Length - 1);
            }

            return removed;
        }

        static Dictionary<string, string> GetFriendlyNameCards(ref Dictionary<string, string> unfriendlyCards)
        {
            Dictionary<string, string> friendlyCards = new Dictionary<string, string>();
            foreach (var card in unfriendlyCards)
            {
                string cardName = GetFriendlyName(card.Key);
                int i = 0;
                string uniqueCard = cardName;
                while (friendlyCards.ContainsKey(uniqueCard))
                {
                    i++;
                    uniqueCard = cardName + "_" + i;
                }
                cardName = uniqueCard;

                friendlyCards.Add(cardName, card.Value);
            }

            return friendlyCards;
        }


        static String GetFriendlyName(string unfriendlyName)
        {
            char[] arr = unfriendlyName.ToCharArray();

            arr = Array.FindAll<char>(arr, (c => (
                (char.IsLetterOrDigit(c) ||
                char.IsWhiteSpace(c) ||
                (c == ',') ||
                (c == '-') ||
                (c == '(') ||
                (c == ')')
                ))));
            String friendlyName = new string(arr).Trim();

            return friendlyName;
        }

        static Dictionary<string, string> GetUniqueCardList(ref Dictionary<string, string> darkCards, ref Dictionary<string, string> lightCards)
        {
            Dictionary<string, string> allCards = new Dictionary<string, string>();

            Dictionary<string, string> list1 = darkCards;
            Dictionary<string, string> list2 = lightCards;
            string sufficForDuplicates = " (Dark)";

            for (int i = 0; i < 2; i++)
            {
                if (i == 1)
                {
                    sufficForDuplicates = " (Light)";
                    list1 = lightCards;
                    list2 = darkCards;
                }

                // Add all of the cards to our master list
                // If there is a duplicate file name in dark and light, 
                // add "(Dark)" and "(Light)" suffixes to the cards
                foreach (var card1 in list1)
                {
                    bool duplicateExists = false;
                    foreach (var card2 in list2)
                    {
                        if (card1.Key == card2.Key)
                        {
                            duplicateExists = true;
                        }
                    }

                    if (!duplicateExists)
                    {
                        allCards.Add(card1.Key, card1.Value);
                    }
                    else
                    {
                        allCards.Add(card1.Key + sufficForDuplicates, card1.Value);
                    }
                }
            }

            return allCards;
        }

        static Dictionary<string, string> ReplaceWithFullImages(ref Dictionary<string, string> allCards, ref string[] allFiles)
        {
            Dictionary<string, string> fullImages = new Dictionary<string, string>();
            foreach (var card in allCards)
            {
                string largeVersion = card.Value.Replace(@"/t_", @"\large\");
                if (File.Exists(largeVersion))
                {
                    fullImages.Add(card.Key, largeVersion);
                }
                else
                {
                    fullImages.Add(card.Key, card.Value);
                }
            }

            return fullImages;
        }


        static Dictionary<string, string> GetCardsFromString(string readLine, string holotableInstall, ref Dictionary<string, string> existingCards)
        {
            Dictionary<string, string> allCards = new Dictionary<string, string>();

            Dictionary<string, string> foundCards = new Dictionary<string, string>();
            if (readLine.StartsWith("card"))
            {
                // Parse the line into:
                // card file name\nMoreData

                int iFirstSpace = readLine.IndexOf(" ");
                int iSecondSpace = readLine.IndexOf(" ", iFirstSpace + 1);

                string relativePath = readLine.Substring(iFirstSpace + 1, iSecondSpace - iFirstSpace - 1);
                relativePath = RemoveQuotes(relativePath);

                int iEndOfName = readLine.IndexOf("\\n", iSecondSpace);
                string cardName = readLine.Substring(iSecondSpace + 1, iEndOfName - iSecondSpace - 1);
                cardName = RemoveQuotes(cardName);
                // remove destiny number of card name
                cardName = cardName.Substring(0, cardName.LastIndexOf("(") - 1);


                if (relativePath.StartsWith("/TWOSIDED"))
                {
                    relativePath = relativePath.Replace("/TWOSIDED", "");

                    // 2 cards. After and before the slash
                    int indexOfSecondFile = relativePath.LastIndexOf('/');
                    int indexOfFirstFile = relativePath.LastIndexOf('/', indexOfSecondFile - 1);
                    
                    string firstFileName = relativePath.Substring(indexOfFirstFile+1, (indexOfSecondFile - 1) - indexOfFirstFile);
                    string secondFileName = relativePath.Substring(indexOfSecondFile + 1);

                    int indexOfSecondCardName = cardName.IndexOf('/');
                    string firstCardName = cardName.Substring(0, indexOfSecondCardName);
                    string secondCardName = cardName.Substring(indexOfSecondCardName+1);
                    firstCardName = firstCardName.Trim();
                    secondCardName = secondCardName.Trim();

                    firstFileName = holotableInstall + "\\cards\\" + relativePath.Substring(0, indexOfFirstFile) + "/" + firstFileName + ".gif";
                    secondFileName = holotableInstall + "\\cards\\" + relativePath.Substring(0, indexOfFirstFile) + "/t_" + secondFileName + ".gif";

                    foundCards.Add(firstCardName, firstFileName);
                    foundCards.Add(secondCardName, secondFileName);
                }
                else
                {
                    string file = holotableInstall + "\\cards\\" + relativePath + ".gif";
                    foundCards.Add(cardName, file);
                }

            }

            foreach (var card in foundCards)
            {
                string cardName = card.Key;

                int i = 0;
                string uniqueCard = cardName;
                while (existingCards.ContainsKey(uniqueCard))
                {
                    i++;
                    uniqueCard = cardName + "_" + i;
                }
                cardName = uniqueCard;
                allCards.Add(cardName, card.Value);
            }

            return allCards;
        }

        static Dictionary<string, string> GetHolotableCards(string holotableInstall, string cdfFile)
        {
            Dictionary<string, string> allCards = new Dictionary<string, string>();

            // Open the CDF and add all of the cards
            StreamReader cdfReader = new StreamReader(cdfFile);
            string readLine = cdfReader.ReadLine();

            while (readLine != null)
            {
                if (readLine.StartsWith("card \"/legacy"))
                {
                    // Legacy card. Skip it!
                }
                else
                {
                    Dictionary<string, string> cards = GetCardsFromString(readLine, holotableInstall, ref allCards);
                    foreach (var card in cards)
                    {
                        allCards.Add(card.Key, card.Value);
                    }
                }
                readLine = cdfReader.ReadLine();
            }

            return allCards;
        }

        static void AddCard(string path, ref Dictionary<string, string> allCards)
        {

            string cardName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (path.Contains(@"starwars\Virtual"))
            {
                cardName += " (V)";
            }

            if (path.Contains("-Dark"))
            {
                cardName += " (Dark)";
            }
            else if (path.Contains("-Light"))
            {
                cardName += " (Light)";
            }



            if (cardName.StartsWith("t_"))
            {
                cardName = cardName.Substring(2);
            }


            if (allCards.ContainsKey(cardName))
            {
                allCards[cardName] = path;
            }
            else
            {
                allCards.Add(cardName, path);
            }
        }
    }
}
