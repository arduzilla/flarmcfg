using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Text;

namespace flarmcfg
{
    public partial class Form1 : Form
    {
        private string ?WorkingDirectory;
        private CSV_helper ?gliders = null;
        private CSV_helper ?tpoints = null;
        static private string[] tpnames = new string[] { "TAKEOFF", "START", "FINISH", "LANDING" };
        public Form1()
        {
            InitializeComponent();
            Stopwatch sw = Stopwatch.StartNew();
            //some global initalizations

            if (Properties.Settings.Default.PNAME is null) Properties.Settings.Default.PNAME = "";
            if (Properties.Settings.Default.CPNAME is null) Properties.Settings.Default.CPNAME = "";
            Properties.Settings.Default.Save();
            string GLFileName = Properties.Settings.Default.GLFILE ?? "GL.csv";
            string TPFileName = Properties.Settings.Default.TPFILE ?? "TP.csv";

            try
            {
                SaveResourceToFile(TPFileName);
                SaveResourceToFile(GLFileName);
                gliders = new CSV_helper(GLFileName);
                tpoints = new CSV_helper(TPFileName);
            }
            catch { }

            //if (Properties.Settings.Default.GLFILE != null) gliders = new CSV_helper(Properties.Settings.Default.GLFILE);
            //if (Properties.Settings.Default.TPFILE != null) tpoints = new CSV_helper(Properties.Settings.Default.TPFILE);
            if (Properties.Settings.Default.PNAME != null) textBox_PILOT.Text = Properties.Settings.Default.PNAME;
            if (Properties.Settings.Default.CPNAME != null) textBox_COPILOT.Text = Properties.Settings.Default.CPNAME;

            if ((tpoints is null) || (gliders is null) || (tpoints.Dict is null) || tpoints.Data is null || gliders.Dict is null || gliders.Data is null)
            {
                MessageBox.Show("Data files missing or corrupted!!\r\nYou need to manyally upload it to the program");
                return;
            }
            WorkingDirectory = Directory.GetCurrentDirectory();
            populateTPcombos(tpoints);
            populateGliders(gliders);
        }

        private void SaveResourceToFile(string fileName)
        {
            try
            {
                string resuorceName = getResourceName(fileName);
                Assembly asmbl = Assembly.GetExecutingAssembly();

                using (Stream? stream = asmbl.GetManifestResourceStream(resuorceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    var fout = File.Open(fileName, FileMode.Create, FileAccess.Write);
                    byte[] bytes = Encoding.ASCII.GetBytes(result);
                    fout.Write(bytes, 0, bytes.Length);
                    fout.Close();
                }
            }
            catch { }
        }

        public string getResourceName(string end)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            return asm.GetManifestResourceNames().Single(str => str.EndsWith(end ?? "X"));
        }

        private async void ReadFileFromURL(string file)
        {
            try
            {
                var uri = new Uri("http://www.gultest1.co.uk/flarmcfg/" + file);
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(uri);
                using (var fs = new FileStream(file, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                    fs.Close();
                }
            }
            catch { };
        }

        private int comboCount = 0;

        private void populateTPcombos(CSV_helper tpoints)
        {

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl.Name.ToLower().Contains("combobox_tps")) comboCount += 1;
            }
            StringCollection tpCollection = new StringCollection();
            for(int index = 0; index < tpoints.Data.Count(); index++)
            {
                tpCollection.Add(tpoints.Data[index][tpoints.Dict[Properties.Settings.Default.TPName]] + " : " + tpoints.Data[index][tpoints.Dict[Properties.Settings.Default.TPDesc]]);
            }

            string[] tpPoints = new string[tpCollection.Count];
            tpCollection.CopyTo(tpPoints, 0);

            for(int index = 1; index <= comboCount; index++)
            {
                ComboBox combo = (ComboBox)Controls.Find("comboBox_TPS" + index.ToString(), false)[0];
                combo.AutoCompleteMode = AutoCompleteMode.Suggest;

                combo.Items.Clear();
                combo.Items.AddRange(tpPoints);
            }
        }

        private void populateGliders(CSV_helper gliders)
        {
            ComboBox combo = this.comboBox_GLIDER;
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            combo.Items.Clear();
            foreach(var str in gliders.Data)
            {
                combo.Items.Add(str[gliders.Dict["REG"]]);
            }
        }

        private void comboBox_GLIDER_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            int index = cb.SelectedIndex;

