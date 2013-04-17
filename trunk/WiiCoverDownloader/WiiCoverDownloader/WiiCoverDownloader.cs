using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using Ini;
using System.Threading;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace WiiCoverDownloader
{
    public partial class WiiCoverDownloader : Form
    {
        string VERSION = "3.2";

        string STARTUP_PATH,
                DOWNLOAD_PATH,
                CACHE_PATH,
                TEMP_PATH,
                TOOLS_PATH,
                SETTINGS_PATH,
                SETTINGS_INI_FILE,
                TITLES_FILE,
                LANGUAGES_FILE,
                GAMES_ID_FILE,
                GAMETDB_ARCHIVE;

        string FILE_TO_DOWNLOAD;        

        // all used link
        string        
        NewWiiCoverDownloaderZip_Link = "http://wii-cover-downloader.googlecode.com/files/WiiCoverDownloader%20v",       
        languagesIni_Link = "http://wii-cover-downloader.googlecode.com/files/languages%20v",                                 
        _7za920_Link = "http://wii-cover-downloader.googlecode.com/files/7za920.exe",
        tools_Link = "http://wii-cover-downloader.googlecode.com/files/tools.zip",
        sleep_Link = "http://wii-cover-downloader.googlecode.com/files/sleep.exe",
        wbfs_file_Link = "http://wii-cover-downloader.googlecode.com/files/wbfs_file.exe",
        wit_Link = "http://wii-cover-downloader.googlecode.com/files/wit.exe";  
                 
        bool
        DOWNLOAD_OR_EXE_WORKING, TIME_OUT, USE_PROGRESS_BAR,
        GX_COVER, CFG_COVER, WIIFLOW_COVER,
        LOADER_FOUND,
        WII_GAMES_FOUND, GAMECUBE_GAMES_FOUND, NAND_GAMES_FOUND,
        FORCE_ROOT_CHANGE;

        long
        FINAL_FILE_SIZE, ACTUAL_FILE_SIZE;

        struct TitleInfo
        {
            public string ID, name;
        }        

        private List<TitleInfo> WiiGamesList;
        private List<TitleInfo> GameCubeGamesList;
        private List<TitleInfo> ChannelTitlesList;
        private List<TitleInfo> TITLES_LIST;      

        int PASSAGE_COUNT, TYMER_INTERVAL, MAX_DELAY_FOR_TYMER = 60000;
        int DOWNLOADED_IMAGES, CACHED_IMAGES, NOT_FOUND_IMAGES, ALREADY_PRESENT_IMAGES;
        int TEMP_WII_GAMES_COUNT, WII_GAMES_COUNT, TEMP_GAMECUBE_GAMES_COUNT, GAMECUBE_GAMES_COUNT, NAND_GAMES_COUNT;
        int FINAL_WII_GAMES_COUNT, FINAL_GAMECUBE_GAMES_COUNT, FINAL_NAND_GAMES_COUNT, FINAL_TITLE_COUNT;

        internal const int SC_CLOSE = 0xF060;           //close button's code in Windows API
        internal const int MF_ENABLED = 0x00000000;     //enabled button status
        internal const int MF_GRAYED = 0x1;             //disabled button status (enabled = false)                   

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr HWNDValue, bool isRevert);

        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(IntPtr tMenu, int targetItem, int targetStatus);

        WiiCoverDownloaderWait WiiCoverDownloaderWaitForm = new WiiCoverDownloaderWait();

        public WiiCoverDownloader()
        {
            InitializeComponent();

            WiiCoverDownloaderWaitForm.Owner = this;
            WiiCoverDownloaderWaitForm.Show();
            WiiCoverDownloaderWaitForm.Refresh();

            // define path
            STARTUP_PATH = System.IO.Directory.GetCurrentDirectory();
            DOWNLOAD_PATH = CombinePath(STARTUP_PATH, "Download");
            TEMP_PATH = CombinePath(STARTUP_PATH, "Download", "Temp");
            CACHE_PATH = CombinePath(STARTUP_PATH, "Cache");
            TOOLS_PATH = CombinePath(STARTUP_PATH, "Tools");
            SETTINGS_PATH = CombinePath(STARTUP_PATH, "Settings");
            SETTINGS_INI_FILE = CombinePath(SETTINGS_PATH, "settings.ini");
            TITLES_FILE = CombinePath(TOOLS_PATH, "titles.txt");
            LANGUAGES_FILE = CombinePath(SETTINGS_PATH, "languages.ini");
            GAMETDB_ARCHIVE = CombinePath(TOOLS_PATH, "wiitdb.zip");
            GAMES_ID_FILE = CombinePath(TOOLS_PATH, "GAMES_ID_FILE.txt");

            FileDelete(CombinePath(STARTUP_PATH, "WiiCoverDownloaderUpdater.bat"));
            DirectoryClean(DOWNLOAD_PATH);

            DOWNLOAD_OR_EXE_WORKING = true;
        
            // initialize struct
            TITLES_LIST = new List<TitleInfo>();
            WiiGamesList = new List<TitleInfo>();
            GameCubeGamesList= new List<TitleInfo>();
            ChannelTitlesList = new List<TitleInfo>();              

            // check for application
            NetworkCheck();
            FolderCheck();
            ToolsCheck();
            SettingsCheck();
            VersionCheck();            
            LanguageCheck();
            ReloadAppLanguages();
            DevicePathCheck();
            TitlesFileCheck();
            AddCredits();

            labelVersion.Text = this.Text + " v" + VERSION;

            labelProgrssBar.Text = "";

            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                WiiCoverDownloaderWaitForm.Close();

            CheckForDevice();
            DOWNLOAD_OR_EXE_WORKING = false;
        }

        private void CreateLogFile()
        {
            FileDelete(CombinePath(STARTUP_PATH, "last_download_log.txt"));

            System.IO.StreamWriter LogFile = new System.IO.StreamWriter(CombinePath(STARTUP_PATH, "last_download_log.txt"), true, Encoding.Unicode);

            LogFile.WriteLine("\r\n************************************************************************************\r\n"
                                + "* This file was automatically created by WiiCoverDownloader on " + DateTime.Now
                                + " *\r\n************************************************************************************\r\n\r\n");

            LogFile.WriteLine(richTextBoxInfo.Text.Replace("\n", "\r\n"));

            LogFile.Close();

        }

        private bool CreateBatchFileForUpdate()
        {
            string lines;

            string sleep = "tools\\sleep.exe";
            string program = "WiiCoverDownloader.exe";
            string new_program = "NewWiiCoverDownloader.exe";

            FileDelete(CombinePath(STARTUP_PATH, "WiiCoverDownloaderUpdater.bat"));

            lines = "@echo off\r\n" +
                    "cls\r\n" +
                    "if EXIST " + program + " goto check1_ok\r\n" +
                    "echo.\r\n" +
                    "echo " + program + " not found.\r\n" +
                    "echo.\r\n" +
                    "echo WiiCoverDownloaderUpdater.bat can't do anything and will be close.\r\n" +
                    "echo.\r\n" +
                    "pause\r\n" +
                    "goto end_of_batch\r\n" +
                    ":check1_ok\r\n" +
                    "if EXIST " + new_program + " goto check2_ok\r\n" +
                    "echo.\r\n" +
                    "echo " + new_program + " not found.\r\n" +
                    "echo.\r\n" +
                    "echo WiiCoverDownloaderUpdater.bat can't do anything and will be close.\r\n" +
                    "echo.\r\n" +
                    "pause\r\n" +
                    "goto end_of_batch\r\n" +
                    ":check2_ok\r\n" +
                    "cls\r\n" +
                    "echo.\r\n" +
                    "echo ------- Downloaded New version: Restaring WiiCoverDownloader -------\r\n" +
                    "echo.\r\n" +
                    "echo|set /p=Please wait a few seconds...\r\n" +
                    "echo|set /p=.\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    "del " + program + " /q\r\n" +
                    "ren " + new_program + " " + program + "\r\n" +
                    "if exist " + sleep + " " + sleep + " 1\r\n" +
                    "echo|set /p=.\r\n" +
                    ":close_batch\r\n" +
                    "if EXIST " + program + " start " + program + "\r\n";

            System.IO.StreamWriter WiiCoverDownloaderUpdaterFile = new System.IO.StreamWriter(CombinePath(STARTUP_PATH, "WiiCoverDownloaderUpdater.bat"));
            WiiCoverDownloaderUpdaterFile.WriteLine(lines);
            WiiCoverDownloaderUpdaterFile.Close();

            if (File.Exists(CombinePath(STARTUP_PATH, "WiiCoverDownloaderUpdater.bat")))
                return true;
            else
                return false;

        }

        private void AddCredits()
        {
            if(!File.Exists(LANGUAGES_FILE))
                return;
            
            richTextBoxInfoAndCredits.AppendText("\n");

            string[] langFile = File.ReadAllLines(LANGUAGES_FILE);

            foreach (string line in langFile)
            {
                if (line.Trim() == "")
                    continue;

                if (line.Length < 2)
                    continue;                

                if ((line.Substring(0, 1) == "[") && (line.Substring(line.Length - 1, 1) == "]"))
                    break;

                richTextBoxInfoAndCredits.AppendText("\n" + line);
                
            }
        }


        static bool FileInUse(string path)
        {
            int error = 0;
        retryFileCheckState:
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    Thread.Sleep(0);
                    return false;
                }

            }
            catch
            {
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    Thread.Sleep(0);
                    Thread.Sleep(200);
                    error++;
                    if (error == 1)
                        goto retryFileCheckState;
                    else
                        return true;
                }
                catch
                {
                    return true;
                }
            }
        }

        static bool FileDelete(string file_to_delete)
        {
            if (!File.Exists(file_to_delete))
                return true;

            if (!FileInUse(file_to_delete))
            {
                File.SetAttributes(file_to_delete, FileAttributes.Normal);
                File.Delete(file_to_delete);
                return true;
            }

            return false;
        }

        public string CombinePath(params string[] path)
        {
            string new_path = path[0];

            for (int i = 1; i < path.Length; i++)
                new_path = System.IO.Path.Combine(new_path, path[i]);

            return new_path;
        }

        private void LanguageCheck()
        {
            if (!ReadLanguageFile())
            {
                SearchLanguageAndSetComboBox();
                SaveLanguageValue();
            }
        }

        private void DevicePathCheck()
        {
            if (!ReadPathDeviceSettingsFile())
                CreatePathDeviceSettingsFile();
        }

        private bool ReadLanguageFile()
        {

            IniFile inifile = new IniFile(SETTINGS_INI_FILE);

            if (inifile.IniReadValue("Languages", "PrimaryLanguage") == "")
                return false;

            comboBoxPrimaryLanguage.Text = inifile.IniReadValue("Languages", "PrimaryLanguage");

            comboBoxLang1.Text = inifile.IniReadValue("Languages", "Lang1");
            comboBoxLang2.Text = inifile.IniReadValue("Languages", "Lang2");
            comboBoxLang3.Text = inifile.IniReadValue("Languages", "Lang3");
            comboBoxLang4.Text = inifile.IniReadValue("Languages", "Lang4");
            comboBoxLang5.Text = inifile.IniReadValue("Languages", "Lang5");
            comboBoxLang6.Text = inifile.IniReadValue("Languages", "Lang6");
            comboBoxLang7.Text = inifile.IniReadValue("Languages", "Lang7");
            comboBoxLang8.Text = inifile.IniReadValue("Languages", "Lang8");

            if (inifile.IniReadValue("Languages", "GameTDB_language") == "" || inifile.IniReadValue("Languages", "GameTDB_language") == "ALL - All languages")
                inifile.IniWriteValue("Languages", "GameTDB_language", SearchLanguageForApplication("wiitdb"));               
            
            comboBoxGameTDBLanguage.Text = inifile.IniReadValue("Languages", "GameTDB_language");
            if (comboBoxGameTDBLanguage.Text == "")
            {
                inifile.IniWriteValue("Languages", "GameTDB_language", SearchLanguageForApplication("wiitdb"));
                comboBoxGameTDBLanguage.Text = inifile.IniReadValue("Languages", "GameTDB_language");
            }   

            if (inifile.IniReadValue("Languages", "Enable_GameTDB_download") == "True")
                checkBoxDownloadGameTDBPack.Checked = true;
            else
                checkBoxDownloadGameTDBPack.Checked = false;

            if (checkBoxDownloadGameTDBPack.Checked)
                comboBoxGameTDBLanguage.Enabled = true;
            else
                comboBoxGameTDBLanguage.Enabled = false;

            return true;

        }

        private void SearchLanguageAndSetComboBox()
        {
            //  string language;
            switch (System.Globalization.RegionInfo.CurrentRegion.ThreeLetterISORegionName)
            {
                case "ITA":
                    comboBoxPrimaryLanguage.Text = "IT - Italian";
                    break;
                case "JPN":
                    comboBoxPrimaryLanguage.Text = "JA - Japanese";
                    break;
                case "ESP":
                case "ARG":
                case "BOL":
                case "CHL":
                case "COL":
                case "CRI":
                case "CUB":
                case "ECU":
                case "SLV":
                case "GTM":
                case "HND":
                case "MEX":
                case "NIC":
                case "PAN":
                case "PRY":
                case "PER":
                case "PRI":
                case "URY":
                case "VEN":
                    comboBoxPrimaryLanguage.Text = "ES - Spanish";
                    break;
                case "FRA":
                case "FXX":
                case "GUF":
                case "PYF":
                case "ATF":
                case "MCO":
                case "LUX":
                case "HTI":
                case "VUT":
                case "MRT":
                case "MLI":
                case "NER":
                case "TCD":
                case "BFA":
                case "SEN":
                case "BEN":
                case "CIV":
                case "GIN":
                case "TGO":
                case "RWA":
                case "BDI":
                case "CMR":
                case "GAB":
                case "COM":
                case "MDG":
                case "MUS":
                case "SYC":
                case "DZA":
                case "MAR":
                case "TUN":
                case "AND":
                case "VNM":
                case "KHM":
                case "LAO":
                    comboBoxPrimaryLanguage.Text = "FR - French";
                    break;
                case "AGO":
                case "BRA":
                case "PRT":
                case "CPV":
                case "MOZ":
                case "GNB":
                case "STP":
                case "MAC":
                    comboBoxPrimaryLanguage.Text = "PT - Portuguese";
                    break;
                case "CHN":
                case "TWN":
                case "SGP":
                    comboBoxPrimaryLanguage.Text = "ZHCN - Chinese (Simplified)";
                    break;
                case "DEU":
                    comboBoxPrimaryLanguage.Text = "DE - German";
                    break;
                case "NLD":
                case "BEL":
                case "SUR":
                case "ABW":
                    comboBoxPrimaryLanguage.Text = "NL - Dutch";
                    break;
                case "AUS":
                    comboBoxPrimaryLanguage.Text = "AU - Australian";
                    break;
                case "CAN":
                case "USA":
                    comboBoxPrimaryLanguage.Text = "US - American";
                    break;
                default:
                    comboBoxPrimaryLanguage.Text = "EN - English";
                    break;
            }

            ChangeLanguageValue();
        }


        private void SaveLanguageValue()
        {
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);

            inifile.IniWriteValue("Languages", "Lang1", comboBoxLang1.Text);
            inifile.IniWriteValue("Languages", "Lang2", comboBoxLang2.Text);
            inifile.IniWriteValue("Languages", "Lang3", comboBoxLang3.Text);
            inifile.IniWriteValue("Languages", "Lang4", comboBoxLang4.Text);
            inifile.IniWriteValue("Languages", "Lang5", comboBoxLang5.Text);
            inifile.IniWriteValue("Languages", "Lang6", comboBoxLang6.Text);
            inifile.IniWriteValue("Languages", "Lang7", comboBoxLang7.Text);
            inifile.IniWriteValue("Languages", "Lang8", comboBoxLang8.Text);

            inifile.IniWriteValue("Languages", "PrimaryLanguage", comboBoxPrimaryLanguage.Text);

            if (comboBoxGameTDBLanguage.Text == "")
                comboBoxGameTDBLanguage.Text = SearchLanguageForApplication("wiitdb");               

            inifile.IniWriteValue("Languages", "GameTDB_language", comboBoxGameTDBLanguage.Text);

            if (checkBoxDownloadGameTDBPack.Checked)
                inifile.IniWriteValue("Languages", "Enable_GameTDB_download", "True");
            else
                inifile.IniWriteValue("Languages", "Enable_GameTDB_download", "False");
        }

        private void ChangeLanguageValue()
        {
            switch (comboBoxPrimaryLanguage.Text)
            {
                case "EN - English":
                    comboBoxLang1.Text = "EN - English";
                    comboBoxLang2.Text = "US - American";
                    comboBoxLang3.Text = "FR - French";
                    comboBoxLang4.Text = "DE - German";
                    comboBoxLang5.Text = "ES - Spanish";
                    comboBoxLang6.Text = "PT - Portuguese";
                    comboBoxLang7.Text = "IT - Italian";
                    comboBoxLang8.Text = "JA - Japanese";
                    comboBoxGameTDBLanguage.Text = "EN - English";
                    break;
                case "US - American":
                    comboBoxLang1.Text = "US - American";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "FR - French";
                    comboBoxLang4.Text = "DE - German";
                    comboBoxLang5.Text = "ES - Spanish";
                    comboBoxLang6.Text = "PT - Portuguese";
                    comboBoxLang7.Text = "IT - Italian";
                    comboBoxLang8.Text = "JA - Japanese";
                    comboBoxGameTDBLanguage.Text = "EN - English";
                    break;
                case "NL - Dutch":
                    comboBoxLang1.Text = "NL - Dutch";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "US - American";
                    comboBoxLang4.Text = "FR - French";
                    comboBoxLang5.Text = "DE - German";
                    comboBoxLang6.Text = "ES - Spanish";
                    comboBoxLang7.Text = "PT - Portuguese";
                    comboBoxLang8.Text = "IT - Italian";
                    comboBoxGameTDBLanguage.Text = "NL - Dutch";
                    break;
                case "DE - German":
                    comboBoxLang1.Text = "DE - German";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "US - American";
                    comboBoxLang4.Text = "FR - French";
                    comboBoxLang5.Text = "NL - Dutch";
                    comboBoxLang6.Text = "ES - Spanish";
                    comboBoxLang7.Text = "PT - Portuguese";
                    comboBoxLang8.Text = "IT - Italian";
                    comboBoxGameTDBLanguage.Text = "DE - German";
                    break;
                case "ZHCN - Chinese (Simplified)":
                    comboBoxLang1.Text = "ZHCN - Chinese (Simplified)";
                    comboBoxLang2.Text = "ZHTW - Chinese (Traditional)";
                    comboBoxLang3.Text = "EN - English";
                    comboBoxLang4.Text = "US - American";
                    comboBoxLang5.Text = "JA - Japanese";
                    comboBoxLang6.Text = "FR - French";
                    comboBoxLang7.Text = "DE - German";
                    comboBoxLang8.Text = "ES - Spanish";
                    comboBoxGameTDBLanguage.Text = "ZHCN - Chinese (Simplified)";
                    break;
                case "ZHTW - Chinese (Traditional)":
                    comboBoxLang1.Text = "ZHTW - Chinese (Traditional)";
                    comboBoxLang2.Text = "ZHCN - Chinese (Simplified)";
                    comboBoxLang3.Text = "EN - English";
                    comboBoxLang4.Text = "US - American";
                    comboBoxLang5.Text = "JA - Japanese";
                    comboBoxLang6.Text = "FR - French";
                    comboBoxLang7.Text = "DE - German";
                    comboBoxLang8.Text = "ES - Spanish";
                    comboBoxGameTDBLanguage.Text = "ZHTW - Chinese (Traditional)";
                    break;
                case "JA - Japanese":
                    comboBoxLang1.Text = "JA - Japanese";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "US - American";
                    comboBoxLang4.Text = "ZHTW - Chinese (Traditional)";
                    comboBoxLang5.Text = "ZHCN - Chinese (Simplified)";
                    comboBoxLang6.Text = "FR - French";
                    comboBoxLang7.Text = "DE - German";
                    comboBoxLang8.Text = "ES - Spanish";
                    comboBoxGameTDBLanguage.Text = "JA - Japanese";
                    break;
                case "FR - French":
                    comboBoxLang1.Text = "FR - French";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "US - American";
                    comboBoxLang4.Text = "DE - German";
                    comboBoxLang5.Text = "ES - Spanish";
                    comboBoxLang6.Text = "PT - Portuguese";
                    comboBoxLang7.Text = "IT - Italian";
                    comboBoxLang8.Text = "JA - Japanese";
                    comboBoxGameTDBLanguage.Text = "FR - French";
                    break;
                case "ES - Spanish":
                    comboBoxLang1.Text = "ES - Spanish";
                    comboBoxLang2.Text = "PT - Portuguese";
                    comboBoxLang3.Text = "EN - English";
                    comboBoxLang4.Text = "US - American";
                    comboBoxLang5.Text = "IT - Italian";
                    comboBoxLang6.Text = "FR - French";
                    comboBoxLang7.Text = "DE - German";
                    comboBoxLang8.Text = "JA - Japanese";
                    comboBoxGameTDBLanguage.Text = "ES - Spanish";
                    break;
                case "PT - Portuguese":
                    comboBoxLang1.Text = "PT - Portuguese";
                    comboBoxLang2.Text = "ES - Spanish";
                    comboBoxLang3.Text = "EN - English";
                    comboBoxLang4.Text = "US - American";
                    comboBoxLang5.Text = "IT - Italian";
                    comboBoxLang6.Text = "FR - French";
                    comboBoxLang7.Text = "DE - German";
                    comboBoxLang8.Text = "JA - Japanese";
                    comboBoxGameTDBLanguage.Text = "PT - Portuguese";
                    break;
                case "IT - Italian":
                    comboBoxLang1.Text = "IT - Italian";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "US - American";
                    comboBoxLang4.Text = "FR - French";
                    comboBoxLang5.Text = "DE - German";
                    comboBoxLang6.Text = "ES - Spanish";
                    comboBoxLang7.Text = "PT - Portuguese";
                    comboBoxLang8.Text = "JA - Japanese";
                    comboBoxGameTDBLanguage.Text = "IT - Italian";
                    break;
                case "AU - Australian":
                    comboBoxLang1.Text = "AU - Australian";
                    comboBoxLang2.Text = "EN - English";
                    comboBoxLang3.Text = "US - American";
                    comboBoxLang4.Text = "FR - French";
                    comboBoxLang5.Text = "DE - German";
                    comboBoxLang6.Text = "ES - Spanish";
                    comboBoxLang7.Text = "PT - Portuguese";
                    comboBoxLang8.Text = "IT - Italian";
                    comboBoxGameTDBLanguage.Text = "EN - English";
                    break;
            }
        }


        private void SettingsCheck()
        {
            if (!File.Exists(SETTINGS_INI_FILE))
                CreateSettingsFile();

            ReadSettingsFile();
        }

        private void ReadSettingsFile()
        {
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);

            textBoxGX_2D.Text = inifile.IniReadValue("GX", "2D");
            textBoxGX_3D.Text = inifile.IniReadValue("GX", "3D");
            textBoxGX_disc.Text = inifile.IniReadValue("GX", "disc");
            textBoxGX_full.Text = inifile.IniReadValue("GX", "full");

            textBoxWiiflow_2D.Text = inifile.IniReadValue("WIIFLOW", "2D");
            textBoxWiiflow_full.Text = inifile.IniReadValue("WIIFLOW", "full");

            textBoxCFG_2D.Text = inifile.IniReadValue("CFG", "2D");
            textBoxCFG_3D.Text = inifile.IniReadValue("CFG", "3D");
            textBoxCFG_disc.Text = inifile.IniReadValue("CFG", "disc");
            textBoxCFG_full.Text = inifile.IniReadValue("CFG", "full");

            if (inifile.IniReadValue("GX", "homebrew_path").Trim() == "")
                inifile.IniWriteValue("GX", "homebrew_path", CombinePath("apps", "usbloader_gx"));
            if (inifile.IniReadValue("WIIFLOW", "homebrew_path").Trim() == "")
                inifile.IniWriteValue("WIIFLOW", "homebrew_path", CombinePath("apps", "wiiflow"));
            if (inifile.IniReadValue("CFG", "homebrew_path").Trim() == "")
                inifile.IniWriteValue("CFG", "homebrew_path", CombinePath("apps", "usbloader"));

            textBoxGX_app_path.Text = inifile.IniReadValue("GX", "homebrew_path");
            textBoxWiiflow_app_path.Text = inifile.IniReadValue("WIIFLOW", "homebrew_path");
            textBoxCFG_app_path.Text = inifile.IniReadValue("CFG", "homebrew_path");                        
        }

        private bool ReadPathDeviceSettingsFile()
        {
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);

            if (inifile.IniReadValue("WIIGAMES", "StaticPath").Trim() == "")
                return false;

            if (inifile.IniReadValue("WIIGAMES", "StaticPath") == "True")
                checkBoxWiiGames.Checked = true;
            if (inifile.IniReadValue("GAMECUBE", "StaticPath") == "True")
                checkBoxGameCube.Checked = true;
            if (inifile.IniReadValue("EMUNAND", "StaticPath") == "True")
                checkBoxNAND.Checked = true;

            textBoxWiiGamesPath.Text = inifile.IniReadValue("WIIGAMES", "path");
            textBoxGameCubePath.Text = inifile.IniReadValue("GAMECUBE", "path");
            textBoxNANDPath.Text = inifile.IniReadValue("EMUNAND", "path");

            return true;
        }

        private void CreateSettingsFile()
        {

            IniFile ini = new IniFile(SETTINGS_INI_FILE);

            ini.IniWriteValue("GX", "homebrew_path", CombinePath("apps", "usbloader_gx"));
            ini.IniWriteValue("GX", "2D", CombinePath("apps", "usbloader_gx", "images", "2D"));
            ini.IniWriteValue("GX", "3D", CombinePath("apps", "usbloader_gx", "images"));
            ini.IniWriteValue("GX", "full", CombinePath("apps", "usbloader_gx", "images", "full"));
            ini.IniWriteValue("GX", "disc", CombinePath("apps", "usbloader_gx", "images", "disc"));

            ini.IniWriteValue("WIIFLOW", "homebrew_path", CombinePath("apps", "wiiflow"));
            ini.IniWriteValue("WIIFLOW", "full", CombinePath("wiiflow", "boxcovers"));
            ini.IniWriteValue("WIIFLOW", "2D", CombinePath("wiiflow", "covers"));

            ini.IniWriteValue("CFG", "homebrew_path", CombinePath("apps", "usbloader"));
            ini.IniWriteValue("CFG", "2D", CombinePath("usb-loader", "covers", "2d"));
            ini.IniWriteValue("CFG", "3D", CombinePath("usb-loader", "covers", "3d"));
            ini.IniWriteValue("CFG", "full", CombinePath("usb-loader", "covers", "full"));
            ini.IniWriteValue("CFG", "disc", CombinePath("usb-loader", "covers", "disc"));            
        }

        private void CreatePathDeviceSettingsFile()
        {

            IniFile ini = new IniFile(SETTINGS_INI_FILE);

            ini.IniWriteValue("WIIGAMES", "path", "wbfs");
            ini.IniWriteValue("WIIGAMES", "StaticPath", "True");
            ini.IniWriteValue("GAMECUBE", "path", "games");
            ini.IniWriteValue("GAMECUBE", "StaticPath", "True");
            ini.IniWriteValue("EMUNAND", "path", "nand");
            ini.IniWriteValue("EMUNAND", "StaticPath", "True");

            ReadPathDeviceSettingsFile();

        }

        private void ToolsCheck()
        {
            string errorMsg = "WiiCoverDownloader will be closed.\n\r\n\rSorry but probably is only a problem on GoogleCode server,\n\rand I can't do anything for resolve this.\n\rI can suggest you to try again later.";


            if (!ToolsFileCheck(TOOLS_PATH, "7za920.exe"))
            {
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading 7za920.exe...";

                if (!executeDownload(_7za920_Link,
                                     CombinePath(TOOLS_PATH, "7za920.exe"),
                                     false))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "sleep.exe") && !ToolsFileCheck(TOOLS_PATH, "sleep.exe") && !ToolsFileCheck(TOOLS_PATH, "wit.exe"))
            {
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading tools.zip...";

                if (!executeDownload(tools_Link,
                                     CombinePath(TOOLS_PATH, "tools.zip"),
                                     false))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }

                if (!ToolsFileCheck(TOOLS_PATH, "tools.zip"))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }

                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...extracting tools.zip...";

                if (!UnZip(CombinePath(TOOLS_PATH, "tools.zip"), TOOLS_PATH))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }

                FileDelete(CombinePath(TOOLS_PATH, "tools.zip"));


            }

            if (!ToolsFileCheck(TOOLS_PATH, "sleep.exe"))
            {
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading sleep.exe...";

                if (!executeDownload(sleep_Link,
                                     CombinePath(TOOLS_PATH, "sleep.exe"),
                                     false))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "wbfs_file.exe"))
            {
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading wbfs_file.exe...";
                if (!executeDownload(wbfs_file_Link,
                                     CombinePath(TOOLS_PATH, "wbfs_file.exe"),
                                     false))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }

            if (!ToolsFileCheck(TOOLS_PATH, "wit.exe"))
            {
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading wit.exe...";
                if (!executeDownload(wit_Link,
                                     CombinePath(TOOLS_PATH, "wit.exe"),
                                     false))
                {
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }
        }

        private bool ToolsFileCheck(string path, string fileToCheck)
        {
            string md5 = "";
            if (!File.Exists(CombinePath(path, fileToCheck)))
                return false;

            switch (fileToCheck)
            {
                case "7za920.exe":
                    md5 = "42badc1d2f03a8b1e4875740d3d49336";
                    break;
                case "tools.zip":
                    md5 = "343e2184de8e72543908396b295192ad";
                    break;
                case "sleep.exe":
                    md5 = "1a1075e5e307f3a4b8527110a51ce827";
                    break;
                case "wbfs_file.exe":
                    md5 = "39f6c1ce581e078fc29e75549486d89e";
                    break;
                case "wit.exe":
                    md5 = "ce4028b2dfd26e7c5c2aaf3d2e3c43a9";
                    break;
                default:
                    return false;
            }

            string md5_from_file = GetMD5HashFromFile(CombinePath(path, fileToCheck));

            if (md5_from_file != md5)
                return false;
            return true;
        }

        protected string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private void VersionCheck()
        {
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);
            string[] versionFile;
            string actualLangVersion = inifile.IniReadValue("Languages", "LanguageFileVersion");
            string lastAppVersion = "";
            string lastLangVersion = "";

            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...Checking for updates...";

            if (!executeDownload("http://code.google.com/p/wii-cover-downloader/downloads/list?can=3&q=&colspec=Filename+Summary+Uploaded+ReleaseDate+Size+DownloadCount",
                                CombinePath(SETTINGS_PATH, "lastVersion.txt"),
                                false))
                return;
                        
            if(!File.Exists(CombinePath(SETTINGS_PATH, "lastVersion.txt")))
                return;        
                      
            // serching last app version
            versionFile = File.ReadAllLines(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
            foreach (string line in versionFile)
            {
                if (line.Trim() == "")
                    continue;
                if (!line.Contains("WiiCoverDownloader v"))
                    continue;
                if (!line.Contains(".zip"))
                    continue;

                lastAppVersion = line;

                lastAppVersion = lastAppVersion.Replace("WiiCoverDownloader v", "");
                lastAppVersion = lastAppVersion.Replace(".zip", "");
                lastAppVersion = lastAppVersion.Trim();
                break;
            }
            if (lastAppVersion != VERSION)
                DoApplicationUpdate(lastAppVersion);

            // serching last lang version
            versionFile = File.ReadAllLines(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
            foreach (string line in versionFile)
            {
                if (line.Trim() == "")
                    continue;
                if (!line.Contains("languages v"))
                    continue;
                if (!line.Contains(".ini"))
                    continue;

                lastLangVersion = line;

                lastLangVersion = lastLangVersion.Replace("languages v", "");
                lastLangVersion = lastLangVersion.Replace(".ini", "");
                lastLangVersion = lastLangVersion.Trim();
                break;
            } 
            if ((lastLangVersion != actualLangVersion) || !File.Exists(LANGUAGES_FILE))
                UpdateLanguagesFile(lastLangVersion);

            FileDelete(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
        }

        private bool DoApplicationUpdate(string new_version)
        {
            FileDelete(CombinePath(SETTINGS_PATH, "lastVersion.txt"));
            CreateBatchFileForUpdate();
            FileDelete(CombinePath(STARTUP_PATH, "NewWiiCoverDownloader.exe"));
            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading new version...";

            if (!executeDownload(NewWiiCoverDownloaderZip_Link + new_version + ".zip",
                                 CombinePath(TOOLS_PATH, "NewWiiCoverDownloader.zip"),
                                 false))
                return false;

            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...extracting new version...";

            if (!UnZip(CombinePath(TOOLS_PATH, "NewWiiCoverDownloader.zip"), TOOLS_PATH))
            {
                FileDelete(CombinePath(TOOLS_PATH, "NewWiiCoverDownloader.zip"));
                return false;
            }

            FileCopy(CombinePath(TOOLS_PATH, "WiiCoverDownloader", "WiiCoverDownloader.exe"), CombinePath(STARTUP_PATH, "NewWiiCoverDownloader.exe"), true);
            FileDelete(CombinePath(TOOLS_PATH, "NewWiiCoverDownloader.zip"));
            DirectoryClean(CombinePath(TOOLS_PATH, "WiiCoverDownloader"));
            Directory.Delete(CombinePath(TOOLS_PATH, "WiiCoverDownloader"));

            ProcessStartInfo p_updater;
            Process UpdateProcess;

            p_updater = new ProcessStartInfo("cmd.exe", "/c \"" + CombinePath(STARTUP_PATH, "WiiCoverDownloaderUpdater.bat") + "\"");

            p_updater.UseShellExecute = false;
            p_updater.RedirectStandardOutput = false;
            p_updater.CreateNoWindow = false;
            p_updater.WorkingDirectory = STARTUP_PATH;
            p_updater.WindowStyle = ProcessWindowStyle.Normal;   // qui la finestra di DOS DEVO vederla ^__^    

            UpdateProcess = Process.Start(p_updater);

            Environment.Exit(0);

            return true;

        }


        private void FolderCheck()
        {
            if (!Directory.Exists(SETTINGS_PATH))
                Directory.CreateDirectory(SETTINGS_PATH);

            if (!Directory.Exists(TOOLS_PATH))
                Directory.CreateDirectory(TOOLS_PATH);

            if (!Directory.Exists(DOWNLOAD_PATH))
                Directory.CreateDirectory(DOWNLOAD_PATH);

            if (!Directory.Exists(TEMP_PATH))
                Directory.CreateDirectory(TEMP_PATH);

            if (!Directory.Exists(CACHE_PATH))
                Directory.CreateDirectory(CACHE_PATH);


        }



        private bool CheckForDevice()
        {
            this.Refresh();
            this.Enabled = false;
            FORCE_ROOT_CHANGE = false;

            TEMP_WII_GAMES_COUNT = TEMP_GAMECUBE_GAMES_COUNT = 0;

            comboBoxLoaderDevice.Items.Clear();
            comboBoxWiiGamesDevice.Items.Clear();
            comboBoxGameCubeDevice.Items.Clear();
            comboBoxNandDevice.Items.Clear();

            bool DeviceFound = false;
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo d in drives)
            {            
                {
                    switch (d.DriveType)
                    {
                        case DriveType.Fixed:
                            break;
                        case DriveType.Removable:
                            break;
                        default:
                            continue;
                    }

                    if (d.IsReady)
                    {
                        if (d.VolumeLabel.Trim() != "")
                        {
                            comboBoxWiiGamesDevice.Items.Add(d.Name + " [" + d.VolumeLabel + ']');
                            comboBoxGameCubeDevice.Items.Add(d.Name + " [" + d.VolumeLabel + ']');
                            comboBoxNandDevice.Items.Add(d.Name + " [" + d.VolumeLabel + ']');
                        }
                        else
                        {
                            comboBoxWiiGamesDevice.Items.Add(d.Name);
                            comboBoxGameCubeDevice.Items.Add(d.Name);
                            comboBoxNandDevice.Items.Add(d.Name);
                        }
                    }
                    else
                    {
                        if (WBFS_CHECK(d.Name, true))
                            comboBoxWiiGamesDevice.Items.Add(d.Name + " [WBFS partition]");
                    }

                    if (Environment.SystemDirectory.Substring(0, 1) != d.Name.Substring(0, 1)) // no search of Loader in pc windows root...                        
                    {
                        if (d.IsReady)
                        {
                            if (d.VolumeLabel.Trim() != "")
                                comboBoxLoaderDevice.Items.Add(d.Name + " [" + d.VolumeLabel + ']');
                            else
                                comboBoxLoaderDevice.Items.Add(d.Name);

                            DeviceFound = true;
                        }
                    }
                }
            }

            if (!DeviceFound)
            {
                MessageBox.Show("Error", "No device found for any Loader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBoxLoaderDevice.Update();
                comboBoxWiiGamesDevice.Update();
                comboBoxGameCubeDevice.Update();
                comboBoxNandDevice.Update();
                this.Enabled = true;
                this.Refresh();
                return false;
            }
            else
            {
                this.Enabled = true;
                this.Refresh();

                comboBoxLoaderDevice.Update();
                comboBoxWiiGamesDevice.Update();
                comboBoxGameCubeDevice.Update();
                comboBoxNandDevice.Update();

                return true;
            }
        }

        private bool executeCommand(string program, string arguments, string WorkingDirectory, string Text4Error)
        {
            MySleep(10);
            if (!DOWNLOAD_OR_EXE_WORKING)
                return false;
            try
            {
                ProcessStartInfo my_command;
                Process MyCommandProcess;

                my_command = new ProcessStartInfo("cmd.exe", "/c \"" + program + " " + arguments + "\"");

                my_command.UseShellExecute = false;
                my_command.RedirectStandardOutput = false;
                my_command.RedirectStandardInput = false;
                my_command.RedirectStandardError = false;
                my_command.CreateNoWindow = true;
                my_command.WindowStyle = ProcessWindowStyle.Hidden;              
                my_command.WorkingDirectory = STARTUP_PATH;
                
                MyCommandProcess = Process.Start(my_command);
                MyCommandProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + Text4Error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }


        private bool WBFS_CHECK(string drive, bool onlyCheck)
        {
            string program = "\"" + CombinePath(TOOLS_PATH, "wbfs_file.exe") + "\"";

            FileDelete(GAMES_ID_FILE);

            if (!executeCommand(program,
                                drive.Substring(0, 2) + " make_info > \"" + GAMES_ID_FILE + "\"",
                                TOOLS_PATH,
                                "Unespcted ERROR using wbfs_file.exe..."))
                return false;

            if (!File.Exists(GAMES_ID_FILE))
                return false;

            if (onlyCheck)
            {
                if (!first_line_check(GAMES_ID_FILE))
                {
                    FileDelete(GAMES_ID_FILE);
                    return false;
                }
            }
            else
            {
                if (!make_game_count(GAMES_ID_FILE))
                {
                    FileDelete(GAMES_ID_FILE);
                    return false;
                }
            }

            return true;
        }

        private bool make_game_count(string outputFile)
        {
            string[] file = File.ReadAllLines(outputFile);

            foreach (string line in file)
            {
                if (line.Contains("Error:") || line.Contains("bad magic: No error"))
                    continue;

                if (line.Trim() == "")
                    break;

                WII_GAMES_COUNT++;
            }

            if (WII_GAMES_COUNT == 0)
                return false;
            else
                return true;
        }

        private bool first_line_check(string outputFile)
        {

            string[] file = File.ReadAllLines(outputFile);

            foreach (string line in file)
            {
                if (line.Contains("Error:") || line.Contains("bad magic: No error"))
                    return false;
                else
                    return true;
            }

            return true;
        }


        private void comboBoxGamesDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetProgressBar();

            if (FORCE_ROOT_CHANGE)
            {
                FORCE_ROOT_CHANGE = false;
                return;
            }

            if (comboBoxWiiGamesDevice.Text.Contains(" [WBFS partition]"))
                textBoxGamesFolder.Text = "WBFS partition detected";
            else
            {

                if (comboBoxWiiGamesDevice.Text.Trim() == "")
                {
                    textBoxGamesFolder.Text = "";
                    return;
                }

                if (StaticPathValid("WiiGames"))
                    return;

                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
                folderBrowserDialog.SelectedPath = comboBoxWiiGamesDevice.Text.Substring(0, 3) + "$RECYCLE.BIN";
                DialogResult result = folderBrowserDialog.ShowDialog();

                string tempPath = "";
                if (result == DialogResult.OK)
                    tempPath = folderBrowserDialog.SelectedPath;

                if (tempPath.Length < 4)
                {
                    textBoxGamesFolder.Text = "";
                    comboBoxWiiGamesDevice.SelectedIndex = -1;
                }
                else
                {
                    textBoxGamesFolder.Text = tempPath;
                    string selected_root = Path.GetPathRoot(textBoxGamesFolder.Text);
                    if (selected_root != comboBoxWiiGamesDevice.Text.Substring(0, 3))
                    {
                        FORCE_ROOT_CHANGE = true;

                        foreach (string device_selected in comboBoxWiiGamesDevice.Items)
                        {
                            if (device_selected.Substring(0, 3) == selected_root)
                                comboBoxWiiGamesDevice.Text = device_selected;
                        }
                    }
                }
            }
        }

        private bool StaticPathValid(string games_type)
        {
            string path_to_check;
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);

            switch (games_type)
            {
                case "WiiGames":
                    if (inifile.IniReadValue("WIIGAMES", "StaticPath") == "False")
                        return false;
                    path_to_check = CombinePath(comboBoxWiiGamesDevice.Text.Substring(0, 3), inifile.IniReadValue("WIIGAMES", "path"));
                    if (Directory.Exists(path_to_check))
                    {
                        textBoxGamesFolder.Text = path_to_check;
                        return true;
                    }
                    break;
                case "GameCubeGames":
                    if (inifile.IniReadValue("GAMECUBE", "StaticPath") == "False")
                        return false;
                    path_to_check = CombinePath(comboBoxGameCubeDevice.Text.Substring(0, 3), inifile.IniReadValue("GAMECUBE", "path"));
                    if (Directory.Exists(path_to_check))
                    {
                        textBoxGameCubeFolder.Text = path_to_check;
                        return true;
                    }
                    break;
                case "EmuNandTitles":
                    if (inifile.IniReadValue("EMUNAND", "StaticPath") == "False")
                        return false;
                    path_to_check = CombinePath(comboBoxNandDevice.Text.Substring(0, 3), inifile.IniReadValue("EMUNAND", "path"));
                    if (Directory.Exists(path_to_check))
                    {
                        textBoxNandFolder.Text = path_to_check;
                        return true;
                    }
                    break;
            }
            return false;
        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            ReadLanguageFile();
        }

        private void buttonSavePath_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);

            ini.IniWriteValue("GX", "homebrew_path", textBoxGX_app_path.Text);
            ini.IniWriteValue("GX", "2D", textBoxGX_2D.Text);
            ini.IniWriteValue("GX", "3D", textBoxGX_3D.Text);
            ini.IniWriteValue("GX", "full", textBoxGX_full.Text);
            ini.IniWriteValue("GX", "disc", textBoxGX_disc.Text);

            ini.IniWriteValue("WIIFLOW", "homebrew_path", textBoxWiiflow_app_path.Text);
            ini.IniWriteValue("WIIFLOW", "2D", textBoxWiiflow_2D.Text);
            ini.IniWriteValue("WIIFLOW", "full", textBoxWiiflow_full.Text);

            ini.IniWriteValue("CFG", "homebrew_path", textBoxCFG_app_path.Text);
            ini.IniWriteValue("CFG", "2D", textBoxCFG_2D.Text);
            ini.IniWriteValue("CFG", "3D", textBoxCFG_3D.Text);
            ini.IniWriteValue("CFG", "full", textBoxCFG_full.Text);
            ini.IniWriteValue("CFG", "disc", textBoxCFG_disc.Text); 
        }

        private void buttonCancelPath_Click(object sender, EventArgs e)
        {
            ReadSettingsFile();
        }

        public void MySleep(int milliseconds)
        {
            bool timeElapsed = false;
            var TimerForSleep = new System.Timers.Timer(milliseconds);
            TimerForSleep.Elapsed += (s, e) => timeElapsed = true;
            TimerForSleep.Start();

            while (!timeElapsed)
                Application.DoEvents();

            Thread.Sleep(0);
        }

        private void AppendText(string message)
        {
            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                return;

            if (message.Length + richTextBoxInfo.TextLength > richTextBoxInfo.MaxLength)
            {
                // Alert user text is too large.
                MessageBox.Show("The text is too large to add to the RichTextBox.\n Now RichTextBox will be cleaned and download will continue without problems.");
                richTextBoxInfo.Clear();
            }

            richTextBoxInfo.AppendText(message);
            richTextBoxInfo.SelectionStart = richTextBoxInfo.Text.Length;
            richTextBoxInfo.ScrollToCaret();
        }

        private void NetworkCheck()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return;

            MessageBox.Show("Network connection not available. WiiCoverDownloader will be closed.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);

        }

        public void EventForDownload(Object myObject, EventArgs myEventArgs)
        {
            if (!USE_PROGRESS_BAR)
            {
                AppendText(".");
                PASSAGE_COUNT++;
                if (PASSAGE_COUNT % 20 == 0)
                    AppendText(" ");
                if (PASSAGE_COUNT * TYMER_INTERVAL > MAX_DELAY_FOR_TYMER)
                    TIME_OUT = true;
            }
            else
            {
                // check for fileszie
                System.IO.FileInfo fileinfo = new System.IO.FileInfo(FILE_TO_DOWNLOAD);

                if (ACTUAL_FILE_SIZE == fileinfo.Length)
                    PASSAGE_COUNT++;
                else
                {
                    PASSAGE_COUNT = 0;
                    ACTUAL_FILE_SIZE = fileinfo.Length;
                }

                if (fileinfo.Length > 0)
                {
                    long longPercent = (fileinfo.Length * 100) / FINAL_FILE_SIZE;
                    int percent = unchecked((int)longPercent);

                    if (percent > 99 && percent > 0)
                        progressBarForDownload.Value = 100;
                    else if (percent < 1)
                        progressBarForDownload.Value = 0;
                    else
                    {
                        progressBarForDownload.Value = percent;

                        labelProgrssBar.Text = (fileinfo.Length / 1024).ToString("0,0", System.Globalization.CultureInfo.CreateSpecificCulture("el-GR")) +
                                               " KB / " +
                                               (FINAL_FILE_SIZE / 1024).ToString("0,0", System.Globalization.CultureInfo.CreateSpecificCulture("el-GR")) +
                                               " KB (" + percent + "%)";
                    }
                }
            }

        }

        public bool executeDownload(string url, string file_name, bool usePorgressBar)
        {
            MySleep(10);
            DirectoryClean(DOWNLOAD_PATH);
            FileDelete(file_name);

            TYMER_INTERVAL = 500; // controllo i dati scaricati ogni mezzo secondo   
            PASSAGE_COUNT = 0;
            FILE_TO_DOWNLOAD = CombinePath(TEMP_PATH, Path.GetFileName(file_name));

            TIME_OUT = false;

            WebClient client = new WebClient();

            System.Windows.Forms.Timer myStandardDowloadTimer = new System.Windows.Forms.Timer();

            if (!Directory.Exists(TEMP_PATH))
                Directory.CreateDirectory(TEMP_PATH);

            try
            {
                HttpWebRequest httpWReq = (HttpWebRequest)HttpWebRequest.Create(url);
                HttpWebResponse httpWRes = (HttpWebResponse)httpWReq.GetResponse();
                if (usePorgressBar)
                {
                    USE_PROGRESS_BAR = true;
                    FINAL_FILE_SIZE = httpWRes.ContentLength;
                    ACTUAL_FILE_SIZE = 0;
                }
                else
                    USE_PROGRESS_BAR = false;

                httpWReq.Abort();
                httpWRes.Close();
            }
            catch
            {
                return false;
            }

            try
            {
                Uri URL = new Uri(url);

                myStandardDowloadTimer.Tick += new EventHandler(EventForDownload);
                myStandardDowloadTimer.Interval = TYMER_INTERVAL;

                client.DownloadFileAsync(URL, FILE_TO_DOWNLOAD);

                myStandardDowloadTimer.Start();

                while (DOWNLOAD_OR_EXE_WORKING == true && TIME_OUT == false && client.IsBusy)
                    Application.DoEvents();

                myStandardDowloadTimer.Stop();

            }
            catch// (Exception ex)
            {
                myStandardDowloadTimer.Stop();

                while (client.IsBusy == true)
                    client.CancelAsync();

                DirectoryClean(DOWNLOAD_PATH);

                return false;
            }

            if (TIME_OUT == true || DOWNLOAD_OR_EXE_WORKING == false)
            {
                while (client.IsBusy == true)
                    client.CancelAsync();

                DirectoryClean(DOWNLOAD_PATH);

                return false;
            }

            if (!File.Exists(FILE_TO_DOWNLOAD))
                return false;

            // check for fileszie
            System.IO.FileInfo fileinfo = new System.IO.FileInfo(FILE_TO_DOWNLOAD);
            if (fileinfo.Length == 0)
            {
                File.Delete(FILE_TO_DOWNLOAD);
                return false;
            }

            FileCopy(FILE_TO_DOWNLOAD, file_name, true);

            if (usePorgressBar)
                ResetProgressBar();

            return true;
        }


        private bool Download_GameTDB_Pack()
        {
            string XML_FILE = CombinePath(TOOLS_PATH, "wiitdb.xml");

            IniFile inifile = new IniFile(SETTINGS_INI_FILE);
            string lang_to_use = inifile.IniReadValue("Languages", "GameTDB_language");
           
            if (lang_to_use.Length > 3)
            {
                if (lang_to_use.Contains("ZHCN") || lang_to_use.Contains("ZHTW"))
                    lang_to_use = "?LANG=" + lang_to_use.Substring(0, 4) + "&";
                else
                    lang_to_use = "?LANG=" + lang_to_use.Substring(0, 2) + "&";
            }
            else
                lang_to_use = "?"; // impossible case, but better do it for make sure to don't have crash in application


            string link_for_GAMETDB_ARCHIVE = "http://www.gametdb.com/wiitdb.zip" + lang_to_use + "FALLBACK=true&GAMECUBE=true&WIIWARE=true";

            AppendText(Dictionary.checkingWiitdbPackage);

            this.Refresh();

            if (FileExistAndUpdated(link_for_GAMETDB_ARCHIVE, GAMETDB_ARCHIVE, true))
            {
                AppendText("...Ok\n");
                this.Refresh();
                goto skipDownload;
            }
            AppendText("...Ok\n");

            this.Refresh();

            AppendText(Dictionary.downloading + " wiitdb.zip...");
            
            if (executeDownload(link_for_GAMETDB_ARCHIVE, GAMETDB_ARCHIVE, true))
                AppendText("...OK\n");
            else
            {
                if (DOWNLOAD_OR_EXE_WORKING)
                    AppendText("...error downloading wiitdb.zip\n");
                return false;
            }

            FileDelete(XML_FILE);

            if (!File.Exists(GAMETDB_ARCHIVE))
                return false;

        skipDownload:

            if (File.Exists(XML_FILE))
                goto skipExtract;

            if (GX_COVER || WIIFLOW_COVER)
            {
                AppendText(Dictionary.extracting + " wiitdb.zip...");

                if (!UnZip(GAMETDB_ARCHIVE, TOOLS_PATH))
                    return false;
            }

        skipExtract:

            AppendText(Dictionary.copyingGametdbPackage);

            if (GX_COVER)
                FileCopy(XML_FILE, CombinePath(comboBoxLoaderDevice.Text.Substring(0, 3), "apps", "usbloader_gx", "wiitdb.xml"), false);
            if (WIIFLOW_COVER)
                FileCopy(XML_FILE, CombinePath(comboBoxLoaderDevice.Text.Substring(0, 3), "wiiflow", "settings", "wiitdb.xml"), false);
            if (CFG_COVER)
            {
                FileCopy(TITLES_FILE, CombinePath(comboBoxLoaderDevice.Text.Substring(0, 3), "usb-loader", "titles.txt"), false);
                FileCopy(GAMETDB_ARCHIVE, CombinePath(comboBoxLoaderDevice.Text.Substring(0, 3), "usb-loader", "wiitdb.zip"), false);
            }

            AppendText("...OK\n\n");


            return true;

        }


        private bool FileExistAndUpdated(string link_to_check, string file_to_check, bool check_only_for_size)
        {
            if (File.Exists(file_to_check))
            {
                FileInfo local_file = new FileInfo(file_to_check);

                try
                {
                    HttpWebRequest httpWReq = (HttpWebRequest)HttpWebRequest.Create(link_to_check);
                    HttpWebResponse httpWRes = (HttpWebResponse)httpWReq.GetResponse();                                                             
                    httpWReq.Abort();
                    httpWRes.Close();

                    if (check_only_for_size)
                    {
                        if (httpWRes.ContentLength == local_file.Length)
                            return true;
                    }
                    else
                    {
                        if (httpWRes.LastModified < local_file.LastWriteTime)
                            return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private bool FileExistAndUpdateToToday(string file_to_check)
        {
            if (File.Exists(file_to_check))
            {
                FileInfo titlesfile = new FileInfo(file_to_check);

                DateTime file_date = titlesfile.LastWriteTime;
                DateTime now_date = DateTime.Now;

                if (file_date.Date == now_date.Date)
                    return true;
            }
            return false;
        }

        static public bool CopyFolder(string sourcePath, string targetPath, bool remove_source)
        {
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);
            string[] files = Directory.GetFiles(sourcePath);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(targetPath, name);

                FileCopy(file, dest, remove_source);

            }
            string[] folders = Directory.GetDirectories(sourcePath);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(targetPath, name);
                CopyFolder(folder, dest, remove_source);
            }
            return true;
        }

        private bool UnZip(string file_to_extract, string destination_folder)
        {
            CleanFolder(TEMP_PATH);
            string program = "\"" + CombinePath(TOOLS_PATH, "7za920.exe") + "\"";
            if (!executeCommand(program,
                                    "x -y -o" + '\u0022' + TEMP_PATH + '\u0022' + " " + '\u0022' + file_to_extract + '\u0022',
                                    TOOLS_PATH,
                                    "Unexpected error while unpacking wiitdb.zip..."))
            {
                if (DOWNLOAD_OR_EXE_WORKING)
                    AppendText("...error\n");

                CleanFolder(DOWNLOAD_PATH);

                return false;
            }

            CopyFolder(TEMP_PATH, destination_folder, true);

            AppendText("...OK\n");
            return true;
        }

        private void TitlesFileCheck()
        {
            WiiGamesList.Clear();
            GameCubeGamesList.Clear();
            ChannelTitlesList.Clear();

            string errorMsg = "WiiCoverDownloader will be closed.\n\r\n\rSorry but probably is only a problem on GameTDB server,\n\rbut I can't do anything for resolve this.\n\rI can suggest you to try again later.";
            
            AppendText(Dictionary.checking + " titles.txt...");

            this.Refresh();

            IniFile inifile = new IniFile(SETTINGS_INI_FILE);
            string lang_to_use = inifile.IniReadValue("Languages", "GameTDB_language");

            if (lang_to_use.Length > 3)
            {
                if (lang_to_use.Contains("ZHCN") || lang_to_use.Contains("ZHTW"))
                    lang_to_use = "?LANG=" + lang_to_use.Substring(0, 4);
                else
                    lang_to_use = "?LANG=" + lang_to_use.Substring(0, 2);
            }
            else
                lang_to_use = ""; // impossible case, but better do it         

            if (FileExistAndUpdated("http://www.gametdb.com/titles.txt" + lang_to_use, TITLES_FILE, true))
            {
                AppendText("...Ok\n");
                this.Refresh();
                goto skipTitlesDownload;
            }

            AppendText("...Ok\n");

            this.Refresh();    

            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading titles.txt...";
            else
                AppendText(Dictionary.downloading + " titles.txt...");                    

            if (executeDownload("http://www.gametdb.com/titles.txt" + lang_to_use, TITLES_FILE, false))            
                AppendText("...OK\n");           
            else
            {
                AppendText("...error!\n" + errorMsg);
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
                return;
            }

            this.Refresh();

            if (!File.Exists(TITLES_FILE)) // impossible case
            {
                AppendText("...error!\n" + errorMsg);
                if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                    MessageBox.Show(errorMsg, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);    
                Environment.Exit(-1);
                return;
            }

            //  write array
        skipTitlesDownload:
            {
                string[] file = File.ReadAllLines(TITLES_FILE);
                TitleInfo titleLine;

                bool firstLineRead = false, ReadingWiiGames = true, ReadingCubeGames = false, ReadingChannelTitles = false;
                int ID_lenght = 6;

                foreach (string line in file)
                {
                    if (!firstLineRead)
                    {
                        firstLineRead = true;
                        continue;
                    }

                    if(line.Length<10)
                        continue; // impossible case.. if titles.txt is good

                    if (ReadingWiiGames)
                    {
                        if (line.Substring(4, 3) == " = ")
                        {
                            ReadingWiiGames = false;
                            ReadingChannelTitles = true;
                            ID_lenght = 4;
                        }
                    }

                    if (ReadingChannelTitles)
                    {
                        if(line.Substring(6, 3) == " = ")
                        {
                            ReadingChannelTitles = false;
                            ReadingCubeGames = true;
                            ID_lenght = 6;
                        }
                    }

                    titleLine.ID = line.Substring(0, ID_lenght);
                    titleLine.name = line.Substring(ID_lenght + 3, line.Length - ID_lenght - 3);

                    if (ReadingWiiGames)
                        WiiGamesList.Add(titleLine);
                    else if (ReadingChannelTitles)
                        ChannelTitlesList.Add(titleLine);
                    else if (ReadingCubeGames)
                        GameCubeGamesList.Add(titleLine);
                }
            }            

            return;

        }  
    
        

        private string IniLangCheck(string lang_to_use, string section, string defaulValue)
        {
            IniFile dic = new IniFile(LANGUAGES_FILE);

            if (lang_to_use == "default")
                return defaulValue;

            if (dic.IniReadValue(lang_to_use, section).Trim() == "")
                return defaulValue;

            return dic.IniReadValue(lang_to_use, section);
        }

        public static class Dictionary
        {
            public static string
                downloadCompleted,
                noLoaderFounded,
                loaderFounded,
                deviceNotMoreFounded,
                noValidTitleFounded,
                titlesCheckComplete,
                coversDownloadStart,
                imagesSearched,
                imagesAlreadyPresent,
                imagesFromCache,
                imagesDownloaded,
                imagesNotFounded,
                checkingWiitdbPackage,
                checkingWiiGamesTitles,
                checkingGameCubeTitles,
                checkingEmulatedNandTitles,
                notFound,
                alreadyPresent,
                downloadStopByUser,
                readingFolder,
                founded,
                noWiiGamesFounded,
                zeroWbfsFileFounded,
                noGameCubeGamesFounded,
                noEmuNandTitlesFounded,
                checking,
                downloading,
                extracting,
                copyingGametdbPackage;
        }

        private void ReloadDictionary(string lang_to_use)
        {
            // menu
            buttonDownloadStart.Text = IniLangCheck(lang_to_use, "buttonDownloadStart", "Start Download");
            buttonStopDownload.Text = IniLangCheck(lang_to_use, "buttonStopDownload", "Stop Download");
            labelAppLang.Text = IniLangCheck(lang_to_use, "labelAppLang", "Application language");
            labelGameTDBInfo.Text = IniLangCheck(lang_to_use, "labelGameTDBInfo", "All covers will downloaded from");
            buttonReloadDevice.Text = IniLangCheck(lang_to_use, "buttonReloadDevice", "Reload device");             
            radioButtonDownloadOnlyMissing.Text = IniLangCheck(lang_to_use, "radioButtonDownloadOnlyMissing", "Download only missing covers");
            radioButtonDownloadAll.Text = IniLangCheck(lang_to_use, "radioButtonDownloadAll", "Redownload every cover");
            labelLoaderDevice.Text = IniLangCheck(lang_to_use, "labelLoaderDevice", "Loader device");
            labelWiiGamesDevice.Text = IniLangCheck(lang_to_use, "labelWiiGamesDevice", "Wii Games device");
            labelGameCubeDevice.Text = IniLangCheck(lang_to_use, "labelGameCubeDevice", "GameCube Games device");
            labelEmuNandDevice.Text = IniLangCheck(lang_to_use, "labelEmuNandDevice", "Emulated NAND device");            
            tabControl.TabPages[0].Text = IniLangCheck(lang_to_use, "coversDownload", "COVERS DOWNLOAD");
            tabControl.TabPages[1].Text = IniLangCheck(lang_to_use, "coversLanguagePreferences", "Covers language preferences");
            tabControl.TabPages[2].Text = IniLangCheck(lang_to_use, "loaderPathPreferences", "Loader path preferences");
            tabControl.TabPages[3].Text = IniLangCheck(lang_to_use, "devicePathPreferences", "Device path preferences");
            tabControl.TabPages[4].Text = IniLangCheck(lang_to_use, "infoAndCredits", "Info and credits");
            buttonSaveLang.Text = IniLangCheck(lang_to_use, "buttonSave", "Save settings");
            buttonCancelLang.Text = IniLangCheck(lang_to_use, "buttonCancel", "Cancel");
            buttonDefaulLang.Text = IniLangCheck(lang_to_use, "buttonSetDefault", "Set and save default settings");
            buttonSavePath.Text = IniLangCheck(lang_to_use, "buttonSave", "Save settings");
            buttonCancelPath.Text = IniLangCheck(lang_to_use, "buttonCancel", "Cancel");
            buttonDefaultLoaderPath.Text = IniLangCheck(lang_to_use, "buttonSetDefault", "Set and save default settings");
            buttonSavePathPreferences.Text = IniLangCheck(lang_to_use, "buttonSave", "Save settings");
            buttonCancelPathPreferences.Text = IniLangCheck(lang_to_use, "buttonCancel", "Cancel");
            buttonDefaultPathPreferences.Text = IniLangCheck(lang_to_use, "buttonSetDefault", "Set and save default settings");
            labelCoverFirstLanguage.Text = IniLangCheck(lang_to_use, "labelCoverFirstLanguage", "Change covers primary language");
            labelGameTDBlang.Text = IniLangCheck(lang_to_use, "labelGameTDBlang", "GameTDB database language");
            checkBoxDownloadGameTDBPack.Text = IniLangCheck(lang_to_use, "checkBoxDownloadGameTDBPack", "Download also GameTDB package");
            labelCoverSerachOrder.Text = IniLangCheck(lang_to_use, "labelCoverSerachOrder", "Choose covers search order");
            groupBoxWiiGames.Text = IniLangCheck(lang_to_use, "groupBoxWiiGames", "Wii Games");
            checkBoxWiiGames.Text = IniLangCheck(lang_to_use, "checkBoxWiiGames", "Use always this folder for search Wii games ( .wbfs files)");
            groupBoxGameCubeGames.Text = IniLangCheck(lang_to_use, "groupBoxGameCubeGames", "GameCube games");
            checkBoxGameCube.Text = IniLangCheck(lang_to_use, "checkBoxGameCube", "Use always this folder for search GameCube games ( .iso files )");
            groupBoxEmuNandTitles.Text = IniLangCheck(lang_to_use, "groupBoxEmuNandTitles", "Emulated NAND channels (like WiiWare and Virtual Console)");
            checkBoxNAND.Text = IniLangCheck(lang_to_use, "checkBoxNAND", "Use always this folder for search channels in emulated NAND");
            groupBoxInfoAndCredits.Text = IniLangCheck(lang_to_use, "groupBoxInfoAndCredits", "Informations and Credits");             

            // message
            Dictionary.downloadCompleted = IniLangCheck(lang_to_use, "downloadCompleted", "DOWNLOAD COMPLETED !!");
            Dictionary.loaderFounded = IniLangCheck(lang_to_use, "loaderFounded", "Loader founded:");
            Dictionary.noLoaderFounded = IniLangCheck(lang_to_use, "noLoaderFounded", "No loader founded in this device.");
            Dictionary.deviceNotMoreFounded = IniLangCheck(lang_to_use, "deviceNotMoreFounded", "Some of selected devices not founded... please reload device.");
            Dictionary.noValidTitleFounded = IniLangCheck(lang_to_use, "noValidTitleFounded", "No valid title founded: no one cover can be downloaded.");
            Dictionary.titlesCheckComplete = IniLangCheck(lang_to_use, "TitlesCheckComplete", "Titles check complete!");
            Dictionary.coversDownloadStart = IniLangCheck(lang_to_use, "coversDownloadStart", "And now covers download will start!!");
            Dictionary.imagesSearched = IniLangCheck(lang_to_use, "imagesSearched", "Images searched:");
            Dictionary.imagesAlreadyPresent = IniLangCheck(lang_to_use, "imagesAlreadyPresent", "Images already present in loader device:");
            Dictionary.imagesFromCache = IniLangCheck(lang_to_use, "imagesFromCache", "Images taked from application cache:");
            Dictionary.imagesDownloaded = IniLangCheck(lang_to_use, "imagesDownloaded", "Images downloaded:");
            Dictionary.imagesNotFounded = IniLangCheck(lang_to_use, "imagesNotFounded", "Images NOT founded:");
            Dictionary.checkingWiitdbPackage = IniLangCheck(lang_to_use, "checkingWiitdbPackage", "Checking wiitdb package...");
            Dictionary.checkingWiiGamesTitles = IniLangCheck(lang_to_use, "checkingWiiGamesTitles", "Cheking Wii Games titles: please wait...");
            Dictionary.checkingGameCubeTitles = IniLangCheck(lang_to_use, "checkingGameCubeTitles", "Cheking GameCube titles: please wait...");
            Dictionary.checkingEmulatedNandTitles = IniLangCheck(lang_to_use, "checkingEmulatedNandTitles", "Cheking Emulated Nand titles: please wait...");
            Dictionary.notFound = IniLangCheck(lang_to_use, "notFound", "..not found");
            Dictionary.alreadyPresent = IniLangCheck(lang_to_use, "alreadyPresent", "..already present.");
            Dictionary.downloadStopByUser = IniLangCheck(lang_to_use, "downloadStopByUser", "DOWNLOAD STOPPED BY USER.");
            Dictionary.readingFolder = IniLangCheck(lang_to_use, "readingFolder", "Reading folder: please wait...");
            Dictionary.founded = IniLangCheck(lang_to_use, "founded", "Founded");           
            Dictionary.noWiiGamesFounded = IniLangCheck(lang_to_use, "noWiiGamesFounded", "No Wii games founded");
            Dictionary.zeroWbfsFileFounded = IniLangCheck(lang_to_use, "zeroWbfsFileFounded", "(0 .wbfs files)");
            Dictionary.noGameCubeGamesFounded = IniLangCheck(lang_to_use, "noGameCubeGamesFounded", "No GameCube games founded (0 .iso files)");
            Dictionary.noEmuNandTitlesFounded = IniLangCheck(lang_to_use, "noEmuNandTitlesFounded", "No titles founded in emulated NAND");            
            Dictionary.checking = IniLangCheck(lang_to_use, "checking", "Checking");
            Dictionary.downloading = IniLangCheck(lang_to_use, "downloading", "Downloading");
            Dictionary.extracting = IniLangCheck(lang_to_use, "extracting", "Extracting");
            Dictionary.copyingGametdbPackage = IniLangCheck(lang_to_use, "copyingGametdbPackage", "Copying gametdb package into Loader folder...");
        }        

        private string searchLanguageInSettings()
        {
           IniFile inifile = new IniFile(SETTINGS_INI_FILE);

           return inifile.IniReadValue("Languages", "ApplicationLanguage");
        }

        private void SearchItemsForLanguage()
        {
            comboBoxAppLanguage.Items.Clear();

            comboBoxAppLanguage.Items.Add("default"); 
             
            if(!File.Exists(LANGUAGES_FILE))
                return;      

            string[] langFile = File.ReadAllLines(LANGUAGES_FILE);

            foreach (string line in langFile)
            {
                if (line.Trim() == "")
                    continue;

                if (line.Length < 2)
                    continue;

                if ((line.Substring(0, 1) == "[") && (line.Substring(line.Length - 1, 1) == "]"))
                {
                    if(line.Substring(1, line.Length - 2) != "default")
                        comboBoxAppLanguage.Items.Add(line.Substring(1, line.Length - 2));
                }
            }
            
        }

        private void ReloadAppLanguages()
        {
            string languageToUse;
            string languageInSettings = searchLanguageInSettings();
            string languageFormCurrentRegion = SearchLanguageForApplication("application");

            if (languageInSettings != "")
                languageToUse = languageInSettings;
            else
                languageToUse = languageFormCurrentRegion;

            SearchItemsForLanguage();

            bool langFounded = false;

            if (comboBoxAppLanguage.Items.Count != 0)
            {
                comboBoxAppLanguage.Enabled = true;
                foreach (string lang in comboBoxAppLanguage.Items)
                {
                    if (lang == languageToUse)
                        langFounded = true;
                }
            }
            else
                comboBoxAppLanguage.Enabled = false;

            if (!langFounded)
            {
                languageToUse = "default";                
            }
            else
            {
                comboBoxAppLanguage.Text = languageToUse;
            }

            ReloadDictionary(languageToUse);
        }

        private string SearchLanguageForApplication(string mode)
        {            
            switch (System.Globalization.RegionInfo.CurrentRegion.ThreeLetterISORegionName)
            {
                case "ITA":
                    if(mode == "wiitdb")
                        return "IT - Italian";
                    else
                        return "italian";
                case "ESP":
                case "ARG":
                case "BOL":
                case "CHL":
                case "COL":
                case "CRI":
                case "CUB":
                case "ECU":
                case "SLV":
                case "GTM":
                case "HND":
                case "MEX":
                case "NIC":
                case "PAN":
                case "PRY":
                case "PER":
                case "PRI":
                case "URY":
                case "VEN":
                    if (mode == "wiitdb")
                        return "ES - Spanish";
                    else
                        return "spanish";                    
                case "FRA":
                case "FXX":
                case "GUF":
                case "PYF":
                case "ATF":
                case "MCO":
                case "LUX":
                case "HTI":
                case "VUT":
                case "MRT":
                case "MLI":
                case "NER":
                case "TCD":
                case "BFA":
                case "SEN":
                case "BEN":
                case "CIV":
                case "GIN":
                case "TGO":
                case "RWA":
                case "BDI":
                case "CMR":
                case "GAB":
                case "COM":
                case "MDG":
                case "MUS":
                case "SYC":
                case "DZA":
                case "MAR":
                case "TUN":
                case "AND":
                case "VNM":
                case "KHM":
                case "LAO":
                    if (mode == "wiitdb")
                        return "FR - French";
                    else
                        return "french";                      
                case "AGO":
                case "BRA":
                case "PRT":
                case "CPV":
                case "MOZ":
                case "GNB":
                case "STP":
                case "MAC":
                    if (mode == "wiitdb")
                        return "PT - Portuguese";
                    else
                        return "portuguese";
                case "JPN":
                    if (mode == "wiitdb")
                        return "JA - Japanese";
                    else
                        return "japanese";  
                 case "PRK":
                 case "KOR":
                    if (mode == "wiitdb")
                        return "KO - Korean";
                    else
                        return "korean"; 
                case "CHN":
                case "TWN":
                case "SGP":
                    if (mode == "wiitdb")
                        return "ZHCN - Chinese (Simplified)";
                    else
                        return "chinese";
                case "DEU":
                    if (mode == "wiitdb")
                        return "DE - German";
                    else
                        return "german";
                case "NLD":
                case "BEL":
                case "SUR":
                case "ABW":
                    if (mode == "wiitdb")
                        return "NL - Dutch";
                    else
                        return "dutch";
                case "RUS":
                    if (mode == "wiitdb")
                        return "RU - Russian";
                    else
                        return "russian";                                  
                default:
                    if (mode == "wiitdb")
                        return "EN - English";
                    else
                        return "english";                   
            }
           
        }

        private void UpdateLanguagesFile(string new_version)
        {
            if (WiiCoverDownloaderWaitForm.IsHandleCreated)
                WiiCoverDownloaderWaitForm.labelFirstTime.Text = "...downloading languages.ini...";

            if (executeDownload(languagesIni_Link + new_version + ".ini", LANGUAGES_FILE, false))
            {
                IniFile inifile = new IniFile(SETTINGS_INI_FILE);
                inifile.IniWriteValue("Languages", "LanguageFileVersion", new_version);
            }
        }        

        private bool CheckDeviceBeforeStart()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            bool found;

            if (comboBoxLoaderDevice.Text.Trim() != "")
            {
                found = false;
                foreach (DriveInfo d in drives)
                {
                    if (d.Name == comboBoxLoaderDevice.Text.Substring(0, 3))
                        found = true;
                }
                if (!found)
                    return false;
            }
            if (comboBoxWiiGamesDevice.Text.Trim() != "")
            {
                found = false;
                foreach (DriveInfo d in drives)
                {
                    if (d.Name == comboBoxWiiGamesDevice.Text.Substring(0, 3))
                        found = true;
                }
                if (!found)
                    return false;
            }
            if (comboBoxGameCubeDevice.Text.Trim() != "")
            {
                found = false;
                foreach (DriveInfo d in drives)
                {
                    if (d.Name == comboBoxGameCubeDevice.Text.Substring(0, 3))
                        found = true;
                }
                if (!found)
                    return false;
            }
            if (comboBoxNandDevice.Text.Trim() != "")
            {
                found = false;
                foreach (DriveInfo d in drives)
                {
                    if (d.Name == comboBoxNandDevice.Text.Substring(0, 3))
                        found = true;
                }
                if (!found)
                    return false;
            }

            return true;
        }
               

        private void buttonDownloadStart_Click(object sender, EventArgs e)
        {
            if (!CheckDeviceBeforeStart())
            {
                MessageBox.Show(Dictionary.deviceNotMoreFounded, "WiiCoverDownloader (Error)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            FolderCheck();

            DOWNLOAD_OR_EXE_WORKING = true;
            DOWNLOADED_IMAGES = CACHED_IMAGES = NOT_FOUND_IMAGES = ALREADY_PRESENT_IMAGES = NOT_FOUND_IMAGES = 0;                       

            DirectoryClean(DOWNLOAD_PATH);

            enablePageAndButton(false);

            ResetProgressBar();

            richTextBoxInfo.Clear();

            // rileggo i dati per essere sicuro
            ReadSettingsFile();
            ReadLanguageFile();

            //se necessario aggiorno file titles.txt            
            TitlesFileCheck();
            if (!DOWNLOAD_OR_EXE_WORKING)
            {
                DownloadStopped(false);
                return;
            }

            // se necessario scarico wiitdb.zip
            if (checkBoxDownloadGameTDBPack.Checked)
            {
                Download_GameTDB_Pack();
                if (!DOWNLOAD_OR_EXE_WORKING)
                {
                    DownloadStopped(false);
                    return;
                }                    
            }

            // ora  metto i dati nella combobox..                        
            if (!PopulateTitlesList())
            {                
                FileDelete(GAMES_ID_FILE);
                {
                    DownloadStopped(false);
                    return;
                }
            }            
            FileDelete(GAMES_ID_FILE);

            // vediamo se c'è della roba...
            if (TITLES_LIST.Count == 0)
            {
                AppendText("\n" + Dictionary.noValidTitleFounded + "\n");
                {
                    DownloadStopped(false);
                    return;
                }
            }
            else
                AppendText("\n" + Dictionary.titlesCheckComplete + "\n\n");

            // and now start covers download !!
            AppendText("\n" + Dictionary.coversDownloadStart + "\n\n");
            ResetProgressBar();
            ReadComboBox();            

            if (DOWNLOAD_OR_EXE_WORKING)
            {                               
                AppendText("\n" + Dictionary.downloadCompleted + "\n\n");

                int images_searched = ALREADY_PRESENT_IMAGES + CACHED_IMAGES + DOWNLOADED_IMAGES + NOT_FOUND_IMAGES;

                AppendText(FINAL_TITLE_COUNT + " Titles (" +
                           FINAL_WII_GAMES_COUNT + " Wii games, " +
                           FINAL_GAMECUBE_GAMES_COUNT + " GameCube games, " +
                           FINAL_NAND_GAMES_COUNT + " EmuNAND channels)\n");

                AppendText(Dictionary.imagesSearched + " " + images_searched + "\n");

                AppendText(Dictionary.imagesAlreadyPresent + " " + ALREADY_PRESENT_IMAGES + "\n");

                if (CACHED_IMAGES != 0)
                    AppendText(Dictionary.imagesFromCache + " " + CACHED_IMAGES + "\n");

                AppendText(Dictionary.imagesDownloaded + " " + DOWNLOADED_IMAGES + "\n");

                AppendText(Dictionary.imagesNotFounded + " " + NOT_FOUND_IMAGES);

                MessageBox.Show(Dictionary.downloadCompleted, "WiiCoverDownloader", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            enablePageAndButton(true);
            DOWNLOAD_OR_EXE_WORKING = false;
            CreateLogFile();

        }


        private bool ImageExist(string ID, string type)
        {
            string device_root = comboBoxLoaderDevice.Text.Substring(0, 3);

            bool img_found = true;

            if (GX_COVER)
            {
                switch (type)
                {
                    case "full":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxGX_full.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "disc":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxGX_disc.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "2d":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxGX_2D.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "3d":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxGX_3D.Text, ID + ".png")))
                            img_found = false;
                        break;
                }
            }

            if (CFG_COVER)
            {
                switch (type)
                {
                    case "full":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxCFG_full.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "disc":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxCFG_disc.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "2d":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxCFG_2D.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "3d":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxCFG_3D.Text, ID + ".png")))
                            img_found = false;
                        break;
                }
            }

            if (WIIFLOW_COVER)
            {
                switch (type)
                {
                    case "full":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxWiiflow_full.Text, ID + ".png")))
                            img_found = false;
                        break;
                    case "2d":
                        if (FileSizeZeroOrNotExist(CombinePath(device_root, textBoxWiiflow_2D.Text, ID + ".png")))
                            img_found = false;
                        break;
                }
            }

            return img_found;
        }

        private void DirectoryClean(string targetPath)
        {
            if (Directory.Exists(targetPath))
                CleanFolder(targetPath);

            return;
        }

        private bool CleanFolder(string targetPath)
        {
            Thread.Sleep(0);
            string[] files = Directory.GetFiles(targetPath);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(targetPath, name);
                if (!FileDelete(dest))
                    return false;
            }
            string[] folders = Directory.GetDirectories(targetPath);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(targetPath, name);
                Thread.Sleep(0);
                if (CleanFolder(dest))
                    return true;
                else
                    return false;
            }
            return true;
        }
        
        private void RefreshProgressBar(int actual_val, int max_val)
        {
            int percent = (actual_val * 100) / max_val;
            if (percent > 100)
                percent = 100;
            else if (percent < 0)
                percent = 0;
            progressBarForDownload.Value = percent;
            labelProgrssBar.Text = actual_val + " of " + max_val + " (" + percent + "%)";
            if (percent == 0 || percent == 100)
                this.Refresh();
        }

        private void ReadComboBox()
        {
            int temp_count = 0;

            if (TITLES_LIST.Count != 0)
            {                
                for (int i = 0; i < TITLES_LIST.Count; i++)
                {
                    bool download_full = false, download_2d = false, download_3d = false, download_disc = false;

                    MySleep(1);

                    if (!DOWNLOAD_OR_EXE_WORKING)
                        return;

                    RefreshProgressBar(temp_count, FINAL_TITLE_COUNT);                   

                    if (TITLES_LIST[i].name != "")
                        AppendText(TITLES_LIST[i].ID + " - " + TITLES_LIST[i].name);
                    else
                    {
                        temp_count++;                        
                        continue;
                    }                    

                    if (radioButtonDownloadOnlyMissing.Checked)
                    {
                        if (!ImageExist(TITLES_LIST[i].ID, "full"))
                            download_full = true;
                        if (!ImageExist(TITLES_LIST[i].ID, "disc"))
                            download_disc = true;
                        if (!ImageExist(TITLES_LIST[i].ID, "2d"))
                            download_2d = true;
                        if (!ImageExist(TITLES_LIST[i].ID, "3d"))
                            download_3d = true;
                    }
                    else
                    {
                        if (GX_COVER || CFG_COVER || WIIFLOW_COVER)
                            download_full = true;
                        if (GX_COVER || CFG_COVER || WIIFLOW_COVER)
                            download_2d = true;
                        if (GX_COVER || CFG_COVER)
                            download_3d = true;
                        if (GX_COVER || CFG_COVER)
                            download_disc = true;
                    }

                    if (GX_COVER || CFG_COVER || WIIFLOW_COVER)
                        AppendText("\n\tFull cover...");
                    if (download_full)
                    {
                        if (downloadImage(TITLES_LIST[i].ID, "coverfullHQ", "coverfull"))
                        {
                            CopyImage(TITLES_LIST[i].ID, "full");
                            AppendText("..Ok");
                        }
                        else
                        {
                            if (!DOWNLOAD_OR_EXE_WORKING)
                                return;
                            AppendText(Dictionary.notFound);
                            NOT_FOUND_IMAGES++;
                        }
                    }
                    else if (GX_COVER || CFG_COVER || WIIFLOW_COVER)
                    {
                        AppendText(Dictionary.alreadyPresent);
                        ALREADY_PRESENT_IMAGES++;
                    }


                    if (GX_COVER || CFG_COVER)
                        AppendText("\n\tDisc cover...");
                    if (download_disc)
                    {
                        if (downloadImage(TITLES_LIST[i].ID, "disc", "disccustom"))
                        {
                            CopyImage(TITLES_LIST[i].ID, "disc");
                            AppendText("..Ok");
                        }
                        else
                        {
                            if (!DOWNLOAD_OR_EXE_WORKING)
                                return;
                            AppendText(Dictionary.notFound);
                            NOT_FOUND_IMAGES++;
                        }
                    }
                    else if (GX_COVER || CFG_COVER)
                    {
                        AppendText(Dictionary.alreadyPresent);
                        ALREADY_PRESENT_IMAGES++;
                    }

                    if (GX_COVER || CFG_COVER || WIIFLOW_COVER)
                        AppendText("\n\t2D cover...");
                    if (download_2d)
                    {
                        if (downloadImage(TITLES_LIST[i].ID, "cover"))
                        {
                            CopyImage(TITLES_LIST[i].ID, "2d");
                            AppendText("..Ok");
                        }
                        else
                        {
                            if (!DOWNLOAD_OR_EXE_WORKING)
                                return;
                            AppendText(Dictionary.notFound);
                            NOT_FOUND_IMAGES++;
                        }
                    }
                    else if (GX_COVER || CFG_COVER || WIIFLOW_COVER)
                    {
                        AppendText(Dictionary.alreadyPresent);
                        ALREADY_PRESENT_IMAGES++;
                    }

                    if (GX_COVER || CFG_COVER)
                        AppendText("\n\t3D cover...");
                    if (download_3d)
                    {
                        if (downloadImage(TITLES_LIST[i].ID, "cover3D"))
                        {
                            CopyImage(TITLES_LIST[i].ID, "3d");
                            AppendText("..Ok");
                        }
                        else
                        {
                            if (!DOWNLOAD_OR_EXE_WORKING)
                                return;
                            AppendText(Dictionary.notFound);
                            NOT_FOUND_IMAGES++;
                        }
                    }
                    else if (GX_COVER || CFG_COVER)
                    {
                        AppendText(Dictionary.alreadyPresent);
                        ALREADY_PRESENT_IMAGES++;
                    }

                    FileDelete(CombinePath(DOWNLOAD_PATH, TITLES_LIST[i].ID + ".png"));

                    AppendText("\n\n");
                    temp_count++;
                }
                RefreshProgressBar(temp_count, FINAL_TITLE_COUNT);
            }
        }

        static void FileCopy(string file_source, string file_dest, bool remove_source)
        {
            string folder_dest = Path.GetDirectoryName(file_dest);

            if (!File.Exists(file_source))
                return;

            if (!Directory.Exists(folder_dest))
                Directory.CreateDirectory(folder_dest);

            FileDelete(file_dest);

            File.Copy(file_source, file_dest, true);

            if (remove_source)
                FileDelete(file_source);
        }

        private bool FileSizeZeroOrNotExist(string file_name)
        {
            if (!File.Exists(file_name))
                return true;

            System.IO.FileInfo fileinfo = new System.IO.FileInfo(file_name);
            if (fileinfo.Length == 0)
            {
                FileDelete(file_name);
                return true;
            }

            return false;
        }

        private void CheckAndCopyImage(string loader_path, string game_id, string loader_used)
        {
            bool file_must_be_copyied = radioButtonDownloadAll.Checked;
            string img_dowloaded = CombinePath(DOWNLOAD_PATH, game_id + ".png");
            string device_root = comboBoxLoaderDevice.Text.Substring(0, 3);
            string file_to_check = CombinePath(device_root, loader_path, game_id + ".png");
            string folder_dest = CombinePath(device_root, loader_path);

            if (!file_must_be_copyied)
            {
                if (FileSizeZeroOrNotExist(file_to_check))
                    file_must_be_copyied = true;
            }

            if (file_must_be_copyied)
            {
                if (!Directory.Exists(folder_dest))
                    Directory.CreateDirectory(folder_dest);

                FileDelete(file_to_check);

                File.Copy(img_dowloaded, file_to_check, true);

                UpdateLoaderCache(loader_used, game_id);

            }
        }

        private void UpdateLoaderCache(string loader_used, string game_id)
        {
            switch (loader_used)
            {
                case "GX":
                    break;
                case "CFG":
                    FileDelete(CombinePath(comboBoxLoaderDevice.Text.Substring(0, 3), "usb-loader", "covers", "cache", game_id + ".ccc"));
                    break;
                case "WIIFLOW":
                    FileDelete(CombinePath(comboBoxLoaderDevice.Text.Substring(0, 3), "wiiflow", "cache", game_id + ".wfc"));
                    break;
            }
        }

        private void CopyImage(string game_id, string type)
        {
            if (GX_COVER)
            {
                switch (type)
                {
                    case "full":
                        CheckAndCopyImage(textBoxGX_full.Text, game_id, "GX");
                        break;
                    case "disc":
                        CheckAndCopyImage(textBoxGX_disc.Text, game_id, "GX");
                        break;
                    case "2d":
                        CheckAndCopyImage(textBoxGX_2D.Text, game_id, "GX");
                        break;
                    case "3d":
                        CheckAndCopyImage(textBoxGX_3D.Text, game_id, "GX");
                        break;
                }
            }

            if (CFG_COVER)
            {
                switch (type)
                {
                    case "full":
                        CheckAndCopyImage(textBoxCFG_full.Text, game_id, "CFG");
                        break;
                    case "disc":
                        CheckAndCopyImage(textBoxCFG_disc.Text, game_id, "CFG");
                        break;
                    case "2d":
                        CheckAndCopyImage(textBoxCFG_2D.Text, game_id, "CFG");
                        break;
                    case "3d":
                        CheckAndCopyImage(textBoxCFG_3D.Text, game_id, "CFG");
                        break;
                }
            }

            if (WIIFLOW_COVER)
            {
                switch (type)
                {
                    case "full":
                        CheckAndCopyImage(textBoxWiiflow_full.Text, game_id, "WIIFLOW");
                        break;
                    case "2d":
                        CheckAndCopyImage(textBoxWiiflow_2D.Text, game_id, "WIIFLOW");
                        break;
                }
            }
        }

        private bool downloadImage(string game_id, params string[] type)
        {
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);
            int language_count;

            FileDelete(CombinePath(DOWNLOAD_PATH, game_id + ".png"));

            for (language_count = 1; language_count < 9; language_count++)
            {
                string lang_for_search = inifile.IniReadValue("Languages", "Lang" + language_count);
                if (lang_for_search.Contains("ZHCN") || lang_for_search.Contains("ZHTW"))
                    lang_for_search = lang_for_search.Substring(0, 4);
                else
                    lang_for_search = lang_for_search.Substring(0, 2);

                for (int i = 0; i < type.Length; i++)
                {
                    if (ImageInCache(type[i], lang_for_search, game_id))
                    {
                        CACHED_IMAGES++;
                        return true;
                    }

                    string link = @"http://art.gametdb.com/wii/" +
                                    type[i] +
                                    Convert.ToString('/') +
                                    lang_for_search +
                                    Convert.ToString('/') +
                                    game_id +
                                    ".png";

                    if (executeDownload(link, CombinePath(DOWNLOAD_PATH, game_id + ".png"), false))
                    {
                        DOWNLOADED_IMAGES++;
                        AddToCache(type[i], lang_for_search, game_id);
                        return true;
                    }
                }
            }

            return false;
        }

        private void AddToCache(string type, string lang_for_search, string game_id)
        {
            if (!Directory.Exists(CombinePath(CACHE_PATH, type, lang_for_search)))
                Directory.CreateDirectory(CombinePath(CACHE_PATH, type, lang_for_search));

            FileCopy(CombinePath(DOWNLOAD_PATH, game_id + ".png"), CombinePath(CACHE_PATH, type, lang_for_search, game_id + ".png"), false);
        }

        private bool ImageInCache(string type, string lang_for_search, string game_id)
        {
            if (File.Exists(CombinePath(CACHE_PATH, type, lang_for_search, game_id + ".png")))
            {
                FileCopy(CombinePath(CACHE_PATH, type, lang_for_search, game_id + ".png"), CombinePath(DOWNLOAD_PATH, game_id + ".png"), false);
                return true;
            }
            return false;
        }

        private bool PopulateTitlesList()
        {
            TITLES_LIST.Clear();  

            WII_GAMES_COUNT = GAMECUBE_GAMES_COUNT = NAND_GAMES_COUNT = 0;
            FINAL_WII_GAMES_COUNT = FINAL_GAMECUBE_GAMES_COUNT = FINAL_NAND_GAMES_COUNT = 0;

            if (WII_GAMES_FOUND)
            {
                AppendText(Dictionary.checkingWiiGamesTitles);
                if (comboBoxWiiGamesDevice.Text.Contains(" [WBFS partition]"))
                {                    
                    if(WBFS_CHECK(comboBoxWiiGamesDevice.Text, true))
                        PopulateGamesComboBox_from_file("wii_games");
                    WII_GAMES_COUNT = TEMP_WII_GAMES_COUNT;
                }
                else
                    PopulateGamesComboBox_for_wbfs_files();
                if(DOWNLOAD_OR_EXE_WORKING)
                    AppendText("..OK.\n");
                else
                    return false;
            }
            if (GAMECUBE_GAMES_FOUND)
            {
                AppendText(Dictionary.checkingGameCubeTitles);
                Search_GameCube_games(textBoxGameCubeFolder.Text, true);
                if (DOWNLOAD_OR_EXE_WORKING)
                    AppendText("..OK.\n");
                else
                    return false;
            }
            if (NAND_GAMES_FOUND)
            {
                AppendText(Dictionary.checkingEmulatedNandTitles);
                Search_Nand_titles(textBoxNandFolder.Text, true);
                if (DOWNLOAD_OR_EXE_WORKING)
                    AppendText("..OK.\n");
                else
                    return false;
            }
            
            FINAL_TITLE_COUNT = FINAL_WII_GAMES_COUNT + FINAL_GAMECUBE_GAMES_COUNT + FINAL_NAND_GAMES_COUNT;

            return true;

        }

        private void add_to_TITLES_LIST(string ID_to_check, string type)
        {
            for (int i = 0; i < TITLES_LIST.Count; i++)
            {
                if (TITLES_LIST[i].ID == ID_to_check)
                    return;
            }

            TitleInfo titleLine;

            switch (type)
            {
                case "wii_games":
                    for (int i = 0; i < WiiGamesList.Count; i++)
                    {
                        if (WiiGamesList[i].ID == ID_to_check)
                        {
                            titleLine.ID = WiiGamesList[i].ID;
                            titleLine.name = WiiGamesList[i].name;

                            TITLES_LIST.Add(titleLine);

                            FINAL_WII_GAMES_COUNT++;
                        }
                    }
                    break;
                case "gamecube_games":
                    for (int i = 0; i < GameCubeGamesList.Count; i++)
                    {
                        if (GameCubeGamesList[i].ID == ID_to_check)
                        {
                            titleLine.ID = GameCubeGamesList[i].ID;
                            titleLine.name = GameCubeGamesList[i].name;

                            TITLES_LIST.Add(titleLine);

                            FINAL_GAMECUBE_GAMES_COUNT++;
                        }
                    }
                    break;
                case "emuNand":
                    for (int i = 0; i < ChannelTitlesList.Count; i++)
                    {
                        if (ChannelTitlesList[i].ID == ID_to_check)
                        {
                            titleLine.ID = ChannelTitlesList[i].ID;
                            titleLine.name = ChannelTitlesList[i].name;

                            TITLES_LIST.Add(titleLine);

                            FINAL_NAND_GAMES_COUNT++;
                        }
                    }
                    break;
                default:
                    return;
            }
            return;
        }

        private void PopulateGamesComboBox_from_file(string source)
        {
            string[] file = File.ReadAllLines(GAMES_ID_FILE);

            foreach (string line in file)
            {
                if (line.Contains("Error:") || line.Contains("split 0:"))
                    continue;

                if (line.Length > 5)
                {
                    if (source == "wii_games")
                    {
                        if (!comboBoxWiiGamesDevice.Text.Contains(" [WBFS partition]"))
                            RefreshProgressBar(WII_GAMES_COUNT, TEMP_WII_GAMES_COUNT);
                        
                        add_to_TITLES_LIST(line.Substring(0, 6), "wii_games");
                    }
                    else if (source == "gamecube_games")
                    {
                        RefreshProgressBar(GAMECUBE_GAMES_COUNT, TEMP_GAMECUBE_GAMES_COUNT);
                        add_to_TITLES_LIST(line.Substring(0, 6), "gamecube_games");   
                    }
                }
            }

            return;

        }

        private void PopulateGamesComboBox_for_wbfs_files()
        {
            Search_WBFS_games(textBoxGamesFolder.Text, true);
            return;
        }



        private void Add_GameGube_File_in_ComboBox(string file)
        {
            string program = "\"" + CombinePath(TOOLS_PATH, "wit.exe") + "\"";

            FileDelete(GAMES_ID_FILE);

            if (!executeCommand(program,
                                "ID6 \"" + file + "\"" + " > \"" + GAMES_ID_FILE + "\"",
                                TOOLS_PATH,
                                "Unespcted ERROR using wit.exe..."))
                return;

            if (!File.Exists(GAMES_ID_FILE))
                return;

            PopulateGamesComboBox_from_file("gamecube_games");
        }

        private void Add_WBFS_File_in_ComboBox(string file)
        {
            string program = "\"" + CombinePath(TOOLS_PATH, "wbfs_file.exe") + "\"";            

            GAMES_ID_FILE = CombinePath(TOOLS_PATH, "GAMES_ID_FILE.txt");

            FileDelete(GAMES_ID_FILE);

            if (!executeCommand(program,
                                "\"" + file + "\"" + " id_title > \"" + GAMES_ID_FILE + "\"",
                                TOOLS_PATH,
                                "Unespcted ERROR using wbfs_file.exe..."))           
                return;

            if (!File.Exists(GAMES_ID_FILE))
                return;

            PopulateGamesComboBox_from_file("wii_games");
        }


        private void enablePageAndButton(bool value)
        {
            buttonStopDownload.Enabled = !value;

            buttonReloadDevice.Enabled = value;
            buttonDownloadStart.Enabled = value;
            radioButtonDownloadOnlyMissing.Enabled = value;
            radioButtonDownloadAll.Enabled = value;
            comboBoxLoaderDevice.Enabled = value;
            comboBoxWiiGamesDevice.Enabled = value;
            comboBoxGameCubeDevice.Enabled = value;
            comboBoxNandDevice.Enabled = value;
            panelLang.Enabled = value;
            panelPath.Enabled = value;
            panelPathPreferences.Enabled = value;
            comboBoxAppLanguage.Enabled = value;

            IntPtr hSystemMenu = GetSystemMenu(this.Handle, false);
            if (value)
                EnableMenuItem(hSystemMenu, SC_CLOSE, MF_ENABLED);
            else
                EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
        }

        private void ResetProgressBar()
        {
            progressBarForDownload.Value = 0;
            labelProgrssBar.Text = "";
        }

        void DownloadStopped(bool addMessage)
        {
            DOWNLOAD_OR_EXE_WORKING = false;
            enablePageAndButton(true);
            TITLES_LIST.Clear();
            if (addMessage)
                AppendText("\n\n" + Dictionary.downloadStopByUser + "\n");
            CreateLogFile();
        }

        private void buttonStopDownload_Click(object sender, EventArgs e)
        {
            DownloadStopped(true);
        }

        private void comboBoxLoaderDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetProgressBar();
            DOWNLOAD_OR_EXE_WORKING = true;
            CheckForLoaderInDevice();
            CheckForLoaderAndGames();
            DOWNLOAD_OR_EXE_WORKING = false;
        }

        private void CheckForLoaderAndGames()
        {
            richTextBoxInfo.Clear();

            if (LOADER_FOUND && (WII_GAMES_FOUND || GAMECUBE_GAMES_FOUND || NAND_GAMES_FOUND))
                buttonDownloadStart.Enabled = true;
            else
                buttonDownloadStart.Enabled = false;

            FileDelete(GAMES_ID_FILE);
        }



        private void CheckForEmulatedNandGamesInDevice()
        {
            NAND_GAMES_FOUND = false;
            NAND_GAMES_COUNT = 0;

            if (comboBoxNandDevice.Text.Trim() == "")
            {
                textBoxNandGamesCount.Text = "";
                return;
            }
            else
            {
                if (textBoxNandFolder.Text.Length < 4)
                {
                    textBoxNandFolder.Text = "";
                    textBoxNandGamesCount.Text = "";
                    comboBoxNandDevice.SelectedIndex = -1;
                }
                else
                {
                    richTextBoxInfo.Clear();
                    AppendText(Dictionary.readingFolder + "\n");
                    this.Refresh();

                    Search_Nand_titles(textBoxNandFolder.Text, false);
                    if (NAND_GAMES_COUNT > 0)
                    {
                        textBoxNandGamesCount.Text = "Founded " + NAND_GAMES_COUNT + " titles.";
                        NAND_GAMES_FOUND = true;
                    }
                    else
                        textBoxNandGamesCount.Text = Dictionary.noEmuNandTitlesFounded;

                    richTextBoxInfo.Clear();

                }
            }
        }

        private void CheckForGameCubeGamesInDevice()
        {
            GAMECUBE_GAMES_FOUND = false;
            GAMECUBE_GAMES_COUNT = 0;

            if (comboBoxGameCubeDevice.Text.Trim() == "")
            {
                textBoxGameCubeGamesCount.Text = "";
                return;
            }
            else
            {
                if (textBoxGameCubeFolder.Text.Length < 4)
                {
                    textBoxGameCubeFolder.Text = "";
                    textBoxGameCubeGamesCount.Text = "";
                    comboBoxGameCubeDevice.SelectedIndex = -1;
                    return;
                }
                richTextBoxInfo.Clear();
                AppendText(Dictionary.readingFolder + "\n");
                this.Refresh();

                Search_GameCube_games(textBoxGameCubeFolder.Text, false);
                if (GAMECUBE_GAMES_COUNT > 0)
                {
                    textBoxGameCubeGamesCount.Text = Dictionary.founded + " " + GAMECUBE_GAMES_COUNT + " .iso files.";
                    GAMECUBE_GAMES_FOUND = true;
                }
                else
                    textBoxGameCubeGamesCount.Text = Dictionary.noGameCubeGamesFounded;

                richTextBoxInfo.Clear();

            }

            TEMP_GAMECUBE_GAMES_COUNT = GAMECUBE_GAMES_COUNT;
        }


        private void CheckForWiiGamesInDevice()
        {
            WII_GAMES_FOUND = false;
            WII_GAMES_COUNT = 0;

            if (comboBoxWiiGamesDevice.Text.Trim() == "")
            {
                textBoxWiiGamesCount.Text = "";
                return;
            }
            else if (comboBoxWiiGamesDevice.Text.Contains(" [WBFS partition]"))
            {
                if (WBFS_CHECK(comboBoxWiiGamesDevice.Text, false))
                {
                    textBoxWiiGamesCount.Text = Dictionary.founded + " " + WII_GAMES_COUNT + " Wii games";
                    WII_GAMES_FOUND = true;
                }
                else
                    textBoxWiiGamesCount.Text = Dictionary.noWiiGamesFounded;                 
            }
            else
            {
                if (textBoxGamesFolder.Text.Length < 4)
                {
                    textBoxGamesFolder.Text = "";
                    textBoxWiiGamesCount.Text = "";
                    comboBoxWiiGamesDevice.SelectedIndex = -1;
                }
                else
                {
                    richTextBoxInfo.Clear();
                    AppendText(Dictionary.readingFolder + "\n");
                    this.Refresh();

                    Search_WBFS_games(textBoxGamesFolder.Text, false);
                    if (WII_GAMES_COUNT > 0)
                    {
                        textBoxWiiGamesCount.Text = Dictionary.founded + " " + WII_GAMES_COUNT + " .wbfs files";
                        WII_GAMES_FOUND = true;
                    }
                    else
                        textBoxWiiGamesCount.Text = Dictionary.noWiiGamesFounded + " " + Dictionary.zeroWbfsFileFounded;

                    richTextBoxInfo.Clear();
                }
            }
            TEMP_WII_GAMES_COUNT = WII_GAMES_COUNT;

        }


        private void Search_Nand_titles(string path_for_titles, bool PopulateComboBox)
        {
            if (!DOWNLOAD_OR_EXE_WORKING)
                return;

            bool hbc_founded = false;

            string path_to_check = "";

            for (int folder_count = 0; folder_count < 2; folder_count++)
            {
                if (folder_count == 0)
                    path_to_check = CombinePath(path_for_titles, "title", "00010001");
                if (folder_count == 1)
                    path_to_check = CombinePath(path_for_titles, "title", "00010002");
                if (folder_count == 2)
                    path_to_check = CombinePath(path_for_titles, "title", "00010004");

                if (!Directory.Exists(path_to_check))
                    continue;

                try
                {
                    string[] folders = Directory.GetDirectories(path_to_check);
                    foreach (string folder in folders)
                    {
                        string dir_name = new DirectoryInfo(folder).Name;
                        dir_name = dir_name.ToUpper();

                        if (dir_name == "4C554C5A" || dir_name == "AF1BF516" || dir_name == "48415858")
                        {
                            hbc_founded = true;
                            continue;
                        }

                        if (IsValidEmuNandTitle(dir_name))
                        {
                            NAND_GAMES_COUNT++;
                            if (PopulateComboBox)                            
                                add_to_TITLES_LIST(ConvertAsciiToString(dir_name), "emuNand");                                                          
                        }
                    }
                }
                catch { }
            }

            if (hbc_founded)
            {
                NAND_GAMES_COUNT++;
                if (PopulateComboBox)
                {
                    add_to_TITLES_LIST("JODI", "emuNand");                    
                }
            }
        }

        private void Search_GameCube_games(string path_for_games, bool PopulateComboBox)
        {
            if (!DOWNLOAD_OR_EXE_WORKING)
                return;

            try
            {
                string[] files = Directory.GetFiles(path_for_games);
                foreach (string file in files)
                {
                    string exetension = Path.GetExtension(file);

                    if ((exetension.ToLower() == ".iso"))
                    {
                        GAMECUBE_GAMES_COUNT++;
                        if (PopulateComboBox)
                            Add_GameGube_File_in_ComboBox(file);
                    }
                }
            }
            catch { }

            try
            {
                string[] folders = Directory.GetDirectories(path_for_games);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string directoryFound = Path.Combine(path_for_games, name);

                    Search_GameCube_games(directoryFound, PopulateComboBox);

                }
            }
            catch { }

        }


        private string ConvertAsciiToString(string folder)
        {
            if (folder.Length != 8)
                return "";

            int n1 = Convert.ToInt32(folder.Substring(0, 2), 16);
            int n2 = Convert.ToInt32(folder.Substring(2, 2), 16);
            int n3 = Convert.ToInt32(folder.Substring(4, 2), 16);
            int n4 = Convert.ToInt32(folder.Substring(6, 2), 16);

            char c1 = (char)n1;
            char c2 = (char)n2;
            char c3 = (char)n3;
            char c4 = (char)n4;

            string tmp_str = c1.ToString() + c2.ToString() + c3.ToString() + c4.ToString();

            return tmp_str.ToUpper();
        }

        private bool IsValidEmuNandTitle(string folder)
        {
            if (folder.Length != 8)
                return false;            

            string title = ConvertAsciiToString(folder);

            for (int i = 0; i < ChannelTitlesList.Count; i++)
            {
                if (ChannelTitlesList[i].ID == title)                                  
                    return true;                
            }
            return false;
        }

        
        private void Search_WBFS_games(string path_for_games, bool PopulateComboBox)
        {
            if (!DOWNLOAD_OR_EXE_WORKING)
                return;

            try
            {
                string[] files = Directory.GetFiles(path_for_games);
                foreach (string file in files)
                {
                    string exetension = Path.GetExtension(file);

                    if ((exetension.ToLower() == ".wbfs"))// || (exetension.ToLower() == ".iso"))
                    {
                        WII_GAMES_COUNT++;
                        if (PopulateComboBox)
                            Add_WBFS_File_in_ComboBox(file);
                    }
                }
            }
            catch { }

            try
            {
                string[] folders = Directory.GetDirectories(path_for_games);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string directoryFound = Path.Combine(path_for_games, name);

                    Search_WBFS_games(directoryFound, PopulateComboBox);

                }
            }
            catch { }

        }

        private void CheckForLoaderInDevice()
        {
            LOADER_FOUND = false;
            GX_COVER = false;
            WIIFLOW_COVER = false;
            CFG_COVER = false;

            if (comboBoxLoaderDevice.Text.Trim() != "")
            {
                string device_for_loader = comboBoxLoaderDevice.Text.Substring(0, 3);
                string loader_found = "";

                if (File.Exists(CombinePath(device_for_loader, textBoxGX_app_path.Text, "boot.dol")) ||
                    File.Exists(CombinePath(device_for_loader, textBoxGX_app_path.Text, "boot.elf")))
                {
                    GX_COVER = true;
                    loader_found = loader_found + "USB Loader GX";
                }

                if (File.Exists(CombinePath(device_for_loader, textBoxWiiflow_app_path.Text, "boot.dol")) ||
                    File.Exists(CombinePath(device_for_loader, textBoxWiiflow_app_path.Text, "boot.elf")))
                {
                    WIIFLOW_COVER = true;
                    if (loader_found != "")
                        loader_found = loader_found + " , ";
                    loader_found = loader_found + "Wiiflow";
                }

                if (File.Exists(CombinePath(device_for_loader, textBoxCFG_app_path.Text, "boot.dol")) ||
                    File.Exists(CombinePath(device_for_loader, textBoxCFG_app_path.Text, "boot.elf")))
                {
                    CFG_COVER = true;
                    if (loader_found != "")
                        loader_found = loader_found + " , ";
                    loader_found = loader_found + "Configurable USB Loader";
                }
                
                if (loader_found == "")
                    textBoxLoaderFound.Text = Dictionary.noLoaderFounded;
                else
                {
                    LOADER_FOUND = true;
                    textBoxLoaderFound.Text = Dictionary.loaderFounded + " " + loader_found;
                }
            }
        }

        private void SetApplicationWorking(bool value)
        {
            if (value)
                this.Cursor = Cursors.WaitCursor;
            else
                this.Cursor = Cursors.Default;

            SendKeys.Send("{TAB}");

            this.Enabled = !value;

            this.Refresh();
        }

        private void textBoxGamesFolder_TextChanged(object sender, EventArgs e)
        {
            SetApplicationWorking(true);
            DOWNLOAD_OR_EXE_WORKING = true;
            CheckForWiiGamesInDevice();
            CheckForLoaderAndGames();
            DOWNLOAD_OR_EXE_WORKING = false;
            SetApplicationWorking(false);
        }

        private void buttonReloadDevice_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            ResetProgressBar();
            DOWNLOAD_OR_EXE_WORKING = true;
            CheckForDevice();
            DOWNLOAD_OR_EXE_WORKING = false;
            textBoxLoaderFound.Text = "";
            textBoxGamesFolder.Text = "";
            textBoxGameCubeFolder.Text = "";
            textBoxNandFolder.Text = "";
            comboBoxWiiGamesDevice.Enabled = false;
            comboBoxGameCubeDevice.Enabled = false;
            comboBoxNandDevice.Enabled = false;
            richTextBoxInfo.Clear();
            this.Cursor = Cursors.Default;

        }

        private void comboBoxGameCubeDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetProgressBar();

            if (FORCE_ROOT_CHANGE)
            {
                FORCE_ROOT_CHANGE = false;
                return;
            }

            if (comboBoxGameCubeDevice.Text.Trim() == "")
            {
                textBoxGameCubeFolder.Text = "";
                return;
            }

            if (StaticPathValid("GameCubeGames"))
                return;

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowserDialog.SelectedPath = comboBoxGameCubeDevice.Text.Substring(0, 3) + "$RECYCLE.BIN";
            DialogResult result = folderBrowserDialog.ShowDialog();

            string tempPath = "";
            if (result == DialogResult.OK)
                tempPath = folderBrowserDialog.SelectedPath;

            if (tempPath.Length < 4)
            {
                textBoxGameCubeFolder.Text = "";
                comboBoxGameCubeDevice.SelectedIndex = -1;
            }
            else
            {
                textBoxGameCubeFolder.Text = tempPath;
                string selected_root = Path.GetPathRoot(textBoxGameCubeFolder.Text);
                if (selected_root != comboBoxGameCubeDevice.Text.Substring(0, 3))
                {
                    FORCE_ROOT_CHANGE = true;

                    foreach (string device_selected in comboBoxGameCubeDevice.Items)
                    {
                        if (device_selected.Substring(0, 3) == selected_root)
                            comboBoxGameCubeDevice.Text = device_selected;
                    }
                }
            }
        }

        private void textBoxGameCubeFolder_TextChanged(object sender, EventArgs e)
        {
            SetApplicationWorking(true);
            ResetProgressBar();
            DOWNLOAD_OR_EXE_WORKING = true;
            CheckForGameCubeGamesInDevice();
            CheckForLoaderAndGames();
            DOWNLOAD_OR_EXE_WORKING = false;
            SetApplicationWorking(false);
        }

        private void textBoxNandFolder_TextChanged(object sender, EventArgs e)
        {
            SetApplicationWorking(true);
            ResetProgressBar();
            DOWNLOAD_OR_EXE_WORKING = true;
            CheckForEmulatedNandGamesInDevice();
            CheckForLoaderAndGames();
            DOWNLOAD_OR_EXE_WORKING = false;
            SetApplicationWorking(false);
        }

        private void comboBoxNandDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetProgressBar();

            if (FORCE_ROOT_CHANGE)
            {
                FORCE_ROOT_CHANGE = false;
                return;
            }

            if (comboBoxNandDevice.Text.Trim() == "")
            {
                textBoxNandFolder.Text = "";
                return;
            }

            if (StaticPathValid("EmuNandTitles"))
                return;

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            folderBrowserDialog.SelectedPath = comboBoxNandDevice.Text.Substring(0, 3) + "$RECYCLE.BIN";
            DialogResult result = folderBrowserDialog.ShowDialog();

            string tempPath = "";
            if (result == DialogResult.OK)
                tempPath = folderBrowserDialog.SelectedPath;

            if (tempPath.Length < 4)
            {
                textBoxNandFolder.Text = "";
                comboBoxNandDevice.SelectedIndex = -1;
            }
            else
            {
                textBoxNandFolder.Text = tempPath;
                string selected_root = Path.GetPathRoot(textBoxNandFolder.Text);
                if (selected_root != comboBoxNandDevice.Text.Substring(0, 3))
                {
                    FORCE_ROOT_CHANGE = true;

                    foreach (string device_selected in comboBoxNandDevice.Items)
                    {
                        if (device_selected.Substring(0, 3) == selected_root)
                            comboBoxNandDevice.Text = device_selected;
                    }
                }
            }

        }

        private void checkBoxWiiGames_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxWiiGames.Checked)
                textBoxWiiGamesPath.Enabled = true;
            else
                textBoxWiiGamesPath.Enabled = false;
        }

        private void checkBoxGameCube_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxGameCube.Checked)
                textBoxGameCubePath.Enabled = true;
            else
                textBoxGameCubePath.Enabled = false;
        }

        private void checkBoxNAND_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNAND.Checked)
                textBoxNANDPath.Enabled = true;
            else
                textBoxNANDPath.Enabled = false;
        }

        private void buttonDefaultPathPreferences_Click(object sender, EventArgs e)
        {
            CreatePathDeviceSettingsFile();
        }
        

        private void buttonSavePathPreferences_Click(object sender, EventArgs e)
        {
            IniFile ini = new IniFile(SETTINGS_INI_FILE);

            if (checkBoxWiiGames.Checked)
                ini.IniWriteValue("WIIGAMES", "StaticPath", "True");
            else
                ini.IniWriteValue("WIIGAMES", "StaticPath", "False");

            if (checkBoxGameCube.Checked)
                ini.IniWriteValue("GAMECUBE", "StaticPath", "True");
            else
                ini.IniWriteValue("GAMECUBE", "StaticPath", "False");

            if (checkBoxNAND.Checked)
                ini.IniWriteValue("EMUNAND", "StaticPath", "True");
            else
                ini.IniWriteValue("EMUNAND", "StaticPath", "False");

            ini.IniWriteValue("WIIGAMES", "path", textBoxWiiGamesPath.Text);
            ini.IniWriteValue("GAMECUBE", "path", textBoxGameCubePath.Text);
            ini.IniWriteValue("EMUNAND", "path", textBoxNANDPath.Text);

        }

        private void buttonDefaulLang_Click(object sender, EventArgs e)
        {
            SearchLanguageAndSetComboBox();
            SaveLanguageValue();
        }

        private void buttonSaveLang_Click(object sender, EventArgs e)
        {
            SaveLanguageValue();
        }

        private void buttonDefaultLoaderPath_Click(object sender, EventArgs e)
        {
            CreateSettingsFile();

            ReadSettingsFile();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!DOWNLOAD_OR_EXE_WORKING)
            {
                ReadSettingsFile();
                ReadLanguageFile();
                ReadPathDeviceSettingsFile();
            }
        }



        private void checkBoxDownloadGameTDBPack_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDownloadGameTDBPack.Checked)
                comboBoxGameTDBLanguage.Enabled = true;
            else
                comboBoxGameTDBLanguage.Enabled = false;
        }

        private void OpenURL(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void buttonGameTDB_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.gametdb.com/");
        }

        private void textBoxLoaderFound_TextChanged(object sender, EventArgs e)
        {
            comboBoxWiiGamesDevice.Enabled = LOADER_FOUND;
            comboBoxGameCubeDevice.Enabled = LOADER_FOUND;
            comboBoxNandDevice.Enabled = LOADER_FOUND;

        }

        private void comboBoxPrimaryLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeLanguageValue();
        }

        private void comboBoxAppLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            IniFile inifile = new IniFile(SETTINGS_INI_FILE);
            inifile.IniWriteValue("Languages", "ApplicationLanguage", comboBoxAppLanguage.Text);

            ReloadDictionary(comboBoxAppLanguage.Text);            
        }

        private void buttonCancelPathPreferences_Click(object sender, EventArgs e)
        {
            ReadPathDeviceSettingsFile();
        }                   
    }
}
