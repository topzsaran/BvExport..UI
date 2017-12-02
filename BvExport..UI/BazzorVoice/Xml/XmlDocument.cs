using BvExport.Settings;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace BvExport.BazzorVoice.Xml
{
    public class XmlDocument
    {
        private List<XElement> Brands;
        private List<XElement> Products;
        private List<XElement> Categories;
        private XNamespace Bv;
        private readonly FeedSettings _item;
        private readonly ILog _log = Loggerfactory.GetLogger("Sitecore.Diagnostics.Dog");
        private MediaItem MediaItem;
        private string ImageUrl;
        private readonly FeedReference _feed = new FeedReference();

        public XmlDocument(ExportSettings item)
        {
            _item = item;
            Bv = item.XNamespace;
            Brands = new List<XElement>();
            Products = new List<XElement>();
            Categories = new List<XElement>();

        }

        public XDocument BuildXml()
        {
            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));

            XAttribute[] feedAttributes = {
                new XAttribute ("name","SamplePOC"),
                new XAttribute("extractDate",DateTime.UtcNow),
                new XAttribute("incremental","false")
            };

            //build all nodes
            
            _log.Info("Export -- Building Brands");
            BuildBrands();
            _log.Info("Export -- Building Categories");
            BuildCategories();
            _log.Info("Export -- Building Products");
            BuildProducts();
            
            var brandNode = new XElement(Bv + "Brands", Brands);
            var categoriesNode = new XElement(Bv + "Categories", Categories);
            var productsNode = new XElement(Bv + "Products", Products);
            
            var feedNode = new XElement(Bv + "Feed", feedAttributes);
            feedNode.Add(brandNode);
            feedNode.Add(categoriesNode);
            feedNode.Add(productsNode);
            
            doc.Add(feedNode);
            
            return doc;
        }

        private void BuildBrands()
        {
            string templateId = _feed.Config.BrandsTemplateId;
            using (new Sitecore.Globalization.LanguageSwitcher(lan))
            {
                Sitecore.Data.Database database = Sitecore.Data.Database.GetDatabase("web");
                Sitecore.Data.Items.Item[] brands =
database.SelectItems("fast:/sitecore/content/home//*[@@templateid='{F0649FE8-18C5-445B-8FFC-BC654E426093}']");
                foreach (var brand in brands)
                {
                    

                    Brands.Add(
                        new XElement(Bv + "Brand",
                            new XElement(Bv + "ExternalId", brand.ID.ToShortID()),
                            new XElement(Bv + "Name", brand.Fields["__Display Name"].Value)
                            ));
                }
            }
        }
 
        private void BuildCategories()
        {
            
                Sitecore.Data.Database database = Sitecore.Data.Database.GetDatabase("web");

                Sitecore.Data.Items.Item[] Categoriesitems =
database.SelectItems("fast:/sitecore/content/home//*[@@templateid='{5957125F-3286-421D-A855-A00BF88357F1}']");


                foreach (var Category in Categoriesitems)
                {

                    var item = Context.Database.GetItem(Category.ID);
                    var options = Sitecore.Links.LinkManager.GetDefaultUrlOptions();
                    options.LanguageEmbedding = Sitecore.Links.LanguageEmbedding.Never;
                    var categoryPageUrl = GetFullUrl(LinkManager.GetItemUrl(item, options));
                    Categories.Add(
                        new XElement(Bv + "Category",
                            new XElement(Bv + "ExternalId", Category.ID.ToShortID()),
                            new XElement(Bv + "Name", Category.Fields["__Display Name"].Value),
                            new XElement(Bv + "CategoryPageUrl", categoryPageUrl),
                            new XElement(Bv + "ImageUrl")
                            ));
                }
            
        }

        private void BuildProducts()
        {
            

                Sitecore.Data.Database database = Sitecore.Data.Database.GetDatabase("web");
                Sitecore.Data.Items.Item[] Productsitems =
database.SelectItems("fast:/sitecore/content/home//*[@@templateid='{2EE70A6B-AF13-498E-8419-BDA52C8C05DD}']");
                foreach (var Product in Productsitems)
                {

                    var item = Context.Database.GetItem(Product.ID);
                    var attributes = new List<XElement>();
                    if (!string.IsNullOrEmpty(Product.Fields["EanBarcode"].Value))
                    {
                        XAttribute[] familyAttributes = { new XAttribute("id", "BV_FE_FAMILY") };
                        XAttribute[] expandAttributes = { new XAttribute("id", "BV_FE_EXPAND") };
                        attributes.Add(new XElement(Bv + "Attribute", familyAttributes, new XElement(Bv + "Value", Product.Fields["EanBarcode"].Value)));
                        attributes.Add(new XElement(Bv + "Attribute", expandAttributes, new XElement(Bv + "Value", "BV_FE_FAMILY:" + Product.Fields["EanBarcode"].Value)));
                    }

                    var productdesc = RemoveHTMLTags(Product.Fields["ProductIntroduction"].Value);
                    var desc = !ExtensionMethods.IsNullOrEmpty(productdesc) ? new XCData(productdesc) : new XCData("");

                    var options = Sitecore.Links.LinkManager.GetDefaultUrlOptions();
                    options.LanguageEmbedding = Sitecore.Links.LanguageEmbedding.Never;
                    var productUrl = LinkManager.GetItemUrl(item, options);


                    var catExternalId = item.Parent.ParentID.ToShortID().ToString();
                    if (catExternalId.IsNullOrEmpty())
                    { continue; }

                    var brandExternalId = item.ParentID.ToShortID().ToString(); ;
                    string ImageUrl = string.Empty;
                    Sitecore.Data.Fields.ImageField imageField = item.Fields["ReferenceImage"];
                    if (imageField != null && imageField.MediaItem != null)
                    {
                        Sitecore.Data.Items.MediaItem image = new Sitecore.Data.Items.MediaItem(imageField.MediaItem);

                        ImageUrl = Sitecore.StringUtil.EnsurePrefix('/', Sitecore.Resources.Media.MediaManager.GetMediaUrl(image));
                    }
                    //Add a single product
                    var prod = new XElement(Bv + "Product");
                    prod.Add(new XElement(Bv + "ExternalId", Product.Fields["EanBarcode"].Value));
                    prod.Add(new XElement(Bv + "Name", new XCData(Product.Fields["__Display Name"].Value)));
                    prod.Add(new XElement(Bv + "Description", desc));
                    prod.Add(new XElement(Bv + "CategoryExternalId", catExternalId));
                    prod.Add(new XElement(Bv + "ProductPageUrl", GetFullUrl(productUrl)));
                    prod.Add(new XElement(Bv + "ImageUrl", GetFullUrl(ImageUrl)));
                    prod.Add(new XElement(Bv + "EANs", new XElement(Bv + "EAN", Product.Fields["EanBarcode"].Value)));
                    prod.Add(new XElement(Bv + "Attributes", attributes));
                    if (!string.IsNullOrEmpty(brandExternalId))
                    {
                        prod.Add(new XElement(Bv + "BrandExternalId", brandExternalId));
                    }
                    Products.Add(prod);


                
            }
        }

        public static string RemoveHTMLTags(string strValue)
        {
            if (!string.IsNullOrEmpty(strValue))
            {
                var _rgx = new Regex("<[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                string _title = _rgx.Replace(strValue, "").Trim();
                return _title;
            }

            return string.Empty;
        }
      

        #region FIXERS

        private static string GetExternalId(string externalId, string name)
        {
            return (externalId.Equals("$name") ? name.ToLower().Replace(" ", string.Empty) : externalId);
        }

        private static string GetFullUrl(string url)
        {
            return string.Concat("http://sampleDomain.com", url);
        }

        #endregion

    }
}