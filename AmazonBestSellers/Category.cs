﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AmazonBestSellers
{
    public class Category
    {
        public string Name { get; set; }
        public string URL { get; set; }
        public List<Book> Books { get; set; }

        public Category(string name, string url)
        {
            Name = name;
            URL = url;
            Books = new List<Book>();
        }

        public async Task<List<Category>> RetrieveCategoryData(int qAboveFold, int qPage)
        {
            List<Category> subCategories = new List<Category>();

            string url = string.Format("{0}?_encoding=UTF8&pg={1}&ajax=1&isAboveTheFold={2}", URL, qPage, qAboveFold);

            string html = await DownloadHtmlPage(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

            doc.LoadHtml(html);
            var root = doc.DocumentNode;
            var titleNodes = root.Descendants().Where(n => n.GetAttributeValue("class", "").Equals("zg_title"));

            int rank = ((qPage - 1) * 20) + 1;

            if(qAboveFold == 0)
            {
                rank += 3; // there are only 3 items on the first ajax page, hopefully that is true for every category
                // consider using the rank number field on the html page
            }

            foreach (HtmlNode node in titleNodes)
            {
                string link = node.FirstChild.GetAttributeValue("href", "").Trim();
                string ISBN = link.Split(new string[] { "/dp/" }, StringSplitOptions.None)[1].Split(new string[] { "/" }, StringSplitOptions.None)[0];
                string title = node.FirstChild.InnerText;
                Book book = new Book(rank, title, ISBN, link);

                Books.Add(book);

                rank++;
            }

            return subCategories;
        }
        public async Task<List<Category>> RetrieveCategoryData(int qPage)
        {
            List<Category> subCategories = new List<Category>();

            string url = string.Format("{0}?pg={1}", URL, qPage);

            string html = await DownloadHtmlPage(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

            doc.LoadHtml(html);
            var root = doc.DocumentNode;
            var titleNodes = root.Descendants().Where(n => n.GetAttributeValue("class", "").Equals("zg_title"));

            int rank = 1;

            foreach (HtmlNode node in titleNodes)
            {
                string link = node.FirstChild.GetAttributeValue("href", "").Trim();
                string ISBN = link.Split(new string[] { "/dp/" }, StringSplitOptions.None)[1].Split(new string[] { "/" }, StringSplitOptions.None)[0];
                string title = node.FirstChild.InnerText;
                Book book = new Book(rank, title, ISBN, link);

                Books.Add(book);

                rank++;
            }

            HtmlNode categoryElement = doc.GetElementbyId("zg_browseRoot");

            HtmlNode lastUlElement = categoryElement.Descendants().Last(n => n.OriginalName == "ul");

            bool hasSubCategories = ! lastUlElement.Descendants().Any(n => n.GetAttributeValue("class", "").Equals("zg_selected"));

            if(hasSubCategories)
            {
                IEnumerable<HtmlNode> aElements = lastUlElement.Descendants().Where(n => n.OriginalName == "a");

                foreach(HtmlNode aElement in aElements)
                {
                    string link = aElement.GetAttributeValue("href", "").Trim();
                    string name = string.Format("{0} > {1}", Name, aElement.InnerText);
                    Category category = new Category(name, link);
                    subCategories.Add(category);
                }
            }

            return subCategories;
        }
        private async Task<string> DownloadHtmlPage(string url)
        {
            string text = null;
            using (GZipWebClient gZipWebClient = new GZipWebClient())
            {
                text = await gZipWebClient.DownloadStringTaskAsync(url);
            }
            return text;
        }
    }
}