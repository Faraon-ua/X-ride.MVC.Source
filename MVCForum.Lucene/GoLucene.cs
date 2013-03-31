﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using MVCForum.Lucene.LuceneModel;
using Version = Lucene.Net.Util.Version;

namespace MVCForum.Lucene
{
    public static class GoLucene
    {
        // Constants - These are the name of the fields in the lucene model
        private const string LucId = "Id";
        private const string LucPostContent = "PostContent";
        private const string LucDateCreated = "DateCreated";
        private const string LucTopicName = "TopicName";
        private const string LucTopicId = "TopicId";
        //private const string LucTopicTags = "TopicTags";
        private const string LucTopicUrl = "TopicUrl";
        private const string LucUsername = "Username";
        private const string LucUserId = "UserId";


        private const string LuceneDirectoryName = "lucene_index";

        // properties
        public static string _luceneDir = HttpContext.Current.Server.MapPath(string.Concat("~/App_Data", "/", LuceneDirectoryName));
        private static FSDirectory _directoryTemp;
        private static FSDirectory _directory
        {
            get
            {
                if (_directoryTemp == null) _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                if (IndexWriter.IsLocked(_directoryTemp)) IndexWriter.Unlock(_directoryTemp);
                var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                if (File.Exists(lockFilePath)) File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }


        // search methods
        public static IEnumerable<LuceneSearchModel> GetAllIndexRecords()
        {
            // validate search index
            if (!System.IO.Directory.EnumerateFiles(_luceneDir).Any()) return new List<LuceneSearchModel>();

            // set up lucene searcher
            var searcher = new IndexSearcher(_directory, false);
            var reader = IndexReader.Open(_directory, false);
            var docs = new List<Document>();
            var term = reader.TermDocs();
            while (term.Next()) docs.Add(searcher.Doc(term.Doc));
            reader.Dispose();
            searcher.Dispose();
            return _mapLuceneToDataList(docs);
        }

        public static IEnumerable<LuceneSearchModel> Search(string input, string fieldName = "")
        {
            if (string.IsNullOrEmpty(input)) return new List<LuceneSearchModel>();

            var terms = input.Trim().Replace("-", " ").Split(' ')
                .Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            input = string.Join(" ", terms);

            return _search(input, fieldName);
        }

        public static IEnumerable<LuceneSearchModel> SearchDefault(string input, string fieldName = "")
        {
            return string.IsNullOrEmpty(input) ? new List<LuceneSearchModel>() : _search(input, fieldName);
        }


        // main search method
        private static IEnumerable<LuceneSearchModel> _search(string searchQuery, string searchField = "")
        {
            // validation
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<LuceneSearchModel>();

            // set up lucene searcher
            using (var searcher = new IndexSearcher(_directory, false))
            {
                const int hitsLimit = 1000;
                var analyzer = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30);

                // search by single field
                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(global::Lucene.Net.Util.Version.LUCENE_30, searchField, analyzer);
                    var query = parseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, hitsLimit).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);
                    analyzer.Close();
                    searcher.Dispose();
                    return results;
                }
                // search by multiple fields (ordered by RELEVANCE)
                else
                {
                    var parser = new MultiFieldQueryParser(global::Lucene.Net.Util.Version.LUCENE_30, new[] { LucId, LucTopicName, LucPostContent }, analyzer);
                    var query = parseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, null, hitsLimit, Sort.INDEXORDER).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);
                    analyzer.Close();
                    searcher.Dispose();
                    return results;
                }
            }
        }
        private static Query parseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }


        // map Lucene search index to data
        private static IEnumerable<LuceneSearchModel> _mapLuceneToDataList(IEnumerable<Document> hits)
        {
            return hits.Select(_mapLuceneDocumentToData).ToList();
        }

        private static IEnumerable<LuceneSearchModel> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => _mapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList();
        }

        private static LuceneSearchModel _mapLuceneDocumentToData(Document doc)
        {
            return new LuceneSearchModel
            {
                Id = Guid.Parse(doc.Get(LucId)),
                TopicName = doc.Get(LucTopicName),
                PostContent = doc.Get(LucPostContent),
                DateCreated = DateTools.StringToDate(doc.Get(LucDateCreated)),
                TopicId = Guid.Parse(doc.Get(LucTopicId)),
                UserId = Guid.Parse(doc.Get(LucUserId)),
                Username = doc.Get(LucUsername),
                TopicUrl = doc.Get(LucTopicUrl)
            };
        }

        // add/update/clear search index data 
        public static void AddUpdateLuceneIndex(LuceneSearchModel sampleData)
        {
            AddUpdateLuceneIndex(new List<LuceneSearchModel> { sampleData });
        }

        public static void AddUpdateLuceneIndex(IEnumerable<LuceneSearchModel> sampleDatas)
        {
            // init lucene
            var analyzer = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                // add data to lucene search index (replaces older entries if any)
                foreach (var sampleData in sampleDatas) _addToLuceneIndex(sampleData, writer);

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
        }
        public static void ClearLuceneIndexRecord(Guid record_id)
        {
            // init lucene
            var analyzer = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                // remove older index entry
                var searchQuery = new TermQuery(new Term(LucId, record_id.ToString()));
                writer.DeleteDocuments(searchQuery);

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
        }
        public static bool ClearLuceneIndex()
        {
            try
            {
                var analyzer = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30);
                using (var writer = new IndexWriter(_directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // remove older index entries
                    writer.DeleteAll();

                    // close handles
                    analyzer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public static void Optimize()
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                analyzer.Close();
                writer.Optimize();
                writer.Dispose();
            }
        }
        private static void _addToLuceneIndex(LuceneSearchModel searchModel, IndexWriter writer)
        {
            // remove older index entry
            var searchQuery = new TermQuery(new Term(LucId, searchModel.Id.ToString()));
            writer.DeleteDocuments(searchQuery);

            // add new index entry
            var doc = new Document();

            // add lucene fields mapped to db fields
            // Posts
            doc.Add(new Field(LucId, searchModel.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field(LucPostContent, searchModel.PostContent, Field.Store.YES, Field.Index.ANALYZED));

            //Topics
            if (!string.IsNullOrEmpty(searchModel.TopicName))
            {
                doc.Add(new Field(LucTopicName, searchModel.TopicName, Field.Store.YES, Field.Index.ANALYZED));
            }
            doc.Add(new Field(LucTopicUrl, searchModel.TopicUrl, Field.Store.YES, Field.Index.NOT_ANALYZED));            

            // Chnage the date so we can query in date order
            var dateValue = DateTools.DateToString(searchModel.DateCreated, DateTools.Resolution.MILLISECOND);
            doc.Add(new Field(LucDateCreated, dateValue, Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field(LucTopicId, searchModel.TopicId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));

            //User
            doc.Add(new Field(LucUsername, searchModel.Username, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field(LucUserId, searchModel.UserId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));

            // add entry to index
            writer.AddDocument(doc);
        }

    }
}

// Example date order by
//var dateValue = DateTools.DateToString(DateTime.Now, DateTools.Resolution.MILLISECOND);
//var filter = FieldCacheRangeFilter.NewStringRange("date", 
//                 lowerVal: dateValue, includeLower: true, 
//                 upperVal: null, includeUpper: false);
//var topDocs = searcher.Search(query, filter, 10000);


// Fuzzy search
// http://scatteredcode.wordpress.com/2011/05/26/performing-a-fuzzy-search-with-multiple-terms-through-multiple-lucene-net-document-fields/
//public SearchResults Search(string searchString)
//{
//            // Setup the fields to search through
//            string[] searchfields = new string[] { "FirstName", "LastName" };

//            // Build our booleanquery that will be a combination of all the queries for each individual search term
//            var finalQuery = new BooleanQuery();
//            var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29, searchfields, CreateAnalyzer());

//            // Split the search string into separate search terms by word
//            string[] terms = searchString.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
//            foreach (string term in terms)
//                finalQuery.Add(parser.Parse(term.Replace("~", "") + "~"), BooleanClause.Occur.MUST);

//            // Perform the search
//            var directory = FSDirectory.Open(new DirectoryInfo(LuceneIndexBaseDirectory));
//            var searcher = new IndexSearcher(directory, true);
//            var hits = searcher.Search(finalQuery, MAX_RESULTS);
//}