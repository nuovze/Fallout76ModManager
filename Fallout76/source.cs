namespace Fallout76ModInstaller {

    public partial class Fallout76ModInstaller : Form {
        public Fallout76ModInstaller() {
            InitializeComponent();
        }

        private string userPath;
        private string modFolderPath;
        private string myGamesFolderPath;
        private string steamFolderPath;

        private List<string> installedMods;

        private Color folderColor = Color.FromArgb(250, 100, 50);
        private Color fileColor = Color.FromArgb(50, 140, 250);
        
        private void Fallout76ModInstaller_Load(object sender, EventArgs e) {
            init();
        }

        private void init() {
            userPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            if (Environment.OSVersion.Version.Major >= 6)
                userPath = Directory.GetParent(userPath).ToString();

            myGamesFolderPath = Properties.Settings.Default.myGamesPath;
            steamFolderPath = Properties.Settings.Default.steamPath;
            modFolderPath = Properties.Settings.Default.modFolderPath;

            setLabels();
            getInstalledMods();
            loadMods(modFolderPath);
        }

        private void getInstalledMods() {
            if (!Directory.Exists(myGamesFolderPath)) return;
            if (!File.Exists(Path.Combine(myGamesFolderPath, "Fallout76Custom.ini"))) return;

            StreamReader streamReader = new StreamReader(Path.Combine(myGamesFolderPath, "Fallout76Custom.ini"));
            string archiveData = streamReader.ReadToEnd();

            archiveData = archiveData.Substring(archiveData.LastIndexOf("sResourceArchive2List=") + "sResourceArchive2List=".Length).Trim();

            installedMods = new List<string>();
            string[] mods = archiveData.Split(',');

            foreach (string mod in mods)
                installedMods.Add(mod.Trim());
        }

        private void loadMods(string path) {
            if (path == null || !Directory.Exists(path)) return;

            modList.Nodes.Clear();

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            buildTree(directoryInfo, modList.Nodes);

            if(modList.Nodes.Count > 0)
                setAllNodeColors(modList.Nodes[0]);
        }

        private void downloadedModsButton_Click(object sender, EventArgs e) {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {

                try {
                    modFolderPath = folderBrowserDialog.SelectedPath;
                    downloadedModsLabel.Text = modFolderPath;
                    
                    Properties.Settings.Default.modFolderPath = modFolderPath;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
            else return;

            loadMods(modFolderPath);
        }

        private void myGamesButton_Click(object sender, EventArgs e) {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                try {
                    myGamesFolderPath = folderBrowserDialog.SelectedPath;
                    myGamesLabel.Text = myGamesFolderPath;

                    Properties.Settings.Default.myGamesPath = myGamesFolderPath;
                    Properties.Settings.Default.Save();

                    getInstalledMods();
                    loadMods(modFolderPath);
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }

        private void steamButton_Click(object sender, EventArgs e) {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                try {
                    steamFolderPath = folderBrowserDialog.SelectedPath;
                    steamLabel.Text = steamFolderPath;

                    Properties.Settings.Default.steamPath = steamFolderPath;
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }

        private void installButton_Click(object sender, EventArgs e) {
            List<string> modsToInstall = getNodes(modList.Nodes, true);
            List<string> modsToUninstall = getNodes(modList.Nodes, false);

            List<(string, string)> installQueue = new List<(string, string)>();
            List<(string, string)> uninstallQueue = new List<(string, string)>();

            foreach (string mod in modsToInstall) {
                string name = mod.Substring(mod.LastIndexOf("\\") + 1);
                installQueue.Add((name, mod));
            }

            foreach (string mod in modsToUninstall) {
                string name = mod.Substring(mod.LastIndexOf("\\") + 1);
                uninstallQueue.Add((name, mod));
            }

            StreamReader streamReader = new StreamReader(Path.Combine(myGamesFolderPath, "Fallout76Custom.ini"));
            string allData = streamReader.ReadToEnd();
            streamReader.Close();

            string dataWithoutArchive = allData.Substring(0, allData.IndexOf("[Archive]"));
            string dataToOverwrite = "[Archive]\nsResourceArchive2List=";
            string archiveData = allData;

            archiveData = archiveData.Substring(archiveData.LastIndexOf("sResourceArchive2List=") + "sResourceArchive2List=".Length).Trim();

            for (int i = 0; i < uninstallQueue.Count; i++) {
                string modName = uninstallQueue[i].Item1;
                string path = uninstallQueue[i].Item2;

                if (archiveData.Contains(modName)) {
                    string oldData = archiveData;
                    archiveData = archiveData.Replace(", " + modName, "");

                    if (oldData.Equals(archiveData))
                        archiveData = archiveData.Replace(modName + ", ", "");

                    File.Delete(Path.Combine(steamFolderPath, "Data", modName));
                }
            }

            for (int i = 0; i < installQueue.Count; i++) {
                string modName = installQueue[i].Item1;
                string path = installQueue[i].Item2;

                if (!archiveData.Contains(modName))
                    archiveData += (", " + modName);

                File.Copy(Path.Combine(modFolderPath, path.Substring(path.IndexOf("\\") + 1)), Path.Combine(steamFolderPath, "Data", modName), true);
            }

            dataToOverwrite += archiveData;

            string finalData = dataWithoutArchive + dataToOverwrite;

            StreamWriter streamWriter = new StreamWriter(Path.Combine(myGamesFolderPath, "Fallout76Custom.ini"));
            streamWriter.Write(finalData);
            streamWriter.Close();

            MessageBox.Show("Installation/Uninstallation Complete");
        }

        private void buildTree(DirectoryInfo directoryInfo, TreeNodeCollection node) {
            try {
                if (directoryInfo.GetFiles("*.ba2", SearchOption.AllDirectories).Length == 0) return;
            } catch (Exception e) {
                Properties.Settings.Default.modFolderPath = null;
                return;
            }

            TreeNode curNode = node.Add(directoryInfo.Name);
            curNode.ImageIndex = 0;

            foreach (FileInfo file in directoryInfo.GetFiles()) {
                TreeNode added = null;

                if(file.Extension.Equals(".ba2")) 
                    added = curNode.Nodes.Add(file.FullName, file.Name, 1);

                if (added != null) {
                    added.Checked = false;

                    if (installedMods != null && installedMods.Contains(file.Name))
                        added.Checked = true;
                }
            }
            
            foreach (DirectoryInfo subdir in directoryInfo.GetDirectories())
                buildTree(subdir, curNode.Nodes);
        }

        private void setLabels() {
            downloadedModsLabel.Text = (modFolderPath != null ? modFolderPath : "N/A");
            myGamesLabel.Text = (myGamesFolderPath != null ? myGamesFolderPath : "N/A");
            steamLabel.Text = (steamFolderPath != null ? steamFolderPath : "N/A");
        }

        private List<string> getNodes(TreeNodeCollection nodes, bool isChecked) {
            List<string> nodeList = new List<string>();
            if (nodes == null) return nodeList;

            foreach(TreeNode childNode in nodes) {
                if (childNode.Checked == isChecked && childNode.Name.EndsWith(".ba2")) nodeList.Add(childNode.FullPath);
                nodeList.AddRange(getNodes(childNode.Nodes, isChecked));
            }

            return nodeList;
        }

        private void setNodeColor(TreeNode node) {
            //if unchecked or uninitialized
            if (node.StateImageIndex == 0 || node.StateImageIndex == -1) {
                node.ForeColor = Color.White;
            //if mixed or checked
            } else if (node.StateImageIndex == 1 || node.StateImageIndex == 2) {
                if (node.Name.EndsWith(".ba2")) node.ForeColor = fileColor;
                else node.ForeColor = folderColor;
            }
        }

        private void setAllNodeColors(TreeNode root) {
            setNodeColor(root);

            if (root.Nodes.Count == 0) return;

            foreach (TreeNode child in root.Nodes)
                setAllNodeColors(child);
        }

        private void modList_AfterSelect(object sender, TreeViewEventArgs e) {
            modList.SelectedNode = null;
        }

        private void modList_MouseDown(object sender, MouseEventArgs e) {
            setAllNodeColors(modList.Nodes[0]);
        }

        private void quitButton_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void keywordSearch_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode != Keys.Enter) return;

            checkAllNodes(modList.Nodes[0], userInput.Text.Trim());
            userInput.Clear();

            setAllNodeColors(modList.Nodes[0]);
        }

        private void checkAllNodes(TreeNode node, String str) {
            if(node.Name.Contains(str))
                node.Checked = !node.Checked;

            if (node.Nodes.Count > 0)
                foreach (TreeNode child in node.Nodes)
                    checkAllNodes(child, str);
            else return;
        }
    }
}