            this.DetailsLabel.Text = "";
            if (gliders is not null)
            {
                for(int field = 0; field < gliders.Data[0].Length; field++)
                this.DetailsLabel.Text += gliders.Data[index][field] + ((index != gliders.Data[0].Length - 1) ? " | " : "");
            }
        }

        private int IsSet(object x)
        {
            int result = 0;
            if (x is ComboBox && ((ComboBox)x).SelectedIndex != -1) result = 1;
            if (x is TextBox && ((TextBox)x).Text.Length != 0) result = 1;
            Color bkClr = result == 1 ? Color.White : ControlPaint.Light(Color.Red);
            if (x is ComboBox) ((ComboBox)x).BackColor = bkClr;
            else ((TextBox)x).BackColor = bkClr;
            return result;
        }

        private string parsePos(string pos)
        {
            string[] result = pos.Split(' ');

            result[0] = result[0].PadLeft(2, '0');
            string[] minutes = result[1].Split('.');
            minutes[0] = minutes[0].PadLeft(2, '0');
            minutes[1] = minutes[1].PadLeft(4, '0');
            result[1] = minutes[0] + minutes[1];
            result[2] = result[2].PadLeft(3, '0');
            minutes = result[3].Split('.');
            minutes[0] = minutes[0].PadLeft(2, '0');
            minutes[1] = minutes[1].PadLeft(4, '0');
            result[3] = minutes[0] + minutes[1];

            string formatedPosition = result[0] + result[1] + ',' + result[2] + result[3];
            return formatedPosition;
        }

        private bool saveTask(string path)
        {
            bool result = true;
            string dummyPoint = "$PFLAC,S,ADDWP,0000000N,00000000E,";
            ComboBox combo;
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    if(textBox_PILOT.Text.Length > 0) writer.WriteLine("$PFLAC,S,PILOT," + textBox_PILOT.Text);
                    if(textBox_COPILOT.Text.Length > 0) writer.WriteLine("$PFLAC,S,COPIL," + textBox_COPILOT.Text);
                    if (gliders is not null && tpoints is not null)
                    {
                        writer.WriteLine("$PFLAC,S,GLIDERTYPE," + gliders.Data[comboBox_GLIDER.SelectedIndex][gliders.Dict["TYPE"]]);
                        writer.WriteLine("$PFLAC,S,GLIDERID," + gliders.Data[comboBox_GLIDER.SelectedIndex][gliders.Dict["REG"]]);
                        writer.WriteLine("$PFLAC,S,COMPCLASS," + gliders.Data[comboBox_GLIDER.SelectedIndex][gliders.Dict["CLASS"]]);
                        writer.WriteLine("$PFLAC,S,LOGINT,1");
                        writer.WriteLine("$PFLAC,S,NEWTASK,", textBox_TASKNAME);
                        if (comboBox_TPS1.SelectedIndex == -1) writer.WriteLine(dummyPoint + "TAKEOFF");
                        else writer.WriteLine("$PFLAC,S,ADDWP," +
                                              parsePos(tpoints.Data[comboBox_TPS1.SelectedIndex][tpoints.Dict[Properties.Settings.Default.TPCoord]]) + "," +
                                              tpoints.Data[comboBox_TPS1.SelectedIndex][tpoints.Dict[Properties.Settings.Default.TPName]]);
                        for(int index = 2; index < comboCount; index++)
                        {
                            combo = (ComboBox)Controls.Find("comboBox_TPS" + index.ToString(), false)[0];
                            if (combo.SelectedIndex != -1)
                            {
                                string part1 = parsePos(tpoints.Data[combo.SelectedIndex][tpoints.Dict[Properties.Settings.Default.TPCoord]]);
                                string part2 = tpoints.Data[combo.SelectedIndex][tpoints.Dict[Properties.Settings.Default.TPName]];
                                writer.WriteLine("$PFLAC,S,ADDWP," + part1 + "," + part2);
                                                  
                            }
                        }
                        combo = (ComboBox)Controls.Find("comboBox_TPS" + comboCount.ToString(), false)[0];
                        if (combo.SelectedIndex == -1) writer.WriteLine(dummyPoint + "LANDING");
                        else writer.WriteLine("$PFLAC,S,ADDWP," +
                                              parsePos(tpoints.Data[combo.SelectedIndex][tpoints.Dict[Properties.Settings.Default.TPCoord]]) + "," +
                                              tpoints.Data[combo.SelectedIndex][tpoints.Dict[Properties.Settings.Default.TPName]]);
                        writer.Close();
                    }
                }
            }
            catch { result = false; }
            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog();

            int numset = //IsSet(comboBox_TPS1) + 
                         IsSet(comboBox_TPS2) + 
                         IsSet(comboBox_TPS3) +
                         //IsSet((ComboBox)Controls.Find("comboBox_TPS" + (comboCount).ToString(), false)[0]) +
                         IsSet((ComboBox)Controls.Find("comboBox_TPS" + (comboCount - 1).ToString(), false)[0]) +
                         IsSet(textBox_PILOT) + 
                         IsSet(comboBox_GLIDER) + 
                         IsSet(textBox_TASKNAME);

            if (numset == 6)
            {
                // Show the FolderBrowserDialog.
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    saveTask(folderBrowserDialog1.SelectedPath + "\\flarmcfg.txt");
                    Properties.Settings.Default.CPNAME = textBox_COPILOT.Text;
                    Properties.Settings.Default.PNAME = textBox_PILOT.Text;
                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Reload();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Form fm = (Form)sender;
        }

        private void checkBox_AUTOLOAD_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            Properties.Settings.Default.CSVAUTOLOAD = cb.Checked;
        }
    }
}