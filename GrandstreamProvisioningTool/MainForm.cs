/*
Copyright 2015 Wayne Reynolds

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace Grandstream_Provisioning_Tool
{
    enum ConfigFormat { NONE, GS, XML };
    enum ConfigEncryption { NONE, GS, AESPSK};

    public partial class MainForm : Form
    {
        private BindingList<PKeyValuePair> currentConfig;
        private Dictionary<string, string> pKeyDefinitions;
        private ConfigOverride configOverride;

        private DataGridViewRowContextMenuStripNeededEventArgs contextRow;

        public MainForm()
        {
            InitializeComponent();

            currentConfig = new BindingList<PKeyValuePair>();
            pKeyDefinitions = new Dictionary<string, string>();

            configDataView.DataSource = currentConfig;

            DataGridViewColumn c = configDataView.Columns.GetFirstColumn(DataGridViewElementStates.None);
            c.ReadOnly = true;
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

            DataGridViewColumn v = configDataView.Columns.GetNextColumn(c, DataGridViewElementStates.None,DataGridViewElementStates.None);
            v.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            comboBox1.SelectedItem = "Generic";
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs1Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs2Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs3Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs4Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs5Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs6Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs7Profile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFxs8Profile.DropDownStyle = ComboBoxStyle.DropDownList;



        }

        private int upsertPKeyValuePair(PKeyValuePair newPair, bool appendAtEnd)
        {
            string pKey = newPair.PKey;
            string pValue = newPair.PValue;

            if (pKey.CompareTo(String.Empty) == 0)
                return -2; // no point adding an invalid/blank key


            // Before adding, we need to see if the new pair is a duplicate of one already existing in our currentConfig list.

            foreach (PKeyValuePair pair in currentConfig)
            {
                if (pair.PKey.CompareTo(pKey) == 0) // if a duplicate is detected, only update the value.
                {
                    pair.PValue = pValue;
                    configDataView.UpdateCellValue(1, currentConfig.IndexOf(pair)); // update field if it was a duplicate
                    return currentConfig.IndexOf(pair); // exit here and return where we updated
                }
            }

            // Next we add our pair object and insert it before the current index.
            if (!appendAtEnd)
                currentConfig.Insert(contextRow.RowIndex, newPair);
            else
                currentConfig.Add(newPair);

            return -1;
        }

        private int upsertPKeyValuePair(string pKey, string pValue, bool appendAtEnd)
        {
            PKeyValuePair newPair = new PKeyValuePair { PKey = pKey, PValue = pValue };
            return upsertPKeyValuePair(newPair, appendAtEnd);
        }

        private void loadPKeyDefinitionFile(string path)
        {
            uint keyDefinitionCount = 0;

            System.IO.StreamReader file;
            string fileCurrentLine;

            if (!File.Exists(path))
            {
                MessageBox.Show(@"File path does not exist: " + path, @"Unable to open P-key definitions file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            pKeyDefinitions.Clear();

            file = new System.IO.StreamReader(path);

            while ((fileCurrentLine = file.ReadLine()) != null)
            {
                string pKey;
                string pDef;
                char[] delimeter = { '=' };

                string fileCurrentLineTrimmed = fileCurrentLine.Trim();

                if (String.IsNullOrEmpty(fileCurrentLineTrimmed) || fileCurrentLineTrimmed[0] == '#') // ignore config/template blank lines and comment lines
                    continue;

                string[] fileCurrentLineSplit = fileCurrentLineTrimmed.Split(delimeter, 2);

                pKey = fileCurrentLineSplit[0].Trim();

                if (fileCurrentLineSplit.Length < 2 || String.IsNullOrEmpty(fileCurrentLineSplit[1]))
                    pDef = pKey;
                else
                    pDef = fileCurrentLineSplit[1].Trim();

                try
                {
                    pKeyDefinitions.Add(pKey, pDef);
                }
                catch(ArgumentException) // remove existing duplicate key and add in the 'newer' one. Entries further down the list are allowed to overwrite the prior entries
                {
                    pKeyDefinitions.Remove(pKey);
                    pKeyDefinitions.Add(pKey, pDef);
                }
                keyDefinitionCount++;
            }
            file.Close();
            updateStatusText("Loaded " + keyDefinitionCount + " P-key definitions from " + Path.GetFileName(path));
        }

        private void updateStatusText(string text)
        {
            toolStripStatusLabel1.Text = text;
            statusStrip1.Refresh();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataGridViewRowContextMenuStripNeededEventArgs f = contextRow;
            if (currentConfig.Count == 0)
                return;
            if(currentConfig.ElementAt(f.RowIndex) != null && currentConfig[f.RowIndex] is PKeyValuePair)
                currentConfig.RemoveAt(f.RowIndex);

        }

        private void insertNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddConfigValue input = new AddConfigValue();
            input.ShowDialog();
            string pKey = input.getPKey();
            string pValue = input.getPValue();

            // Next we create our pair object and insert it before the current index.*/
            PKeyValuePair newKeyValuePair = new PKeyValuePair { PKey = pKey, PValue = pValue };
            upsertPKeyValuePair(newKeyValuePair, false);
            

        }

        private void configDataView_RowContextMenuStripNeeded(object sender, DataGridViewRowContextMenuStripNeededEventArgs e)
        {
            if (e.RowIndex == -1)
                return;

            configDataView.ClearSelection();
            configDataView.Rows[e.RowIndex].Selected = true;
            configDataView.CurrentCell = configDataView.Rows[e.RowIndex].Cells[0];


            contextRow = e;

            foreach (DataGridViewRow row in configDataView.SelectedRows) ;
            e.ContextMenuStrip = contextMenuStripConfigRow;

        }

        private void configDataView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DataGridView.HitTestInfo hit = configDataView.HitTest(e.X, e.Y);
                if (hit.Type == DataGridViewHitTestType.None)
                {
                    contextMenuStripConfigEmpty.Show((Control) sender, e.X, e.Y);
                }
            }
        }

        private void addKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddConfigValue input = new AddConfigValue();
            input.ShowDialog();
            string pKey = input.getPKey();
            string pValue = input.getPValue();

            PKeyValuePair newKeyValuePair = new PKeyValuePair { PKey = pKey, PValue = pValue };
            upsertPKeyValuePair(newKeyValuePair, true);
        }

        private void configDataView_DragDrop(object sender, DragEventArgs e)
        {
            string[] filePaths = (string[]) e.Data.GetData(DataFormats.FileDrop);

            string firstFilePath = filePaths[0];
            int keyCount = loadConfigurationFile(firstFilePath);

            if(keyCount >= 0)
            {
                if(keyCount > 0)
                    populateFormConfigOverrides();
                updateStatusText("Loaded " + keyCount + " P-values from " + Path.GetFileName(firstFilePath));
            }
            else
                MessageBox.Show("File path does not exist: " + firstFilePath, "Unable to open file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void populateFormConfigOverrides()
        {
            if (configOverride == null)
                return;

            for(int x = 0; x < configOverride.dict.Count; x++)
            {
                KeyValuePair<string, string> pair;
                pair = configOverride.dict.ElementAt(x);

                switch(pair.Key)
                {
                    // populate user id fields

                    case "FXS1_USERID":
                        textBoxFxs1UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS2_USERID":
                        textBoxFxs2UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS3_USERID":
                        textBoxFxs3UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS4_USERID":
                        textBoxFxs4UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS5_USERID":
                        textBoxFxs5UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS6_USERID":
                        textBoxFxs6UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS7_USERID":
                        textBoxFxs7UserId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS8_USERID":
                        textBoxFxs8UserId.Text = getPValueFromPKey(pair.Value); break;

                    // populate auth id fields

                    case "FXS1_AUTHID":
                        textBoxFxs1AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS2_AUTHID":
                        textBoxFxs2AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS3_AUTHID":
                        textBoxFxs3AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS4_AUTHID":
                        textBoxFxs4AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS5_AUTHID":
                        textBoxFxs5AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS6_AUTHID":
                        textBoxFxs6AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS7_AUTHID":
                        textBoxFxs7AuthId.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS8_AUTHID":
                        textBoxFxs8AuthId.Text = getPValueFromPKey(pair.Value); break;

                    // populate password fields

                    case "FXS1_PASSWORD":
                        textBoxFxs1Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS2_PASSWORD":
                        textBoxFxs2Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS3_PASSWORD":
                        textBoxFxs3Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS4_PASSWORD":
                        textBoxFxs4Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS5_PASSWORD":
                        textBoxFxs5Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS6_PASSWORD":
                        textBoxFxs6Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS7_PASSWORD":
                        textBoxFxs7Password.Text = getPValueFromPKey(pair.Value); break;
                    case "FXS8_PASSWORD":
                        textBoxFxs8Password.Text = getPValueFromPKey(pair.Value); break;

                    case "FXS1_PROFILE":
                        if (comboBox1.SelectedItem.ToString().CompareTo("HT502") == 0) // UI fix for HT502 lacking dedicated Pvalue for this
                        {
                            if (getPValueFromPKey(pair.Value).CompareTo("1") == 0)
                                comboBoxFxs1Profile.SelectedItem = "Profile 1";
                        }
                        else if (getPValueFromPKey(pair.Value).CompareTo("0") == 0) comboBoxFxs1Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs1Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs1Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS2_PROFILE":
                        if (comboBox1.SelectedItem.ToString().CompareTo("HT502") == 0) // UI fix for HT502 lacking dedicated Pvalue for this
                        {
                            if (getPValueFromPKey(pair.Value).CompareTo("1") == 0)
                                comboBoxFxs2Profile.SelectedItem = "Profile 2";
                        }
                        else if (getPValueFromPKey(pair.Value).CompareTo("0") == 0) comboBoxFxs2Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs2Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs2Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS3_PROFILE":
                        if (getPValueFromPKey(pair.Value).CompareTo("0") == 0)      comboBoxFxs3Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs3Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs3Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS4_PROFILE":
                        if (getPValueFromPKey(pair.Value).CompareTo("0") == 0)      comboBoxFxs4Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs4Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs4Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS5_PROFILE":
                        if (getPValueFromPKey(pair.Value).CompareTo("0") == 0)      comboBoxFxs5Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs5Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs5Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS6_PROFILE":
                        if (getPValueFromPKey(pair.Value).CompareTo("0") == 0)      comboBoxFxs6Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs6Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs6Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS7_PROFILE":
                        if (getPValueFromPKey(pair.Value).CompareTo("0") == 0)      comboBoxFxs7Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs7Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs7Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS8_PROFILE":
                        if (getPValueFromPKey(pair.Value).CompareTo("0") == 0)      comboBoxFxs8Profile.SelectedItem = "Profile 1";
                        else if (getPValueFromPKey(pair.Value).CompareTo("1") == 0) comboBoxFxs8Profile.SelectedItem = "Profile 2";
                        else if (getPValueFromPKey(pair.Value).CompareTo("2") == 0) comboBoxFxs8Profile.SelectedItem = "Profile 3";
                        break;

                    case "FXS1_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs1Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS2_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs2Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS3_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs3Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS4_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs4Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS5_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs5Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS6_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs6Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS7_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs7Profile.SelectedItem = "Disabled";
                        break;

                    case "FXS8_ENABLED":
                        if (getPValueFromPKey(pair.Value).CompareTo("1") != 0) comboBoxFxs8Profile.SelectedItem = "Disabled";
                        break;

                    // SIP PROFILE 1 STUFF
                    case "PROFILE1_SIPSERVER":
                        textBoxProfile1SipServer.Text = getPValueFromPKey(pair.Value); break;
                    case "PROFILE1_FAILOVERSIPSERVER":
                        textBoxProfile1FailoverSipServer.Text = getPValueFromPKey(pair.Value); break;
                    case "PROFILE1_OUTBOUNDPROXY":
                        textBoxProfile1OutboundProxy.Text = getPValueFromPKey(pair.Value); break;

                    // SIP PROFILE 2 STUFF
                    case "PROFILE2_SIPSERVER":
                        textBoxProfile2SipServer.Text = getPValueFromPKey(pair.Value); break;
                    case "PROFILE2_FAILOVERSIPSERVER":
                        textBoxProfile2FailoverSipServer.Text = getPValueFromPKey(pair.Value); break;
                    case "PROFILE2_OUTBOUNDPROXY":
                        textBoxProfile2OutboundProxy.Text = getPValueFromPKey(pair.Value); break;
                    default:
                        continue;
                }
            }
        }

        private string getPValueFromPKey(string input)
        {
            string ret = "";


            foreach (PKeyValuePair pair in currentConfig)
            {
                if (pair.PKey.CompareTo(input) == 0)
                    ret = pair.PValue;
            }
            return ret;
        }

        private int loadConfigurationFile(string filePath)
        {
            bool currentConfigCleared = false;
            int keyCount = 0;

            System.IO.StreamReader file;
            string fileCurrentLine;

            if (!File.Exists(filePath))
                return -1;
 

            file = new System.IO.StreamReader(filePath);

            while ((fileCurrentLine = file.ReadLine()) != null)
            {
                string fileCurrentLineTrimmed = fileCurrentLine.Trim();
                if (String.IsNullOrEmpty(fileCurrentLineTrimmed) || fileCurrentLineTrimmed[0] == '#') // ignore config/template blank lines and comment lines
                    continue;
                else if (fileCurrentLineTrimmed[0] == 'P')
                {
                    string pKey;
                    string pValue;
                    char[] delimeter = { '=' };
                    string[] fileCurrentLineSplit = fileCurrentLineTrimmed.Split(delimeter, 2);

                    pKey = fileCurrentLineSplit[0].Trim();

                    if (fileCurrentLineSplit.Length < 2 || String.IsNullOrEmpty(fileCurrentLineSplit[1]))
                        pValue = String.Empty;
                    else
                        pValue = fileCurrentLineSplit[1].Trim();

                    PKeyValuePair newPair = new PKeyValuePair { PValue = pValue, PKey = pKey };

                    if (!currentConfigCleared)
                    {
                        currentConfig.Clear();
                        currentConfigCleared = true;
                    }

                    currentConfig.Add(newPair);
                    keyCount++;
                }
            }
            file.Close();

            return keyCount;
        }

        private void configDataView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void configDataView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            string cellPKey = currentConfig[e.RowIndex].PKey;
            string pValue;

            if (pKeyDefinitions.TryGetValue(cellPKey, out pValue))
                updateStatusText(pValue);
            else
                updateStatusText(cellPKey);
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {

            ConfigFormat usingConfigFormat = ConfigFormat.NONE;
            ConfigEncryption usingConfigEncryption = ConfigEncryption.NONE;

            string deviceMac = maskedTextBoxDeviceMac.Text.Replace(@":", @"").ToLower().Trim();

            if (radioButtonFormatXml.Checked && !radioButtonFormatProprietary.Checked)
                usingConfigFormat = ConfigFormat.XML;
            else if (!radioButtonFormatXml.Checked && radioButtonFormatProprietary.Checked)
                usingConfigFormat = ConfigFormat.GS;
            else
            {
                MessageBox.Show("You must select an XML format or legacy format.", "Invalid format selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (checkBoxEncryption.Checked)
            {
                if (usingConfigFormat == ConfigFormat.XML)
                {
                    if (textBoxEncryptionSecret.Text.Length > 0)
                        usingConfigEncryption = ConfigEncryption.AESPSK;
                    else
                    {
                        MessageBox.Show("The XML Key field should have a key/password when using encryption with XML format files.", "XML format encryption key/password not specified", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else if (usingConfigFormat == ConfigFormat.GS)
                    usingConfigEncryption = ConfigEncryption.GS;
                else
                {
                    MessageBox.Show("The specified format does not support encryption.", "Format does not support encryption", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if(usingConfigFormat == ConfigFormat.GS)
            {
                if (deviceMac.Length != 12)
                {
                    MessageBox.Show("When using Grandstream proprietary file format you must specify a MAC address.", "Format type requires MAC Address", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Replace / Add values to the configuration value list according to what is in the GUI form

            string value;
            if (configOverride != null)
            {
                // FXS 1
                if (configOverride.dict.TryGetValue("FXS1_PROFILE", out value))
                    if (comboBoxFxs1Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs1Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs1Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS1_ENABLED", out value)) // For devices like the HT502, this may overwrite the _PROFILE value!
                    if (comboBoxFxs1Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs1UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS1_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs1UserId.Text, true);
                if (textBoxFxs1AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS1_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs1AuthId.Text, true);
                if (textBoxFxs1Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS1_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs1Password.Text, true);

                // FXS 2
                if (configOverride.dict.TryGetValue("FXS2_PROFILE", out value))
                    if (comboBoxFxs2Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs2Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs2Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS2_ENABLED", out value))
                    if (comboBoxFxs2Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs2UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS2_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs2UserId.Text, true);
                if (textBoxFxs2AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS2_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs2AuthId.Text, true);
                if (textBoxFxs2Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS2_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs2Password.Text, true);

                // FXS 3
                if (configOverride.dict.TryGetValue("FXS3_PROFILE", out value))
                    if (comboBoxFxs3Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs3Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs3Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS3_ENABLED", out value))
                    if (comboBoxFxs3Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs3UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS3_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs3UserId.Text, true);
                if (textBoxFxs3AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS3_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs3AuthId.Text, true);
                if (textBoxFxs3Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS3_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs3Password.Text, true);

                // FXS 4
                if (configOverride.dict.TryGetValue("FXS4_PROFILE", out value))
                    if (comboBoxFxs4Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs4Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs4Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS4_ENABLED", out value))
                    if (comboBoxFxs4Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs4UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS4_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs4UserId.Text, true);
                if (textBoxFxs4AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS4_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs4AuthId.Text, true);
                if (textBoxFxs4Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS4_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs4Password.Text, true);

                // FXS 5
                if (configOverride.dict.TryGetValue("FXS5_PROFILE", out value))
                    if (comboBoxFxs5Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs5Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs5Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS5_ENABLED", out value))
                    if (comboBoxFxs5Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs5UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS5_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs5UserId.Text, true);
                if (textBoxFxs5AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS5_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs5AuthId.Text, true);
                if (textBoxFxs5Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS5_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs5Password.Text, true);

                // FXS 6
                if (configOverride.dict.TryGetValue("FXS6_PROFILE", out value))
                    if (comboBoxFxs6Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs6Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs6Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS6_ENABLED", out value))
                    if (comboBoxFxs6Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs6UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS6_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs6UserId.Text, true);
                if (textBoxFxs6AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS6_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs6AuthId.Text, true);
                if (textBoxFxs6Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS6_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs6Password.Text, true);

                // FXS 7
                if (configOverride.dict.TryGetValue("FXS7_PROFILE", out value))
                    if (comboBoxFxs7Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs7Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs7Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS7_ENABLED", out value))
                    if (comboBoxFxs7Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs7UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS7_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs7UserId.Text, true);
                if (textBoxFxs7AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS7_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs7AuthId.Text, true);
                if (textBoxFxs7Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS7_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs7Password.Text, true);

                // FXS 8
                if (configOverride.dict.TryGetValue("FXS8_PROFILE", out value))
                    if (comboBoxFxs8Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else if (comboBoxFxs8Profile.SelectedItem.ToString().CompareTo("Profile 1") == 0)
                        upsertPKeyValuePair(value, "1", true);
                    else if (comboBoxFxs8Profile.SelectedItem.ToString().CompareTo("Profile 2") == 0)
                        upsertPKeyValuePair(value, "2", true);
                if (configOverride.dict.TryGetValue("FXS8_ENABLED", out value))
                    if (comboBoxFxs8Profile.SelectedItem.ToString().CompareTo("Disabled") == 0)
                        upsertPKeyValuePair(value, "0", true);
                    else
                        upsertPKeyValuePair(value, "1", true);

                if (textBoxFxs8UserId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS8_USERID", out value))
                        upsertPKeyValuePair(value, textBoxFxs8UserId.Text, true);
                if (textBoxFxs8AuthId.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS8_AUTHID", out value))
                        upsertPKeyValuePair(value, textBoxFxs8AuthId.Text, true);
                if (textBoxFxs8Password.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("FXS8_PASSWORD", out value))
                        upsertPKeyValuePair(value, textBoxFxs8Password.Text, true);

                // Profile 1

                if (textBoxProfile1SipServer.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("PROFILE1_SIPSERVER", out value))
                        upsertPKeyValuePair(value, textBoxProfile1SipServer.Text, true);
                if (textBoxProfile1FailoverSipServer.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("PROFILE1_FAILOVERSIPSERVER", out value))
                        upsertPKeyValuePair(value, textBoxProfile1FailoverSipServer.Text, true);
                if (textBoxProfile1OutboundProxy.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("PROFILE1_OUTBOUNDPROXY", out value))
                        upsertPKeyValuePair(value, textBoxProfile1OutboundProxy.Text, true);

                // Profile 2

                if (textBoxProfile2SipServer.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("PROFILE2_SIPSERVER", out value))
                        upsertPKeyValuePair(value, textBoxProfile2SipServer.Text, true);
                if (textBoxProfile2FailoverSipServer.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("PROFILE2_FAILOVERSIPSERVER", out value))
                        upsertPKeyValuePair(value, textBoxProfile2FailoverSipServer.Text, true);
                if (textBoxProfile2OutboundProxy.Text.Length != 0)
                    if (configOverride.dict.TryGetValue("PROFILE2_OUTBOUNDPROXY", out value))
                        upsertPKeyValuePair(value, textBoxProfile2OutboundProxy.Text, true);
            }

            // end override processing

            string cfgDefaultName = "cfg";

            if (!String.IsNullOrEmpty(deviceMac)) // append the device mac to the default device name, if applicable
                cfgDefaultName += deviceMac;

            saveFileDialog1.FileName = cfgDefaultName;

            if (usingConfigFormat == ConfigFormat.XML)
            {
                saveFileDialog1.Filter = "XML file (.xml)|*.xml";
                saveFileDialog1.DefaultExt = "xml";
                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;

                if (string.IsNullOrEmpty(saveFileDialog1.FileName))
                    return;

                try
                {

                    if (usingConfigEncryption == ConfigEncryption.AESPSK)
                        ConfigWriter.WriteConfigToXmlFile(currentConfig.ToList(), deviceMac, saveFileDialog1.FileName, textBoxEncryptionSecret.Text);
                    else
                        ConfigWriter.WriteConfigToXmlFile(currentConfig.ToList(), deviceMac, saveFileDialog1.FileName, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An exception occured when attempting to create the configuration file: " + ex.Message, "Failed");
                    return;
                }

                MessageBox.Show("XML format configuration file saved to " + saveFileDialog1.FileName, "Success");

            }
            else if(usingConfigFormat == ConfigFormat.GS)
            {
                saveFileDialog1.Filter = "";
                saveFileDialog1.DefaultExt = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;

                if (string.IsNullOrEmpty(saveFileDialog1.FileName))
                    return;

                try
                {
                    if (usingConfigEncryption == ConfigEncryption.GS)
                        ConfigWriter.WriteConfigToLegacyFile(currentConfig.ToList(), deviceMac, saveFileDialog1.FileName, true);
                    else
                        ConfigWriter.WriteConfigToLegacyFile(currentConfig.ToList(), deviceMac, saveFileDialog1.FileName, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An exception occured when attempting to create the configuration file: " + ex.Message, "Failed");
                    return;
                }

                if (usingConfigEncryption == ConfigEncryption.GS)
                    MessageBox.Show("Legacy format configuration file created and saved to " + saveFileDialog1.FileName +"\n\nWarning: Legacy encryption is NOT secure. If your configuration file contains sensitive information, use XML format and encryption.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                    MessageBox.Show("Legacy format configuration file created and saved to " + saveFileDialog1.FileName, "Success");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem.ToString().CompareTo("HT502") == 0)
            {
                groupBoxFXS1.Enabled = true;
                groupBoxFXS2.Enabled = true;
                groupBoxFXS3.Enabled = false;
                groupBoxFXS4.Enabled = false;
                groupBoxFXS5.Enabled = false;
                groupBoxFXS6.Enabled = false;
                groupBoxFXS7.Enabled = false;
                groupBoxFXS8.Enabled = false;

                loadPKeyDefinitionFile(Directory.GetCurrentDirectory() + @"\deviceinfo\HT502_definitions.txt");
                configOverride = new ConfigOverride("HT502", Directory.GetCurrentDirectory() + @"\deviceinfo\HT502_override.txt");
            }
            else if(comboBox1.SelectedItem.ToString().CompareTo("GXW4004") == 0)
            {
                groupBoxFXS1.Enabled = true;
                groupBoxFXS2.Enabled = true;
                groupBoxFXS3.Enabled = true;
                groupBoxFXS4.Enabled = true;
                groupBoxFXS5.Enabled = false;
                groupBoxFXS6.Enabled = false;
                groupBoxFXS7.Enabled = false;
                groupBoxFXS8.Enabled = false;

                configOverride = new ConfigOverride("GXW4004", Directory.GetCurrentDirectory() + @"\deviceinfo\GXW4004_override.txt");
            }
            else if(comboBox1.SelectedItem.ToString().CompareTo("GXW4008") == 0)
            {
                groupBoxFXS1.Enabled = true;
                groupBoxFXS2.Enabled = true;
                groupBoxFXS3.Enabled = true;
                groupBoxFXS4.Enabled = true;
                groupBoxFXS5.Enabled = true;
                groupBoxFXS6.Enabled = true;
                groupBoxFXS7.Enabled = true;
                groupBoxFXS8.Enabled = true;
            }
            else if (comboBox1.SelectedItem.ToString().CompareTo("Generic") == 0)
            {
                groupBoxFXS1.Enabled = false;
                groupBoxFXS2.Enabled = false;
                groupBoxFXS3.Enabled = false;
                groupBoxFXS4.Enabled = false;
                groupBoxFXS5.Enabled = false;
                groupBoxFXS6.Enabled = false;
                groupBoxFXS7.Enabled = false;
                groupBoxFXS8.Enabled = false;


                configOverride = null;
            }

            if(currentConfig.Count > 0 )
            {
                populateFormConfigOverrides();
            }
        }

        private void loadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadConfigTemplateToolStripMenuItem_Click(sender, e);
        }

        private void loadConfigTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = String.Empty;
            openFileDialog1.Filter = "Text File (*.txt)|*.txt|All files|*";
            openFileDialog1.DefaultExt = "txt";
            if (openFileDialog1.ShowDialog() != DialogResult.Cancel)
            {
                int keyCount = loadConfigurationFile(openFileDialog1.FileName);
                if (keyCount > 0)
                    populateFormConfigOverrides();
                updateStatusText("Loaded " + keyCount + " P-values from " + Path.GetFileName(openFileDialog1.FileName));
            }
        }

        private void maskedTextBoxDeviceMac_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 about = new AboutBox1();
            about.ShowDialog();
        }
    }

    public class PKeyValuePair
    {
        public string PKey { get; set; }
        public string PValue { get; set; }
    }
}
