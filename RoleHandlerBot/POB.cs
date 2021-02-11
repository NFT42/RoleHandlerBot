using System;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
namespace RoleHandlerBot {
    public class POB {
        public POB() {
        }

        public static List<string> GetHash() {
            List<string> listA = new List<string>();
            int counter = 0;
            using (var reader = new StreamReader(@"results-20210211-101616.csv")) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    if (counter != 0)
                        listA.Add(values[0]);
                    counter++;
                }
            }
            return listA;
        }

        public static void GetColors() {
            var list = GetHash();
            Console.WriteLine($"Total of {list.Count} hashes to check");
            var data = "";
            int counter = 0;
            foreach (var hash in list) {
                counter++;
                using (System.Net.WebClient wc = new System.Net.WebClient()) {
                    try {
                        data = wc.DownloadString("https://pob.studio/api/prerender?hash=" + hash);
                    }
                    catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                }
                var json = JObject.Parse(data);
                var palettes = json["gene"]["foreground"]["colorPalletes"] as JArray;
                if (palettes.Count > 5)
                    Console.WriteLine($"{counter} - {hash}\n{palettes.Count} colorsr");
            }
        }
    }
}
