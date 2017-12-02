using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace BvExport.Settings
{
    public class ExportSettings: FeedSettings
    {
        public string Compression { get; set; }
        public XNamespace XNamespace { get; set; }

        public ExportSettings(ID itemId, string language)
        {

            Itm = GetSettingItem(itemId, language);
            if (Itm == null)
            {
                Log.Error("Export Settings -- Error -- Unable to find the settings item");
                Log.Info("The settings item could not be found under the tree item -- {1488A3A0-343F-4953-B4F0-B34A175EDC1D}/{A0F41646-0A7C-4C4D-8EEC-E7E20CD7F5C3} ");
                Log.Info("Also check that the config file has the correct ID reference");
                return;
            }

            var exportPrefix = Itm.Fields[ID.Parse(Feed.Config.ExportPrefix)].Value;
            var month = DateTime.UtcNow.ToString("MMM");
            var date = DateTime.UtcNow.ToString("dd");
            var year = DateTime.UtcNow.ToString("yyyy");
            Url = Itm.Fields[Feed.Config.ExportUrl].Value;
            FolderPath = Itm.Fields[ID.Parse(Feed.Config.ExportFolderPath)].Value;
            Compression = Itm.Fields[ID.Parse(Feed.Config.Compression)].Value;
            XNamespace = Itm.Fields[ID.Parse(Feed.Config.ExportXnamespace)].Value;
            var extension = !Compression.IsNullOrEmpty() ? ".gz" : "";
            FileName = string.Format("{0}{1}-{2}-{3}.xml{4}", exportPrefix, month, date, year, extension);
            Username = Itm.Fields[ID.Parse(Feed.Config.Username)].Value;
            Password = Itm.Fields[ID.Parse(Feed.Config.Password)].Value;
            Localfolderpath = Itm.Fields[ID.Parse(Feed.Config.LocalFolderPath)].Value;
            FullLocalPath = Localfolderpath + FileName;
        }
    }
}