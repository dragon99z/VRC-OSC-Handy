using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace VRC_OSC_Handy
{
    // Config file load/save (vrc_config.json, config.json) and the shutdown sequence
    // that stops every background poller before the app closes. Split out of
    // MainWindow.xaml.cs since it's a self-contained I/O concern.
    public partial class MainWindow
    {
        public void genConfig(string filename, out JObject json)
        {
            if (File.Exists(cfg_path + filename))
            {
                bool newValues = false;

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetName().Name.Replace("-", "_") + ".resource." + filename;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader recourceReader = new StreamReader(stream))
                using (StreamReader file = File.OpenText(cfg_path + filename))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    string result = recourceReader.ReadToEnd();
                    JObject internCFG = JObject.Parse(result);
                    json = (JObject)JToken.ReadFrom(reader);

                    foreach (var item in internCFG)
                    {
                        foreach (var item1 in (JObject)item.Value)
                        {
                            JObject objetc = (JObject)json[item.Key];
                            if (!objetc.ContainsKey(item1.Key))
                            {
                                objetc.Add(item1.Key, item1.Value);
                                json[item.Key] = objetc;
                                newValues = true;
                            }
                        }
                    }
                }

                if (newValues)
                    File.WriteAllText(cfg_path + filename, json.ToString());
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetName().Name.Replace("-", "_") + ".resource." + filename;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    json = JObject.Parse(result);
                    File.WriteAllText(cfg_path + filename, json.ToString());
                }
            }
        }

        public static void stopAll()
        {
            if (config.SpotifyConfig.Enabled && spotify != null)
            {
                updateName.stop();

                updateProgess.stop();

                updateSpotify.stop();
            }

            osc.stop();


            updateToken.Cancel();
            updateVMToken.Cancel();
            remoteControle?.LogOut(); // null when VoiceMeeter isn't installed (see MainWindow ctor)
            if (wisper.isRunning)
                wisper.stop();

        }

        public static void saveAll()
        {
            var vrcJsonString = JsonConvert.SerializeObject(vrcConfig, Formatting.Indented);
            File.WriteAllText(cfg_path + "vrc_config.json", vrcJsonString);

            var configJsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(cfg_path + "config.json", configJsonString);
        }
    }
}
