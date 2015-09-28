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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Grandstream_Provisioning_Tool
{
    public partial class AddConfigValue : Form
    {
        private string pKey;
        private string pValue;

        public string getPKey() { return pKey; }
        public string getPValue() { return pValue; }

        public AddConfigValue()
        {
            InitializeComponent();
            pKey = String.Empty;
            pValue = String.Empty;
        }

        private void AddConfigValue_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            pKey = @"P" + textBox1.Text;
            pValue = textBox2.Text.Trim();
            this.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
                SystemSounds.Beep.Play();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string trimmedInput = textBox1.Text.Trim();
            if(trimmedInput.CompareTo(String.Empty) != 0 && !containsOnlyDigits(trimmedInput))
            {
                textBox1.Text = String.Empty;
                SystemSounds.Beep.Play();
            }
            else
            {
                textBox1.Text = trimmedInput;
            }
        }
        private static bool containsOnlyDigits(string input)
        {
            return Regex.IsMatch(input, @"^\d+$");
        }
    }
}
