using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NicoPlayerHohoema.Mntone.Nico2.Ranking
{
	/* 
	 Licensed under the Apache License, Version 2.0

	 http://www.apache.org/licenses/LICENSE-2.0
	 */
	[XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
	public class Link
	{
		[XmlAttribute(AttributeName = "rel")]
		public string Rel { get; set; }
		[XmlAttribute(AttributeName = "type")]
		public string Type { get; set; }
		[XmlAttribute(AttributeName = "href")]
		public string Href { get; set; }
	}

	[XmlRoot(ElementName = "guid")]
	public class Guid
	{
		[XmlAttribute(AttributeName = "isPermaLink")]
		public string IsPermaLink { get; set; }
		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(ElementName = "item")]
	public class NiconicoVideoRssItem
	{
		[XmlElement(ElementName = "title")]
		public string Title { get; set; }
		[XmlElement(ElementName = "link")]
		public string VideoUrl { get; set; }
		[XmlElement(ElementName = "guid")]
		public Guid Guid { get; set; }
		[XmlElement(ElementName = "pubDate")]
		public string PubDate { get; set; }
		[XmlElement(ElementName = "description")]
		public string Description { get; set; }
	}

	[XmlRoot(ElementName = "channel")]
	public class Channel
	{
		[XmlElement(ElementName = "title")]
		public string Title { get; set; }
		[XmlElement(ElementName = "link")]
		public List<string> Link { get; set; }
		[XmlElement(ElementName = "description")]
		public string Description { get; set; }
		[XmlElement(ElementName = "pubDate")]
		public string PubDate { get; set; }
		[XmlElement(ElementName = "lastBuildDate")]
		public string LastBuildDate { get; set; }
		[XmlElement(ElementName = "generator")]
		public string Generator { get; set; }
		[XmlElement(ElementName = "language")]
		public string Language { get; set; }
		[XmlElement(ElementName = "copyright")]
		public string Copyright { get; set; }
		[XmlElement(ElementName = "docs")]
		public string Docs { get; set; }
		[XmlElement(ElementName = "item")]
		public List<NiconicoVideoRssItem> Items { get; set; }
	}

	[XmlRoot(ElementName = "rss")]
	public class NiconicoRankingRss
	{
		[XmlElement(ElementName = "channel")]
		public Channel Channel { get; set; }
		[XmlAttribute(AttributeName = "version")]
		public string Version { get; set; }
		[XmlAttribute(AttributeName = "atom", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Atom { get; set; }
	}
}
