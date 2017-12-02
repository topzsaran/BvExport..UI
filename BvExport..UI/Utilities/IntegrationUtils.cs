using BvExport.Settings;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BvExport.Utilities
{
    public enum StorageLocation
    {
        Ftp,
        Sftp,
        Local
    }

    public class IntegrationUtils
    {
        private readonly FeedReference _feed = new FeedReference();
        public readonly ILog _log = LoggerFactory.GetLogger("Sitecore.Diagnostics.Dog");
        public IntegrationUtils()
        {
            _sitecoreService = new SitecoreService("web");
        }

        public MemoryStream CompressXDocumentToGzip(XDocument feed, string compression)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(feed.ToString())) { Position = 0 };
            var newStream = new MemoryStream();

            if (compression != "1")
            {
                _log.Info("Export -- This document wont be compressed, there is a tickbox on the settings item to specify");
                stream.CopyTo(newStream);
            }
            else
            {
                var compressedStream = new GZipStream(newStream, CompressionMode.Compress);
                stream.CopyTo(compressedStream);

                compressedStream.Flush();
                compressedStream.Close();
            }

            stream.Flush();
            stream.Close();

            return newStream;
        }

        public void SaveDocument(FeedSettings account, MemoryStream xmlfile, StorageLocation storage)
        {
            var result = false;
            if (storage == StorageLocation.Ftp)
            {
                _log.Info("Export -- XML Document -- Saving -- To FTP path -- " + string.Format("{0}{1}{2}", account.Url, account.FolderPath, account.FileName));
                result = PushToFtp(account, xmlfile.ToArray());
            }
            else if (storage == StorageLocation.Sftp)
            {
                _log.Info("Export -- XML Document -- Saving -- To SFTP path -- " + string.Format("{0}{1}{2}", account.Url, account.FolderPath, account.FileName));
                result = PushToSftp(account, xmlfile.ToArray());
            }

            if (!result)
            {
                _log.Info("Export -- XML Document -- Saving -- To Local path -- " + string.Format("{0}", account.FullLocalPath));
                SaveLocally(account, xmlfile.ToArray());
            }
        }

        private void SaveLocally(FeedSettings account, byte[] xmlfile)
        {
            using (var fileStream = new FileStream(account.FullLocalPath, FileMode.Create, FileAccess.Write))
            {
                using (var memoryStream = new MemoryStream(xmlfile, 0, xmlfile.Length))
                {
                    memoryStream.CopyTo(fileStream);
                    memoryStream.Flush();
                    memoryStream.Close();
                    _log.Info("Export -- XML Document -- Saving -- To Local path complete");
                }
            }
        }

        private bool PushToFtp(FeedSettings account, byte[] xmlfile)
        {
            try
            {
                _log.Info("Export -- XML Document -- Saving -- FTP -- Connecting");
                var request = (FtpWebRequest)WebRequest.Create(account.Url + account.FolderPath + account.FileName);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(account.UserName, account.Password);

                using (var ftpStream = request.GetRequestStream())
                {
                    _log.Info("Export -- XML Document -- Saving -- FTP -- Uploading");
                    ftpStream.Write(xmlfile, 0, xmlfile.Length);
                    ftpStream.Close();
                    ftpStream.Flush();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Export -- XML Document -- Saving -- FTP -- Connection Failed with exception message of -- " + ex.Message);
                return false;
            }
        }

        private bool PushToSftp(FeedSettings account, byte[] xmlfile)
        {
            try
            {
                var authMethod = new PasswordAuthenticationMethod(account.UserName, account.Password);

                var connectionInfo = new ConnectionInfo(account.Url, 22, account.UserName, authMethod);
                using (var bazaarVoiceSftp = new SftpClient(connectionInfo))
                {
                    using (var stream = new MemoryStream(xmlfile, 0, xmlfile.Length))
                    {
                        _log.Info("Export -- XML Document -- Saving -- SFTP -- Connecting");
                        bazaarVoiceSftp.Connect();
                        _log.Info("Export -- XML Document -- Saving -- SFTP -- Uploading");
                        bazaarVoiceSftp.UploadFile(stream, account.FolderPath + account.FileName);
                        bazaarVoiceSftp.Disconnect();
                        return true;
                    }
                }
            }

            catch (Exception ex)
            {
                _log.Info("Export -- XML Document -- Saving -- SFTP -- Connection Failed with exception message of -- " + ex.Message);
                return false;
            }

        }

        public XDocument GetExternalFeedDocument(ImportSettings account)
        {
            try
            {
                var stream = new MemoryStream();
                var authMethod = new PasswordAuthenticationMethod(account.Username, account.Password);

                var connectionInfo = new ConnectionInfo(account.Url, 22, account.Username, authMethod);

                using (var bazaarVoiceSftp = new SftpClient(connectionInfo))
                {
                    _log.Debug("Import -- External Feed -- Connecting");
                    bazaarVoiceSftp.Connect();
                    _log.Info("Import -- External Feed -- Downloading");
                    bazaarVoiceSftp.DownloadFile(account.FolderPath + account.FileName, stream);
                    _log.Debug("Import -- External Feed -- Download Complete");
                    bazaarVoiceSftp.Disconnect();

                    stream.Position = 0;

                    return DeserializeFromStream(stream, account.XNamespace);
                }
            }
            catch (Exception ex)
            {
                _log.Error("Import -- External Feed -- Connection Failed with exception message of -- " + ex.Message);
                return new XDocument();
            }
        }

        public XDocument DeserializeFromStream(MemoryStream stream, XNamespace ns)
        {
            _log.Debug("Import -- External Feed -- Deserialize the external feed to an XDocument");
            stream.Position = 0;
            var document = XDocument.Load(stream);

            return document;
        }
        

        private static string RoundDown(string number)
        {
            var rounded = "0";

            decimal output;

            if (decimal.TryParse(number, out output))
            {
                rounded = Math.Round(output, 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return rounded;
        }

        public T DeserializeFromXDoc<T>(XDocument source)
        {
            if (source == null)
                return default(T);

            using (var reader = source.CreateReader())
            {
                try
                {
                    var a = new XmlSerializer(typeof(T));
                    var b = a.Deserialize(reader);
                    var c = (T)b;

                    return c;
                }
                catch (Exception ex)
                {
                    _log.Debug(ex.Message);
                    throw;
                }
            }
        }

        //private void BuildStats(IEnumerable<AverageRatingValue> stats, XNamespace ns, string id)
        //{

        //    foreach (var stat in stats)
        //    {
        //       // var prodId = MatchProdct(id);
        //        if (prodId.IsNullOrEmpty())
        //        { continue; }

        //        var ratingMark = stat.AverageRating;
        //        if (ratingMark.IsNullOrEmpty())
        //        { continue; }

        //        var ratingType = BuildRatingType(stat.RatingDimension.Label);
        //        if (ratingType == null)
        //        { continue; }

        //        var rating = new Rating
        //        {
        //            Mark = (int)Convert.ToDecimal(ratingMark),
        //            Product = Guid.Parse(prodId),
        //            RatingType = ratingType
        //        };

        //        Ratings.Add(rating);
        //    }
        //}

        //private string MatchProdct(string id)
        //{
        //    var productId = id.IsNullOrEmpty() ? "" : id;
        //    //var product = ProductItems.FirstOrDefault(x => x.Ean == productId);

        //    //return product != null ? product.ItemId.ToString() : "";
        //}

        private RatingType BuildRatingType(string ratingValue)
        {
            var ratingValueField = ratingValue.IsNullOrEmpty() ? "" : ratingValue;
            var rating = RatingItems.FirstOrDefault(x => x.Name.Equals(ratingValueField, StringComparison.CurrentCultureIgnoreCase));

            var ratingType = rating != null ? rating.GetItem().GlassCast<RatingType>() : null;
            //_sitecoreService.Map(rating);

            return ratingType;
        }

        /// <summary>
        /// Deletes the old ratings.
        /// </summary>
        public void DeleteOldRatings()
        {
            using (new DatabaseSwitcher(Sitecore.Configuration.Factory.GetDatabase("master")))
            {
                var ratingFolder = Context.Database.GetItem(ID.Parse(_feed.Config.RatingFolderId));

                if (ratingFolder == null) return;

                ratingFolder.DeleteChildren();
            }
        }

        /// <summary>
        /// Creates the new ratings.
        /// </summary>
        /// <param name="ratings">The ratings.</param>
        public void CreateNewRatings(List<Rating> ratings)
        {
            using (new DatabaseSwitcher(Sitecore.Configuration.Factory.GetDatabase("master")))
            {
                using (new SecurityDisabler())
                {
                    var ratingFolder = Context.Database.GetItem(ID.Parse(_feed.Config.RatingFolderId));

                    if (ratingFolder != null)
                    {
                        TemplateItem ratingTemplate = Context.Database.GetItem(ID.Parse(_feed.Config.RatingTemplateId));

                        for (var i = 0; i < ratings.Count; i++)
                        {
                            try
                            {
                                var rating = ratingFolder.Add("Rating_" + i, ratingTemplate);
                                rating.Editing.BeginEdit();

                                string markField = _feed.Config.Mark;
                                string ratingType = _feed.Config.RatingType;
                                string product = _feed.Config.Product;

                                //Mark
                                rating.Fields[ID.Parse(markField)].Value = ratings[i].Mark.ToString(); ;
                                //Rating Type
                                rating.Fields[ID.Parse(ratingType)].Value = ratings[i].RatingType.RatingTypeId.ToString();
                                //Product
                                var productId = ratings[i].Product.ToString().ToUpper();

                                if (!productId.Contains("{"))
                                {
                                    productId = string.Format("{{{0}}}", productId);
                                }

                                rating.Fields[ID.Parse(product)].Value = productId;

                                rating.Editing.AcceptChanges();
                                rating.Editing.EndEdit();
                            }
                            catch (Exception ex)
                            {
                                _log.Error("Import -- Error importing reviews -- " + ex.Message);
                                throw;
                            }

                            if (i == ratings.Count / 4)
                            {
                                _log.Info("Import -- Importing status -- 25%");
                            }

                            if (i == ratings.Count / 2)
                            {
                                _log.Info("Import -- Importing status -- 50%");
                            }

                            if (i == Convert.ToInt32(ratings.Count * 0.75))
                            {
                                _log.Info("Import -- Importing status -- 75%");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates new review statistics
        /// </summary>
        /// <param name="reviewStatistics"></param>
        //public void CreateNewReviewStatistics(List<ReviewStatistics> reviewStatistics)
        //{
        //    using (new DatabaseSwitcher(Sitecore.Configuration.Factory.GetDatabase("master")))
        //    {
        //        using (new SecurityDisabler())
        //        {
        //            var reviewStatisticsFolder = Context.Database.GetItem(ID.Parse(_feed.Config.ReviewStatisticsFolderId));

        //            if (reviewStatisticsFolder != null)
        //            {
        //                //TODO Check if exists
        //                TemplateItem reviewStatisticsTemplate = Context.Database.GetItem(ID.Parse(_feed.Config.ReviewStatisticsTemplateId));

        //                //var CurrentReviewStatistics = GetItems<LOREAL.UK.Core.Models.Reviews.ReviewStatistics>(ID.Parse(_feed.Config.ReviewStatisticsTemplateId));

        //                for (var i = 0; i < reviewStatistics.Count; i++)
        //                {
        //                    try
        //                    {
        //                        //var match = CurrentReviewStatistics.FirstOrDefault(x => x.Product == reviewStatistics[i].Product);

        //                        string name = reviewStatistics[i].Name.ToString().ToLower();

        //                        var reviewStatistic = reviewStatisticsFolder.Add(name, reviewStatisticsTemplate);

        //                        reviewStatistic.Editing.BeginEdit();

        //                        string reviewsTotal = _feed.Config.ReviewTotalField;
        //                        string ratingsAverage = _feed.Config.RatingsAverageField;
        //                        string product = _feed.Config.StatisticsProductField;

        //                        //Ratings Average
        //                        reviewStatistic.Fields[ID.Parse(ratingsAverage)].Value = reviewStatistics[i].RatingsAverage.ToString();
        //                        //Reviews Total
        //                        reviewStatistic.Fields[ID.Parse(reviewsTotal)].Value = reviewStatistics[i].ReviewsTotal.ToString();
        //                        //Product
        //                        var productId = reviewStatistics[i].Product.ToString().ToUpper();

        //                        if (!productId.Contains("{"))
        //                        {
        //                            productId = string.Format("{{{0}}}", productId);
        //                        }

        //                        reviewStatistic.Fields[ID.Parse(product)].Value = productId;

        //                        reviewStatistic.Editing.AcceptChanges();
        //                        reviewStatistic.Editing.EndEdit();
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _log.Error("Import -- Error importing review statistics -- " + ex.Message);
        //                        throw;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //public static IQueryable<TK> GetSolrResult<TK>(Expression<Func<TK, bool>> predicate) where TK : class
        //{
        //    var index = ContentSearchManager.GetIndex("sitecore_web_index");
        //    using (var context = index.CreateSearchContext())
        //    {
        //        return context.GetQueryable<SearchResultItem>().Where(predicate).ToList();
        //    }

        //}

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        //public List<T> GetItems<T>(ID templateId, Expression<Func<T, bool>> expression = null) where T : SearchResultItem
        //{
        //    var predicate = PredicateBuilder.True<T>()
        //        .And(p => p.TemplateId == templateId)
        //        .And(p => !p.Name.Equals("__Standard Values"));
        //    //.And(p => p.Language == Context.Language.Name);

        //    //var results = GetSolrResult(predicate).ToList();
        //    var results = null;
        //    return results;
        //}

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public List<BrandSearchItem> GetBrands(ID templateId, Expression<Func<Brand, bool>> expression = null)
        {
            List<Library.SC.Lucene.BrandSearchItem> ProductResultItems = new List<Lucene.BrandSearchItem>();
            //var predicate = PredicateBuilder.True<Brand>()
            //    .And(p => p.TemplateId == templateId)
            //    .And(p => !p.Name.Equals("__Standard Values"));
            ////.And(p => p.Language == Context.Language.Name);
            //var results = GetSolrResult(predicate).ToList();
            //return results;
            //List<BrandSearchItem> list = new List<BrandSearchItem>();
            //var language = Sitecore.Context.Language.CultureInfo.TwoLetterISOLanguageName.ToString();
            //var index = ContentSearchManager.GetIndex("sitecore_web_index");
            //using (var context = index.CreateSearchContext())
            //{
            //    var predicate = PredicateBuilder.True<SearchResultItem>();

            //    predicate = predicate.And((p => p.TemplateId == templateId));
            //    var results = context.GetQueryable<BrandSearchItem>().Where(predicate).ToList();

            //}
            //return list;

            //List<Library.SC.Lucene.BrandSearchItem> ProductResultItems = new List<Lucene.BrandSearchItem>();
            ISearchIndex indexbrand = ContentSearchManager.GetIndex("website_custom_brand");
            using (IProviderSearchContext context = indexbrand.CreateSearchContext())
            {
                System.Linq.Expressions.Expression<Func<Library.SC.Lucene.BrandSearchItem, bool>> predicate = PredicateBuilder.True<Library.SC.Lucene.BrandSearchItem>();
                predicate = predicate.And(p => p._language.Equals(Garnier.Library.SC.Utility.Extensions.currentContextLanguage()));
                predicate = predicate.And(p => p.TemplateId == Lookups.TemplateBrandID);
                ProductResultItems = context.GetQueryable<Library.SC.Lucene.BrandSearchItem>()
                                       .Where(predicate)
                                           .ToList();
            }
            if (ProductResultItems != null && ProductResultItems.Count > 0)
            {
                //List<Sitecore.Data.Items.Item> ResultItems = new List<Sitecore.Data.Items.Item>();

                //foreach (Library.SC.Lucene.BrandSearchItem ProductResultItem in ProductResultItems)
                //{
                //    Sitecore.Data.Items.Item thisItem = ProductResultItem.GetContextItem(ResultItems);
                //    if (thisItem != null && thisItem.itemIsValid() && (thisItem.itemHasValidChildren()))
                //    {
                //        ResultItems.Add(thisItem);
                //    }
                //}
                //return ResultItems;
                return ProductResultItems;
            }
            else
            {
                return new List<Library.SC.Lucene.BrandSearchItem>();
            }
        }
        public List<ProductSearchItem> GetProducts(ID templateId)
        {
            //var predicate = PredicateBuilder.True<Brand>()
            //    .And(p => p.TemplateId == templateId)
            //    .And(p => !p.Name.Equals("__Standard Values"));
            ////.And(p => p.Language == Context.Language.Name);
            //var results = GetSolrResult(predicate).ToList();
            //return results;
            List<ProductSearchItem> ProductResultItems = new List<ProductSearchItem>();
            //var language = Sitecore.Context.Language.CultureInfo.TwoLetterISOLanguageName.ToString();
            //var index = ContentSearchManager.GetIndex("sitecore_web_index");
            //using (var context = index.CreateSearchContext())
            //{
            //    var predicate = PredicateBuilder.True<SearchResultItem>();

            //    predicate = predicate.And((p => p.TemplateId == templateId));
            //   // predicate = predicate.And((p => p.Language == language));


            //    var results = context.GetQueryable<SearchResultItem>().Where(predicate).ToList();

            //}
            ISearchIndex indexbrand = ContentSearchManager.GetIndex("website_custom_product");
            using (IProviderSearchContext context = indexbrand.CreateSearchContext())
            {
                System.Linq.Expressions.Expression<Func<Library.SC.Lucene.ProductSearchItem, bool>> predicate = PredicateBuilder.True<Library.SC.Lucene.ProductSearchItem>();
                predicate = predicate.And(p => p._language.Equals(Garnier.Library.SC.Utility.Extensions.currentContextLanguage()));
                predicate = predicate.And(p => p.TemplateId == Lookups.TemplateProductID);
                ProductResultItems = context.GetQueryable<Library.SC.Lucene.ProductSearchItem>()
                                       .Where(predicate)
                                           .ToList();
            }
            if (ProductResultItems != null && ProductResultItems.Count > 0)
            {
                //List<Sitecore.Data.Items.Item> ResultItems = new List<Sitecore.Data.Items.Item>();

                //foreach (Library.SC.Lucene.BrandSearchItem ProductResultItem in ProductResultItems)
                //{
                //    Sitecore.Data.Items.Item thisItem = ProductResultItem.GetContextItem(ResultItems);
                //    if (thisItem != null && thisItem.itemIsValid() && (thisItem.itemHasValidChildren()))
                //    {
                //        ResultItems.Add(thisItem);
                //    }
                //}
                //return ResultItems;
                return ProductResultItems;
            }
            else
            {
                return new List<Library.SC.Lucene.ProductSearchItem>();
            }

            //return list;
        }

        /// <summary>
        /// Gets the department pages.
        /// </summary>
        /// <param name="templateId">The template identifier.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        //public List<DepartmentPage> GetDepartmentPages(ID templateId, Expression<Func<DepartmentPage, bool>> expression = null)
        //{
        //    var predicate = PredicateBuilder.True<DepartmentPage>()
        //        .And(p => p.TemplateId == templateId)
        //        .And(p => !p.Name.Equals("__Standard Values"));
        //    //.And(p => p.Language == Context.Language.Name);
        //    var results = GetSolrResult(predicate).ToList();
        //    return results;
        //}

    }
}