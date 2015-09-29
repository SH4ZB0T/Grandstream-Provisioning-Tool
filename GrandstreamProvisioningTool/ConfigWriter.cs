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
using System.Text;
using System.Xml;
using System.IO;

namespace Grandstream_Provisioning_Tool
{
    class ConfigWriter
    {
        static public void WriteConfigToXmlFile(List<PKeyValuePair> config, string gsMac, string filePath, string encryptionPassword)
        {

            MemoryStream configString = new MemoryStream();

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Encoding = Encoding.UTF8;

            XmlWriter writer = XmlWriter.Create(configString,writerSettings);

            writer.WriteStartDocument();

            writer.WriteStartElement("gs_provision");
            writer.WriteAttributeString("version", "1");

            if (!String.IsNullOrEmpty(gsMac)) // if available, include the device mac
            {
                writer.WriteStartElement("mac");
                writer.WriteString(gsMac);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("config");
            writer.WriteAttributeString("version", "1");

            foreach (PKeyValuePair pair in config)
            {
                writer.WriteStartElement(pair.PKey);
                writer.WriteString(pair.PValue);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // end config
            writer.WriteEndElement(); // end gs_provision

            writer.WriteEndDocument();
            writer.Close();

            if (!String.IsNullOrEmpty(encryptionPassword))
            {
                byte[] configEncrypted = net.infodox.AESCryptNET.AESCryptNET.EncryptCBC(configString.ToArray(), encryptionPassword, 256);

                File.WriteAllBytes(filePath, configEncrypted);
            }
            else
                File.WriteAllBytes(filePath, configString.ToArray());
        }

        static public void WriteConfigToLegacyFile(List<PKeyValuePair> config, string gsMac, string filePath, bool useEncryption)
        {
            WriteConfigToLegacyFileApi(config, gsMac, filePath, useEncryption);
        }

        static public void WriteConfigToLegacyFileApi(List<PKeyValuePair> config, string gsMac, string filePath, bool useEncryption)
        {
            StringBuilder configString = new StringBuilder();

            foreach(PKeyValuePair pair in config)
            {
                configString.Append(pair.PKey);
                configString.Append('=');
                configString.Append(pair.PValue);
                configString.Append('\n');
            }

            byte[] configEncoded = com.grandstream.provision.TextEncoder.Encode(configString.ToString(), gsMac, useEncryption);

            File.WriteAllBytes(filePath, configEncoded);
        }
    }
}