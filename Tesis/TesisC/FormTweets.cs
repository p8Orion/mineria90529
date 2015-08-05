using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TesisC
{
    public partial class FormTweets : Form
    {
        private DbTopic About;
        private int Selection; // Positivos, negativos, relacionados con..., etc.
        private String RelatedTo; // Relacionados con x
        private IEnumerable<DbTweet> TweetSet; // A mostrar
        private Core Core;

        public const int SelectionWordRelated = 1;
        public const int SelectionFav = 2;
        public const int SelectionPos = 3;
        public const int SelectionNeg = 4;
        public const int SelectionAmb = 5;

        public FormTweets(Core core, IEnumerable<DbTweet> tweetSet, DbTopic about, int selection, String relatedTo)
        {
            InitializeComponent();

            this.Core = core;
            this.About = about;
            this.Selection = selection;
            this.RelatedTo = relatedTo;
            this.TweetSet = tweetSet;

            ShowTweets();
        }

        private void ShowTweets()
        {
            List<DbTweet> TweetList = TweetSet.ToList();

            if (Selection != SelectionWordRelated) // No se muestran los tweets relacionados con una palabra, sino los que cumplen con cierta característica.
            {
                if (Selection == SelectionFav)
                {
                    TweetList = TweetList.Where(x => x.Weight > 1).ToList();
                    TweetList.Sort((x, y) => { return -x.Weight.CompareTo(y.Weight); });                   
                    if (TweetList.Count > 5) TweetList.RemoveRange(5, TweetList.Count - 5);                    
                }
                if (Selection == SelectionPos)
                {
                    TweetList = TweetList.Where(x => x.PosValue > 0).ToList();
                    TweetList.Sort((x, y) => { return -x.PosValue.CompareTo(y.PosValue); });
                    if (TweetList.Count > 5) TweetList.RemoveRange(5, TweetList.Count - 5);
                }
                if (Selection == SelectionNeg)
                {
                    TweetList = TweetList.Where(x => x.NegValue > 0).ToList();
                    TweetList.Sort((x, y) => { return -x.NegValue.CompareTo(y.NegValue); });
                    if (TweetList.Count > 5) TweetList.RemoveRange(5, TweetList.Count - 5);
                }
                if (Selection == SelectionAmb)
                {
                    TweetList = TweetList.Where(x => x.PosValue > 0 && x.NegValue > 0).ToList();
                    TweetList.Sort((x, y) => { return -(x.PosValue + x.NegValue).CompareTo(y.PosValue + y.NegValue); });
                    if (TweetList.Count > 5) TweetList.RemoveRange(5, TweetList.Count - 5);
                }
            }

            foreach (DbTweet tw in TweetList)
            {
                RichTextBox toAdd = new RichTextBox() { Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular), Width = 500, AutoSize = true, Padding = new Padding(3) };

                toAdd.Enabled = false;
                toAdd.ForeColor = Color.FromArgb(50, 50, 50);
                toAdd.Text = tw.Text;
                foreach (String w in tw.Terms)
                {
                    Color c = Color.FromArgb(255, 0, 0, 128);
                    if (Core.GetDbTopicFromAlias(w) != null)
                    {
                        c = Color.FromArgb(255, 255, 0, 255);

                        toAdd.Find(w);
                        toAdd.SelectionColor = c;
                        toAdd.SelectionFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);
                    }
                    else if (Core.GetDbWordFromName(w) != null)
                    {
                        if (Core.GetDbWordFromName(w).Value == 1)
                            c = Color.FromArgb(255, 0, 128, 0);
                        else if (Core.GetDbWordFromName(w).Value == -1)
                            c = Color.FromArgb(255, 128, 0, 0);
                        else if (Core.GetDbWordFromName(w).Value == 2)
                            c = Color.FromArgb(255, 128, 128, 255);
                        else
                            c = Color.FromArgb(255, 0, 0, 128);

                        toAdd.Find(w, RichTextBoxFinds.WholeWord);
                        toAdd.SelectionColor = c;
                    }
                }

                tableLayoutPanel.RowCount++;
                tableLayoutPanel.Controls.Add(new Label() { Text = tw.Author, Padding = new Padding(3) }, 0, tableLayoutPanel.RowCount - 1);
                tableLayoutPanel.Controls.Add(toAdd, 1, tableLayoutPanel.RowCount - 1);
                tableLayoutPanel.Controls.Add(new Label() { Text = (tw.Weight - 1) + "", Padding = new Padding(3) }, 2, tableLayoutPanel.RowCount - 1);
            }


            tableLayoutPanel.RowStyles[0].Height = 32;
            tableLayoutPanel.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
