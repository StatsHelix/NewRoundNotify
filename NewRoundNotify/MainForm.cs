using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewRoundNotify
{
    public partial class MainForm : Form
    {
        string ConfigPath { get; set;  }
        HttpListener Server { get; set; }
        
        public MainForm()
        {
            InitializeComponent();
            Initialize();
        }

        public void Initialize()
        {
            var csgodir = GetCSGODir();

            if(csgodir == null)
            {
                MessageBox.Show("We couldn't locate your CS:GO installation, please help us locating the \"csgo\" directory!", "Sorry!");
                var diag = new FolderBrowserDialog();
                diag.ShowDialog();
                if(string.IsNullOrEmpty(diag.SelectedPath) || !Directory.Exists(Path.Combine(diag.SelectedPath, "cfg")))
                {
                    MessageBox.Show("Invalid Path! Aborting, please restart!");
                    Environment.Exit(150);
                }

                csgodir = diag.SelectedPath;
            }
            string configPath = Path.Combine(csgodir, "cfg");
            if(!Directory.Exists(configPath))
            {
                MessageBox.Show("Couldn't find cfg-directory in the CS:GO directory, aborting!", "Error!");
                Environment.Exit(151);
            }

            this.ConfigPath = Path.Combine(configPath, "gamestate_integration_statshelify.cfg");

            if(File.Exists(this.ConfigPath))
            {
                generateConfigButton.Enabled = false;
                generateConfigButton.Text = "Config exists!";
            }
        }

        private void generateConfigButton_Click(object sender, EventArgs e)
        {
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleConfig.txt"), this.ConfigPath);

            MessageBox.Show("You may have to restart CS:GO after the config has been written to implement the changes!", "Info");
            Initialize();
        }

        /// <summary>
        /// Returns the location of the CS:GO installation, or null if it's unable to find it. 
        /// </summary>
        /// <returns></returns>
        private string GetCSGODir()
        {
            string steamPath = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", "");

            string pathsFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(pathsFile))
                return null;

            List<string> libraries = new List<string>();
            libraries.Add(Path.Combine(steamPath));

            var pathVDF = File.ReadAllLines(pathsFile);


            // Okay, this is not a full vdf-parser, but it seems to work pretty much, since the 
            // vdf-grammar is pretty easy. Hopefully it never breaks. I'm too lazy to write a full vdf-parser though. 
            Regex pathRegex = new Regex(@"\""(([^\""]*):\\([^\""]*))\""");
            foreach (var line in pathVDF)
            {
                if(pathRegex.IsMatch(line))
                {
                    string match = pathRegex.Matches(line)[0].Groups[1].Value;

                    // De-Escape vdf. 
                    libraries.Add(match.Replace("\\\\", "\\"));
                }
            }

            foreach(var library in libraries)
            {
                string csgoPath = Path.Combine(library, "steamapps\\common\\Counter-Strike Global Offensive\\csgo");
                if (Directory.Exists(csgoPath))
                {
                    return csgoPath;
                }
            }


            return null;
        }

        private void startServerButton_Click(object sender, EventArgs e)
        {
            startServerButton.Enabled = false;

            Server = new HttpListener();
            Server.Prefixes.Add("http://127.0.0.1:50409/");
            Server.Start();

            var t = new Thread(new ThreadStart(HandleRequests));
            t.IsBackground = true;
            // Set a low priority to not interfere with gameplay!
            t.Priority = ThreadPriority.BelowNormal;
            t.Start();
        }

        private void HandleRequests()
        {
            var serializer = new JsonSerializer();
            while(true)
            {
                var request = Server.GetContext();

                Debug.WriteLine("------");
                Debug.WriteLine(request.Request.Url);

                JObject data = (JObject)serializer.Deserialize(new JsonTextReader(new StreamReader(request.Request.InputStream)));
                
                if(data["round"] != null)
                {
                    if(data["round"]["phase"].Value<string>() == "freezetime")
                    {
                        var process = GetCSGOProcess();
                        if(process == null)
                        {
                            MessageBox.Show("The match goes on!");
                        }
                        else
                        {
                            FlashWindow.Flash(process.MainWindowHandle, 5);
                        }
                    }
                }

                request.Response.StatusCode = 200;
                request.Response.Close();
            }
        }

        private Process GetCSGOProcess()
        {
            var csgo = Process.GetProcessesByName("csgo");

            if (csgo.Length != 0)
                return csgo[0];

            return null;
        }
    }
}
