using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BvExport.Settings
{
    public class FeedSettings
    {
        public readonly ILog Log = LoggerFactory.GetLogger("Sitecore.Diagnostics.Dog");
        public FeedReference feed = new FeedReference();

        public Item Itm { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }

        public string LocalFolderpath { get; set; }

        public string FileName { get; set; }

        public string Url { get; set; }

        public string FolderPath { get; set; }

        public string FullLocalPath { get; set; }

        public Item GetSettingsItem(ID commandId)
        {
            //BVExport 
            if (commandId.Tostring() == feed.Config.BvExportCommandId)
            {
                Log.Debug("Getting Bazzor Voice Setting Item")
                 return Context.Database.GetItem(ID.Parse(feed.Config.BvExportSettingsItem));
            }
            return null;
        }
        }
    }
