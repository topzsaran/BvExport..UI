using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace BvExport.Settings
{
    public class ImportSettings:FeedSettings
    {
        public XNamespace XNamespace { get; set; }

        public ImportSettings(ID itemId)
        {
            string lan = string.Empty;
            Itm = GetSettingsItem(itemId, lan);
            if (Itm == null)
            {
                Log.Error("Export Settings -- Error -- Unable to find the settings item");
                Log.Debug("The settings item could not be found under the tree item -- {A0F41646-0A7C-4C4D-8EEC-E7E20CD7F5C3} ");
                Log.Debug("Also check that the config file has the correct ID reference");
                return;
            }

            Url = Itm.Fields[ID.Parse(Feed.Config.ImportUrl)].Value;
            FolderPath = Itm.Fields[ID.Parse(Feed.Config.ImportFolderPath)].Value;
            FileName = Itm.Fields[ID.Parse(Feed.Config.ImportPrefix)].Value;
            UserName = Itm.Fields[ID.Parse(Feed.Config.Username)].Value;
            Password = Itm.Fields[ID.Parse(Feed.Config.Password)].Value;
            XNamespace = Itm.Fields[ID.Parse(Feed.Config.ImportXnamespace)].Value;
            LocalFolderpath = Itm.Fields[ID.Parse(Feed.Config.LocalFolderPath)].Value;
            FullLocalPath = LocalFolderpath + FileName;
        }
    }
}