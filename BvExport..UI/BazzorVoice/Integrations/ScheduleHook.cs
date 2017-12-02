using BvExport.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BvExport.BazzorVoice.Export;

namespace BvExport.BazzorVoice.Integrations
{
    public class ScheduleHook : FeedReference
    {

        public void Hook(Item[] items, Sitecore.Tasks.CommandItem command, Sitecore.Tasks.ScheduleItem schedule)
        {
            var feed = new FeedReference();

            if (!IsCorrectTime(feed))
                return;

            using (new DatabaseSwitcher(Sitecore.Configuration.Factory.GetDatabase("web")))
            {
                /// Bazaar Voice Export Command Item
                if (command.ID.ToString() == feed.Config.BvExportCommandId)
                {
                    new BaExport(command.ID);
                }
            }
        }

        public bool IsCorrectTime(FeedReference feed)
        {
            return DateTime.UtcNow >= DateTime.Parse(feed.Config.EarliestTimeToRun) && DateTime.UtcNow <= DateTime.Parse(feed.Config.LatestTimeToRun)

        }
    }
}