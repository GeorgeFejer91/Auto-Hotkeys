using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AutoHotkeys
{
    [DataContract]
    internal sealed class Settings
    {
        [DataMember] public bool AutoStart = true;
        [DataMember] public List<string> DisabledActions = new List<string>();
        [OnDeserializing] private void BeforeRead(StreamingContext context) { AutoStart = true; DisabledActions = new List<string>(); }
        internal static Settings Load()
        {
            string file = Path.Combine(AppPaths.StateDirectory, "settings.json");
            if (!File.Exists(file)) return new Settings();
            try
            {
                using (FileStream stream = File.OpenRead(file))
                {
                    Settings settings = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(stream);
                    if (settings.DisabledActions == null) settings.DisabledActions = new List<string>();
                    return settings;
                }
            }
            catch (Exception ex) { AppPaths.Log("Settings could not be read: " + ex.Message); return new Settings(); }
        }
        internal void Save()
        {
            Directory.CreateDirectory(AppPaths.StateDirectory);
            string file = Path.Combine(AppPaths.StateDirectory, "settings.json");
            string temp = file + ".tmp";
            using (FileStream stream = File.Create(temp)) new DataContractJsonSerializer(typeof(Settings)).WriteObject(stream, this);
            if (File.Exists(file)) File.Replace(temp, file, null);
            else File.Move(temp, file);
        }
    }
}
