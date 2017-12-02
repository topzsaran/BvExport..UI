using BvExport.BazzorVoice.Xml;
using BvExport.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using log4net;
using BvExport.Utilities;

namespace BvExport.BazzorVoice.Export
{
    public class BaExport
    {
        public ExportSettings SettingsItem { get; set; }
        public XDocument Doc { get; set; }
        public readonly ILog _log = LoggerFactory.GetLogger("Sitecore.Diagnostics.Dog");

        public BaExport(ID commandId)
        {

            _log.Info("--------");
            _log.Info("Bazzar Voice Export -- Started");

            //Build a common item that has all the information needed -- items, ftp account, compression options
            _log.Info("Export -- Settings -- Building");
            SettingsItem = new ExportSettings(commandId);

            //Build the XML file
            _log.Info("Export -- XML Document -- Building");
            var xml = new XmlDocument(SettingsItem);
            Doc = xml.BuildXml();

            //Compress XML file to GZIP compression level
            _log.Info("Export -- XML Document -- Compressing");
            var utils = new IntegrationUtils();
            var compressDoc = utils.CompressXDocumentToGzip(Doc, SettingsItem.Compression);

            //Push the XML file to either the local disk or the FTP/SFTP location
            _log.Info("Export -- XML Document -- Saving");
            utils.SaveDocument(SettingsItem, compressDoc, StorageLocation.Sftp);

            _log.Info("Bazzar Voice Export -- Finished");
            _log.Info("--------");
        }
    }
}