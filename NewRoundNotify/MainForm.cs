using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewRoundNotify
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            GetCSGODir();
        }

        public void UpdateStatus()
        {
            
        }

        private void generateConfigButton_Click(object sender, EventArgs e)
        {

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
                string csgoPath = Path.Combine(library, "steamapps\\common\\Counter-Strike Global Offensive");
                if (!Directory.Exists(csgoPath))
                {
                    return csgoPath;
                }
            }


            return null;
        }
    }
}
