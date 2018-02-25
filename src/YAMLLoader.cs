using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SharpYaml.Schemas;
using SharpYaml.Serialization;
using UnityEngine;
using Object = System.Object;

namespace KSP_YAML
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class YAMLLoader : MonoBehaviour
    {
        /// <summary>
        /// Assigs the config filetype to yaml files
        /// </summary>
        public static readonly UrlDir.ConfigFileType yamlConfigFileType =
            new UrlDir.ConfigFileType(UrlDir.FileType.Config, new[] {"yaml", "yml"});
        
        /// <summary>
        /// Register the callback for GameDatabase
        /// </summary>
        void Awake()
        {
            GameEvents.OnGameDatabaseLoaded.Add(OnGameDatabaseLoaded);
        }

        /// <summary>
        /// Transform all yaml files in GameDatabase into ConfigNodes
        /// </summary>
        void OnGameDatabaseLoaded()
        {
            Debug.Log("HI");
            Debug.Log(GameDatabase.Instance.root);
            CrawlGameDatabase(GameDatabase.Instance.root);
            
        }

        /// <summary>
        /// Iterates over the files in a directory and transforms YAML files to ConfigNodes
        /// </summary>
        /// <param name="directory"></param>
        void CrawlGameDatabase(UrlDir directory)
        {
            for (Int32 i = 0; i < directory.files.Count; i++)
            {
                UrlDir.UrlFile file = directory.files[i];
                
                // Did we found a YAML file?
                Debug.Log(file.fileExtension);
                if (file.fileExtension == "yaml" || file.fileExtension == "yml" || file.fileExtension == "json")
                {
                    file.ConfigureFile(new[] {yamlConfigFileType});
                    
                    // Load the yaml
                    String yaml = "";
                    foreach (String line in File.ReadAllLines(file.fullPath))
                    {
                        // Escape identifiers that aren't allowed in the YAML spec
                        yaml += Regex.Replace(line, @"^(\s*)?([&!|%@].*):", "$1\"$2\":") +"\n";
                    }
                    
                    Dictionary<String, Dictionary<Object, Object>> data =
                        new Serializer().Deserialize<Dictionary<String, Dictionary<Object, Object>>>(yaml);
                    foreach (KeyValuePair<String, Dictionary<Object, Object>> kVP in data)
                    {
                        file.AddConfig(YAMLToConfigNode(kVP.Key, kVP.Value));
                    }
                }
                
                // Reassign the file 
                directory.files[i] = file;
            }
            
            // Crawl the subdirectories of the directory
            for (Int32 i = 0; i < directory.children.Count; i++)
            {
                CrawlGameDatabase(directory.children[i]);
            }
        }

        /// <summary>
        /// Converts a section from a YAML file into a config node
        /// </summary>
        private ConfigNode YAMLToConfigNode(String name, Object value, ConfigNode node = null)
        {
            // If the node is empty, create it
            ConfigNode _node = node == null ? node = new ConfigNode(name) : node.AddNode(name);
            
            // Is the object a list?
            if (value is IList)
            {
                List<Object> list = value as List<Object>;
                foreach (Object obj in list)
                {
                    // Is the element another list or a dictionary?
                    if (obj is IList || obj is IDictionary)
                    {
                        _node = YAMLToConfigNode("Key", obj, _node);
                    }

                    // Is the element a primitive type?
                    else
                    {
                        _node.AddValue("key", obj.ToString());
                    }
                }
            }
            
            // Is the object a dictionary?
            else if (value is IDictionary)
            {
                Dictionary<Object, Object> dictionary = value as Dictionary<Object, Object>;
                foreach (KeyValuePair<Object, Object> kVP in dictionary)
                {
                    // Is the element another list or a dictionary?
                    if (kVP.Value is IList || kVP.Value is IDictionary)
                    {
                        _node = YAMLToConfigNode(kVP.Key.ToString(), kVP.Value, _node);
                    }

                    // Is the element a primitive type?
                    else
                    {
                        _node.AddValue(kVP.Key.ToString(), kVP.Value.ToString());
                    }
                }
            }

            return node;
        }
    }
}