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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace Grandstream_Provisioning_Tool
{
    class ConfigOverride
    {
        private static string ModelIdPattern = @"\[([A-Za-z0-9]*)\]";

        public Dictionary<string, string> dict;

        private string model;
        public ConfigOverride(string model, string overrideFilePath)
        {
            this.model = model;
            dict = new Dictionary<string, string>();

            if(!LoadConfiguration(overrideFilePath))
            {
                throw new Exception();
            }
        }
        public bool LoadConfiguration(string filePath)
        {
            System.IO.StreamReader file;
            string fileCurrentLine;
            bool modelIdFound = false;
            char[] delimeter = { '=' };
            string oKey;
            string oValue;

            if (!File.Exists(filePath))
            {
                throw new System.IO.FileNotFoundException("Could not find override configuration file", filePath);
            }

            file = new System.IO.StreamReader(filePath);

            // first locate model id
            while ((fileCurrentLine = file.ReadLine()) != null)
            {
                string fileCurrentLineTrimmed = fileCurrentLine.Trim();
                if (!modelIdFound)
                {
                    Match modelIdMatch = Regex.Match(fileCurrentLineTrimmed, ConfigOverride.ModelIdPattern);
                    if (modelIdMatch.Success)
                    {
                        Group g = modelIdMatch.Groups[1];

                        if (model.CompareTo(g.Value) == 0)
                            modelIdFound = true;
                    }
                }
                else // once we match the model ID we load the configuration
                {
                    if (fileCurrentLineTrimmed != String.Empty && fileCurrentLineTrimmed[0] == '#')
                        continue;

                    string[] fileCurrentLineSplit = fileCurrentLineTrimmed.Split(delimeter, 2);

                    oKey = fileCurrentLineSplit[0].Trim();

                    if (fileCurrentLineSplit.Length < 2 || String.IsNullOrEmpty(fileCurrentLineSplit[1]))
                        continue;
                    else
                        oValue = fileCurrentLineSplit[1].Trim();

                    try
                    {
                        dict.Add(oKey, oValue);
                    }
                    catch (ArgumentException) // remove existing duplicate key and add in the 'newer' one. Entries further down the list are allowed to overwrite the prior entries
                    {
                        dict.Remove(oKey);
                        dict.Add(oKey, oValue);
                    }
                }

            }
            file.Close();

            return modelIdFound;
        }

    }
}
