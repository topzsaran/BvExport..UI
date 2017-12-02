using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Dynamic;
using System.Collections.Specialized;

namespace BvExport.Settings
{
    public class FeedReference
    {
        public dynamic Config;

        public FeedReference()
        {
            Config = new AppSettingsWrapper();
        }
    }

    public class AppSettingsWrapper : DynamicObject
    {
        private readonly NameValueCollection _items;

        public AppSettingsWrapper()
        {
            _items = ConfigurationManager.AppSettings;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _items[binder.Name];
            return result != null;
        }
    }
}