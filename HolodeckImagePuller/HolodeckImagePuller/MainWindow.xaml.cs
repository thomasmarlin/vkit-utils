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
using System.Text.RegularExpressions;

namespace WpfApplication1
{


    public class Card
    {
        public string fileName = "";
        public string cardName = "";
        public string side = "";
        public string setName = "";
        public bool nameFixed = false;

        public Card(string _fileName, string _cardName, string _side, string _setName)
        {
            fileName = _fileName;
            cardName = _cardName;
            side = _side;
            setName = _setName;
        }
    };


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


            LinkedList<Card> darkCards = GetHolotableCards(myHolotableFolder.Text, myHolotableFolder.Text + "\\darkside.cdf", false);
            LinkedList<Card> lightCards = GetHolotableCards(myHolotableFolder.Text, myHolotableFolder.Text + "\\lightside.cdf", true);

            darkCards = ReplaceWithFullImages(ref darkCards);

            lightCards = ReplaceWithFullImages(ref lightCards);

            // Get a unique list of cards, adding " (Light)" or " (Dark)" suffixes if needed
            LinkedList<Card> allCards = GetUniqueCardList(ref darkCards, ref lightCards);


            // Next, output all of the cards to the output folder:
            foreach (var card in allCards)
            {
 
                string cardFolder = System.IO.Path.Combine(myOutputFolder.Text, card.cardName);
                string cardPath = System.IO.Path.Combine(cardFolder, "image.png");
                Directory.CreateDirectory(cardFolder);

                File.Copy(card.fileName, cardPath, true);
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

            while (removed[removed.Length - 1] == '\\' || removed[removed.Length - 1] == '"')
            {
                removed = removed.Substring(0, removed.Length - 1);
            }

            return removed;
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


        static LinkedList<Card> GetUniqueCardList(ref LinkedList<Card> darkCards, ref LinkedList<Card> lightCards)
        {
            LinkedList<Card> allCards = new LinkedList<Card>();

            foreach (var card in darkCards)
            {
                allCards.AddLast(card);
            }

            foreach (var card in lightCards)
            {
                allCards.AddLast(card);
            }


            // Apply (Light/Dark) and optionally (Set) to resolve all duplicates
            foreach (Card card1 in allCards)
            {

                LinkedList<Card> cardsMatchingName = new LinkedList<Card>();

                foreach (Card card2 in allCards)
                {
                    if (card1 == card2)
                    {
                        continue;
                    }

                    // Add to the list of matches
                    if (card1.cardName == card2.cardName)
                    {
                        cardsMatchingName.AddLast(card2);
                    }
                }

                if (cardsMatchingName.Count > 0)
                {
                    cardsMatchingName.AddLast(card1);
                }

                // Add Set + Side to every card to resolve the duplication
                foreach (Card dupNamedCard in cardsMatchingName)
                {
                    if (!dupNamedCard.nameFixed)
                    {
                        dupNamedCard.cardName = dupNamedCard.cardName + " (" + dupNamedCard.setName + ") (" + dupNamedCard.side + ")";
                        dupNamedCard.nameFixed = true;
                    }
                }
            }

            return allCards;
        }

        static LinkedList<Card> ReplaceWithFullImages(ref LinkedList<Card> allCards)
        {
            foreach (var card in allCards)
            {
                string largeVersionPath = card.fileName.Replace(@"/t_", @"\large\");
                if (File.Exists(largeVersionPath))
                {
                    card.fileName = largeVersionPath;
                }
                else
                {
                    // Oh well...leave it alone
                }
            }

            return allCards;
        }


        static LinkedList<Card> GetCardsFromString(bool isLight, string readLine, string holotableInstall, ref LinkedList<Card> existingCards)
        {
            LinkedList<Card> foundCards = new LinkedList<Card>();
            if (readLine.StartsWith("card"))
            {

                // Set String we look for is:     Set: Death Star II\n
                // Full Holotable line: card "/starwars/ReflectionsII-Dark/t_theemperor" "�The Emperor (1)\nDark Character - Dark Jedi Master/Imperial [PM]\nSet: Reflections II\nPower: 4 Ability: 7 Dark Jedi Master\nDeploy: 6 Forfeit: 9\nIcons: Death Star II\n\nLore: Leader. Secretive manipulator of the galaxy. Played Darth Vader and Prince Xizor off against one another in his relentless pursuit of 'young Skywalker'.\n\nText: Deploys only to Coruscant or Death Star II. Never moves to a site occupied by opponent (even if carried). If Vader or Xizor here, and Luke is not on table, adds 2 to attrition against opponent at other locations. Immune to attrition."


                // Get the Set
                var cardSet = "";
                Regex regex = new Regex("Set:.*?\\\\n");
                Match match = regex.Match(readLine);
                if (match.Success)
                {
                    //Console.WriteLine(match.Value);
                    cardSet = match.Value;
                    cardSet = cardSet.Replace("Set:", "");
                    cardSet = cardSet.Replace("\\n", "");
                    cardSet.Trim();
                    cardSet = cardSet.Trim();
                }

                // Get Light/Dark
                var side = "Light";
                if (!isLight)
                {
                    side = "Dark";
                }

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

                    string firstFileName = relativePath.Substring(indexOfFirstFile + 1, (indexOfSecondFile - 1) - indexOfFirstFile);
                    string secondFileName = relativePath.Substring(indexOfSecondFile + 1);

                    int indexOfSecondCardName = cardName.IndexOf('/');
                    string firstCardName = cardName.Substring(0, indexOfSecondCardName);
                    string secondCardName = cardName.Substring(indexOfSecondCardName + 1);
                    firstCardName = firstCardName.Trim();
                    secondCardName = secondCardName.Trim() + "_back";

                    firstFileName = holotableInstall + "\\cards\\" + relativePath.Substring(0, indexOfFirstFile) + "/" + firstFileName + ".gif";
                    secondFileName = holotableInstall + "\\cards\\" + relativePath.Substring(0, indexOfFirstFile) + "/t_" + secondFileName + ".gif";

                    firstCardName = GetFriendlyName(firstCardName);
                    secondCardName = GetFriendlyName(secondCardName);
                    var card1 = new Card(firstFileName, firstCardName, side, cardSet);
                    var card2 = new Card(secondFileName, secondCardName, side, cardSet);

                    foundCards.AddLast(card1);
                    foundCards.AddLast(card2);
                }
                else
                {
                    cardName = GetFriendlyName(cardName);
                    string file = holotableInstall + "\\cards\\" + relativePath + ".gif";
                    var card = new Card(file, cardName, side, cardSet);
                    foundCards.AddLast(card);
                }

            }


            return foundCards;
        }

        static LinkedList<Card> GetHolotableCards(string holotableInstall, string cdfFile, bool isLight)
        {
            LinkedList<Card> allCards = new LinkedList<Card>();

            // Open the CDF and add all of the cards
            StreamReader cdfReader = new StreamReader(cdfFile);
            string readLine = cdfReader.ReadLine();

            while (readLine != null)
            {
                //Console.WriteLine(readLine);
                if (readLine.StartsWith("card \"/legacy") || readLine.StartsWith("card \"/TWOSIDED/legacy"))
                {
                    // Legacy card. Skip it!
                }
                else
                {
                    LinkedList<Card> cards = GetCardsFromString(isLight, readLine, holotableInstall, ref allCards);
                    foreach (var card in cards)
                    {
                        allCards.AddLast(card);
                    }
                }
                readLine = cdfReader.ReadLine();
            }

            return allCards;
        }

    }
}


