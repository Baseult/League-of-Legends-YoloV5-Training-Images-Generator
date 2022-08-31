using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GenerateImages
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
        }

        public Image RoundCorners(Image startImage, int cornerRadius, Color backgroundColor)
        {
            cornerRadius *= 2;
            var roundedImage = new Bitmap(startImage.Width, startImage.Height);

            using (var g = Graphics.FromImage(roundedImage))
            {
                g.Clear(backgroundColor);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                using (Brush brush = new TextureBrush(startImage))
                {
                    using (var gp = new GraphicsPath())
                    {
                        gp.AddArc(-1, -1, cornerRadius, cornerRadius, 180, 90);
                        gp.AddArc(0 + roundedImage.Width - cornerRadius, -1, cornerRadius, cornerRadius, 270, 90);
                        gp.AddArc(0 + roundedImage.Width - cornerRadius, 0 + roundedImage.Height - cornerRadius,
                            cornerRadius, cornerRadius, 0, 90);
                        gp.AddArc(-1, 0 + roundedImage.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);

                        g.FillPath(brush, gp);
                    }
                }

                return roundedImage;
            }
        }

        private double _textbox;
        private int _oldx = 0;
        private int _oldy = 0;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            _textbox = Convert.ToInt32(textBox2.Text);

            const string mappath = "maps/";
            var mp = new DirectoryInfo(mappath);

            const string championpath = "champions/";
            var cp = new DirectoryInfo(championpath);

            var countmapfiles = mp.GetFiles("*.png").Count();
            var countchampfiles = cp.GetFiles("*.png").Count();

            var totalfiles = ((countmapfiles * countchampfiles) * Convert.ToInt32(textBox2.Text)) +
                             (((countmapfiles * countchampfiles) * (int)Math.Round(_textbox * 0.2))) +
                             (((countmapfiles * countchampfiles) * (int)Math.Round(_textbox * 0.1)));

            var countprogessedfiles = 0;

            for (var v = 0; v <= 3; v++)
            {
                var pass = 0;
                var safepath = "data";

                switch (v)
                {
                    case 0:
                        pass = (int)_textbox;
                        safepath = "training/";
                        break;
                    case 1:
                    {
                        var test = _textbox * 0.2;
                        pass = (int)Math.Round(test);
                        safepath = "validation/";
                        break;
                    }
                    case 2:
                    {
                        var test = _textbox * 0.1;
                        pass = (int)Math.Round(test);
                        safepath = "testing/";
                        break;
                    }
                }

                foreach (var mapfile in mp.GetFiles("*.png"))
                {
                    foreach (var champfile in cp.GetFiles("*.png"))
                    {
                        var clearimage = false;

                        for (var i = 1; i <= pass; i++)
                        {
                            countprogessedfiles++;
                            label1.Text = "Images: " + Convert.ToString(totalfiles) + " / " +
                                          Convert.ToString(countprogessedfiles);

                            var map = Image.FromFile(mapfile.FullName);
                            var champion = Image.FromFile(champfile.FullName);

                            if (checkBox2.Checked)
                            {
                                champion = RoundCorners(champion, 25, Color.Transparent);
                            }
                            
                            var rand = new Random();
                            var randwidth = rand.Next(5, map.Width - 5 - 31);
                            var randheight = rand.Next(5, map.Height - 5 - 31);

                            if (randwidth == _oldx && randheight == _oldy)
                            {
                                randwidth /= 2;
                                randheight /= 2;
                            }

                            _oldx = randwidth;
                            _oldy = randheight;

                            var g = Graphics.FromImage(map);
                            g.DrawImage(champion, randwidth, randheight, 31, 31);

                            if (clearimage && checkBox1.Checked)
                            {
                                var ping = Image.FromFile("ping.png");
                                var randpings = rand.Next(0, 5);

                                for (var j = 0; j < randpings; j++)
                                {
                                    var pingwidth = rand.Next(randwidth - ping.Width, randwidth);
                                    var pingheight = rand.Next(randheight - ping.Height, randheight);

                                    var p = Graphics.FromImage(map);
                                    p.DrawImage(ping, pingwidth, pingheight, ping.Width, ping.Height);
                                }
                            }
                            else
                            {
                                clearimage = true;
                            }


                            var numberchar = Convert.ToString(i);
                            var pathexist = "data/" + champfile.Name.Replace(".png", "") + "/" + safepath;

                            if (!Directory.Exists(pathexist))
                            {
                                // Try to create the directory.
                                var di = Directory.CreateDirectory(pathexist);
                            }

                            map.Save("data/" + champfile.Name.Replace(".png", "") + "/" + safepath +
                                     champfile.Name.Replace(".png", "") + numberchar +
                                     mapfile.Name.Replace(".png", "") + ".png");

                            var path = "data/" + champfile.Name.Replace(".png", "") + "/" + safepath +
                                       champfile.Name.Replace(".png", "") + numberchar +
                                       mapfile.Name.Replace(".png", "") + ".xml";
                            if (!File.Exists(path))
                            {
                                // Create a file to write to.
                                using (var sw = File.CreateText(path))
                                {
                                    sw.WriteLine("<annotation>");
                                    sw.WriteLine("<folder>my-project-name</folder>");
                                    sw.WriteLine("<filename>" + champfile.Name.Replace(".png", "") + numberchar +
                                                 ".png</filename>");
                                    sw.WriteLine("<path>/my-project-name/" + champfile.Name.Replace(".png", "") + "/" +
                                                 safepath + champfile.Name.Replace(".png", "") + numberchar +
                                                 mapfile.Name.Replace(".png", "") + ".png</path>");
                                    sw.WriteLine("<source>");
                                    sw.WriteLine("<database>Unspecified</database>");
                                    sw.WriteLine("</source>");
                                    sw.WriteLine("<size>");
                                    sw.WriteLine("<width>" + Convert.ToString(map.Width) + "</width>");
                                    sw.WriteLine("<height>" + Convert.ToString(map.Height) + "</height>");
                                    sw.WriteLine("<depth>3</depth>");
                                    sw.WriteLine("</size>");
                                    sw.WriteLine("<object>");
                                    sw.WriteLine("<name>" + champfile.Name.Replace(".png", "") + "</name>");
                                    sw.WriteLine("<pose>Unspecified</pose>");
                                    sw.WriteLine("<truncated>Unspecified</truncated>");
                                    sw.WriteLine("<difficult>Unspecified</difficult>");
                                    sw.WriteLine("<bndbox>");
                                    sw.WriteLine("<xmin>" + Convert.ToString(randwidth) + "</xmin>");
                                    sw.WriteLine("<ymin>" + Convert.ToString(randheight) + "</ymin>");
                                    sw.WriteLine("<xmax>" + Convert.ToString(randwidth + 31) + "</xmax>");
                                    sw.WriteLine("<ymax>" + Convert.ToString(randheight + 31) + "</ymax>");
                                    sw.WriteLine("</bndbox>");
                                    sw.WriteLine("</object>");
                                    sw.WriteLine("</annotation>");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            const string championpath = "champions/";
            var cp = new DirectoryInfo(championpath);

            var countchampfiles = cp.GetFiles("*.png").Count();

            foreach (var champfile in cp.GetFiles("*.png"))
            {
                var champion = champfile.Name.Replace(".png", "");

                var pathexist = "download/" + champfile.Name.Replace(".png", "");

                if (!Directory.Exists(pathexist))
                {
                    // Try to create the directory.
                    var di = Directory.CreateDirectory(pathexist);
                }

                try
                {
                    using (WebClient client = new WebClient())
                    {

                        client.DownloadFile(
                            new Uri("https://raw.communitydragon.org/latest/game/assets/characters/" +
                                    champion.ToLower() + "/hud/" + champion.ToLower() + "_circle.png"),
                            pathexist + "/" + champion + ".png");
                    }
                }
                catch
                {
                    try
                    {
                        using (WebClient client = new WebClient())
                        {

                            client.DownloadFile(
                                new Uri("https://raw.communitydragon.org/latest/game/assets/characters/" +
                                        champion.ToLower() + "/hud/" + champion.ToLower() + "_circle_0.png"),
                                pathexist + "/" + champion + ".png");
                        }
                    }
                    catch
                    {
                        Console.WriteLine("https://raw.communitydragon.org/latest/game/assets/characters/" + champion.ToLower() + "/hud/" + champion.ToLower() + "_circle.png");
                    }

                }

            }
        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {
            const string mappath = "maps/";
            var mp = new DirectoryInfo(mappath);

            const string championpath = "champions/";
            var cp = new DirectoryInfo(championpath);

            var countmapfiles = mp.GetFiles("*.png").Count();
            var countchampfiles = cp.GetFiles("*.png").Count();
            _textbox = Convert.ToInt32(textBox2.Text);
            try
            {
                var totalfiles = ((countmapfiles * countchampfiles) * (int)_textbox) +
                                 (((countmapfiles * countchampfiles) * (int)Math.Round(_textbox * 0.2))) +
                                 (((countmapfiles * countchampfiles) * (int)Math.Round(_textbox * 0.1)));
                label1.Text = "Images: " + Convert.ToString(totalfiles);
            }
            catch
            {
                // ignored
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            backgroundWorker2.RunWorkerAsync();
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            _textbox = Convert.ToInt32(textBox2.Text);

            const string mappath = "maps/";
            var mp = new DirectoryInfo(mappath);

            const string championpath = "champions/";
            var cp = new DirectoryInfo(championpath);

            var countmapfiles = mp.GetFiles("*.png").Count();
            var countchampfiles = cp.GetFiles("*.png").Count();


            foreach (var mapfile in mp.GetFiles("*.png"))
            {
                foreach (var champfile in cp.GetFiles("*.png"))
                {
                    int size = 20;
                    for (int i = 20; i <= 40; i++) //20 Images with different Sizes
                    {
                        size = i;

                        var map = Image.FromFile(mapfile.FullName);
                        var champion = Image.FromFile(champfile.FullName);

                        var rand = new Random();
                        var randwidth = rand.Next(5, map.Width - 5 - size);
                        var randheight = rand.Next(5, map.Height - 5 - size);

                        if (randwidth == _oldx && randheight == _oldy)
                        {
                            randwidth /= 2;
                            randheight /= 2;
                        }

                        _oldx = randwidth;
                        _oldy = randheight;

                        var g = Graphics.FromImage(map);
                        g.DrawImage(champion, randwidth, randheight, size, size);


                        var numberchar = Convert.ToString(i);
                        var pathexist = "data/" + champfile.Name.Replace(".png", "") + "/" + "training/";

                        if (!Directory.Exists(pathexist))
                        {
                            // Try to create the directory.
                            var di = Directory.CreateDirectory(pathexist);
                        }

                        map.Save("data/" + champfile.Name.Replace(".png", "") + "/" + "training/" +  champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".png");
                        var path = "data/" + champfile.Name.Replace(".png", "") + "/" + "training/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".xml";

                        if (!File.Exists(path))
                        {
                            // Create a file to write to.
                            using (var sw = File.CreateText(path))
                            {
                                sw.WriteLine("<annotation>");
                                sw.WriteLine("<folder>my-project-name</folder>");
                                sw.WriteLine("<filename>" + champfile.Name.Replace(".png", "") + numberchar + ".png</filename>");
                                sw.WriteLine("<path>/my-project-name/" + champfile.Name.Replace(".png", "") + "/" + "training/" + champfile.Name.Replace(".png", "") + numberchar +   mapfile.Name.Replace(".png", "") + ".png</path>");
                                sw.WriteLine("<source>");
                                sw.WriteLine("<database>Unspecified</database>");
                                sw.WriteLine("</source>");
                                sw.WriteLine("<size>");
                                sw.WriteLine("<width>" + Convert.ToString(map.Width) + "</width>");
                                sw.WriteLine("<height>" + Convert.ToString(map.Height) + "</height>");
                                sw.WriteLine("<depth>3</depth>");
                                sw.WriteLine("</size>");
                                sw.WriteLine("<object>");
                                sw.WriteLine("<name>" + champfile.Name.Replace(".png", "") + "</name>");
                                sw.WriteLine("<pose>Unspecified</pose>");
                                sw.WriteLine("<truncated>Unspecified</truncated>");
                                sw.WriteLine("<difficult>Unspecified</difficult>");
                                sw.WriteLine("<bndbox>");
                                sw.WriteLine("<xmin>" + Convert.ToString(randwidth) + "</xmin>");
                                sw.WriteLine("<ymin>" + Convert.ToString(randheight) + "</ymin>");
                                sw.WriteLine("<xmax>" + Convert.ToString(randwidth + size) + "</xmax>");
                                sw.WriteLine("<ymax>" + Convert.ToString(randheight + size) + "</ymax>");
                                sw.WriteLine("</bndbox>");
                                sw.WriteLine("</object>");
                                sw.WriteLine("</annotation>");
                            }
                        }


                    }
                }
            }

            foreach (var mapfile in mp.GetFiles("*.png"))
            {
                foreach (var champfile in cp.GetFiles("*.png"))
                {
                    int size = 20;
                    int count = 0;
                    for (int i = 20; i <= 40; i++) //20 Images with different Sizes
                    {
                        count++;
                        if (count >= 5)
                        {
                            count = 0;

                            size = i;

                            var map = Image.FromFile(mapfile.FullName);
                            var champion = Image.FromFile(champfile.FullName);

                            var rand = new Random();
                            var randwidth = rand.Next(5, map.Width - 5 - size);
                            var randheight = rand.Next(5, map.Height - 5 - size);

                            if (randwidth == _oldx && randheight == _oldy)
                            {
                                randwidth /= 2;
                                randheight /= 2;
                            }

                            _oldx = randwidth;
                            _oldy = randheight;

                            var g = Graphics.FromImage(map);
                            g.DrawImage(champion, randwidth, randheight, size, size);


                            var numberchar = Convert.ToString(i);
                            var pathexist = "data/" + champfile.Name.Replace(".png", "") + "/" + "validation/";

                            if (!Directory.Exists(pathexist))
                            {
                                // Try to create the directory.
                                var di = Directory.CreateDirectory(pathexist);
                            }

                            map.Save("data/" + champfile.Name.Replace(".png", "") + "/" + "validation/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".png");
                            var path = "data/" + champfile.Name.Replace(".png", "") + "/" + "validation/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".xml";

                            if (!File.Exists(path))
                            {
                                // Create a file to write to.
                                using (var sw = File.CreateText(path))
                                {
                                    sw.WriteLine("<annotation>");
                                    sw.WriteLine("<folder>my-project-name</folder>");
                                    sw.WriteLine("<filename>" + champfile.Name.Replace(".png", "") + numberchar + ".png</filename>");
                                    sw.WriteLine("<path>/my-project-name/" + champfile.Name.Replace(".png", "") + "/" + "validation/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".png</path>");
                                    sw.WriteLine("<source>");
                                    sw.WriteLine("<database>Unspecified</database>");
                                    sw.WriteLine("</source>");
                                    sw.WriteLine("<size>");
                                    sw.WriteLine("<width>" + Convert.ToString(map.Width) + "</width>");
                                    sw.WriteLine("<height>" + Convert.ToString(map.Height) + "</height>");
                                    sw.WriteLine("<depth>3</depth>");
                                    sw.WriteLine("</size>");
                                    sw.WriteLine("<object>");
                                    sw.WriteLine("<name>" + champfile.Name.Replace(".png", "") + "</name>");
                                    sw.WriteLine("<pose>Unspecified</pose>");
                                    sw.WriteLine("<truncated>Unspecified</truncated>");
                                    sw.WriteLine("<difficult>Unspecified</difficult>");
                                    sw.WriteLine("<bndbox>");
                                    sw.WriteLine("<xmin>" + Convert.ToString(randwidth) + "</xmin>");
                                    sw.WriteLine("<ymin>" + Convert.ToString(randheight) + "</ymin>");
                                    sw.WriteLine("<xmax>" + Convert.ToString(randwidth + size) + "</xmax>");
                                    sw.WriteLine("<ymax>" + Convert.ToString(randheight + size) + "</ymax>");
                                    sw.WriteLine("</bndbox>");
                                    sw.WriteLine("</object>");
                                    sw.WriteLine("</annotation>");
                                }
                            }

                        }
                        else
                        {
                            continue;
                        }

                    }
                }
            }


            foreach (var mapfile in mp.GetFiles("*.png"))
            {
                foreach (var champfile in cp.GetFiles("*.png"))
                {
                    int size = 20;
                    int count = 0;
                    for (int i = 20; i <= 40; i++) //20 Images with different Sizes
                    {
                        count++;
                        if (count >= 15)
                        {
                            count = 0;

                            size = i;

                            var map = Image.FromFile(mapfile.FullName);
                            var champion = Image.FromFile(champfile.FullName);

                            var rand = new Random();
                            var randwidth = rand.Next(5, map.Width - 5 - size);
                            var randheight = rand.Next(5, map.Height - 5 - size);

                            if (randwidth == _oldx && randheight == _oldy)
                            {
                                randwidth /= 2;
                                randheight /= 2;
                            }

                            _oldx = randwidth;
                            _oldy = randheight;

                            var g = Graphics.FromImage(map);
                            g.DrawImage(champion, randwidth, randheight, size, size);


                            var numberchar = Convert.ToString(i);
                            var pathexist = "data/" + champfile.Name.Replace(".png", "") + "/" + "testing/";

                            if (!Directory.Exists(pathexist))
                            {
                                // Try to create the directory.
                                var di = Directory.CreateDirectory(pathexist);
                            }

                            map.Save("data/" + champfile.Name.Replace(".png", "") + "/" + "testing/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".png");
                            var path = "data/" + champfile.Name.Replace(".png", "") + "/" + "testing/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".xml";

                            if (!File.Exists(path))
                            {
                                // Create a file to write to.
                                using (var sw = File.CreateText(path))
                                {
                                    sw.WriteLine("<annotation>");
                                    sw.WriteLine("<folder>my-project-name</folder>");
                                    sw.WriteLine("<filename>" + champfile.Name.Replace(".png", "") + numberchar + ".png</filename>");
                                    sw.WriteLine("<path>/my-project-name/" + champfile.Name.Replace(".png", "") + "/" + "testing/" + champfile.Name.Replace(".png", "") + numberchar + mapfile.Name.Replace(".png", "") + ".png</path>");
                                    sw.WriteLine("<source>");
                                    sw.WriteLine("<database>Unspecified</database>");
                                    sw.WriteLine("</source>");
                                    sw.WriteLine("<size>");
                                    sw.WriteLine("<width>" + Convert.ToString(map.Width) + "</width>");
                                    sw.WriteLine("<height>" + Convert.ToString(map.Height) + "</height>");
                                    sw.WriteLine("<depth>3</depth>");
                                    sw.WriteLine("</size>");
                                    sw.WriteLine("<object>");
                                    sw.WriteLine("<name>" + champfile.Name.Replace(".png", "") + "</name>");
                                    sw.WriteLine("<pose>Unspecified</pose>");
                                    sw.WriteLine("<truncated>Unspecified</truncated>");
                                    sw.WriteLine("<difficult>Unspecified</difficult>");
                                    sw.WriteLine("<bndbox>");
                                    sw.WriteLine("<xmin>" + Convert.ToString(randwidth) + "</xmin>");
                                    sw.WriteLine("<ymin>" + Convert.ToString(randheight) + "</ymin>");
                                    sw.WriteLine("<xmax>" + Convert.ToString(randwidth + size) + "</xmax>");
                                    sw.WriteLine("<ymax>" + Convert.ToString(randheight + size) + "</ymax>");
                                    sw.WriteLine("</bndbox>");
                                    sw.WriteLine("</object>");
                                    sw.WriteLine("</annotation>");
                                }
                            }

                        }
                        else
                        {
                            continue;
                        }

                    }
                }
            }

        }
    }

}
