﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices.JavaScript;
using System.Xml;
namespace SnowmeetOfficialAccount.Models
{
	[Table("oa_sent")]
	public class OASent
	{
        public struct NewContent
        {
            public string title { get; set; }
            public string description { get; set; }
            public string url { get; set; }
            public string picUrl { get; set; }
        }


		[Key]
		public int id { get; set; }

		public string FromUserName { get; set; }
		public string ToUserName { get; set; }
		public int is_service { get; set; } = 0;
		public int origin_message_id { get; set; } = 0;
		public string MsgType { get; set; } = "text";
		public string Content { get; set; } = "";
		public string Thumb { get; set; } = "";
		public string err_code { get; set; } = "";
		public string err_msg { get; set; } = "";

        [NotMapped]
        public NewContent[] newContentArray { get; set; } = new NewContent[0];


		public XmlDocument GetXmlDocument()
		{
			XmlDocument xmlD = new XmlDocument();
            xmlD.LoadXml("<xml></xml>");

            XmlNode n = xmlD.CreateNode(XmlNodeType.Element, "FromUserName", "");
            n.InnerXml = "<![CDATA[" + FromUserName.Trim() + "]]>";
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            n = xmlD.CreateNode(XmlNodeType.Element, "ToUserName", "");
            n.InnerXml = "<![CDATA[" + ToUserName.Trim() + "]]>";
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            n = xmlD.CreateNode(XmlNodeType.Element, "CreateTime", "");
            n.InnerText = GetTimeStamp().Trim();
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            n = xmlD.CreateNode(XmlNodeType.Element, "MsgType", "");
            n.InnerXml = "<![CDATA[" + MsgType.Trim() + "]]>";
            xmlD.SelectSingleNode("//xml").AppendChild(n);

            switch (MsgType)
            {
                case "text":
                    n = xmlD.CreateNode(XmlNodeType.Element, "Content", "");
                    n.InnerXml = "<![CDATA[" + Content.Trim() + "]]>";
                    xmlD.SelectSingleNode("//xml").AppendChild(n);
                    break;
                /*
                case "news":
                    n = xmlD.CreateNode(XmlNodeType.Element, "ArticleCount", "");
                    n.InnerXml = "<![CDATA[" + sqlDr["wxreplymsg_msgcount"].ToString().Trim() + "]]>";
                    xmlD.SelectSingleNode("//xml").AppendChild(n);
                    n = xmlD.CreateNode(XmlNodeType.Element, "Articles", "");
                    n.InnerXml = sqlDr["wxreplymsg_content"].ToString().Trim();
                    xmlD.SelectSingleNode("//xml").AppendChild(n);

                    break;
                case "image":
                    n = xmlD.CreateNode(XmlNodeType.Element, "Image", "");
                    XmlNode subN = xmlD.CreateNode(XmlNodeType.Element, "MediaId", "");
                    subN.InnerXml = "<![CDATA[" + sqlDr["wxreplymsg_content"].ToString().Trim() + "]]>";
                    n.AppendChild(subN);
                    xmlD.SelectSingleNode("//xml").AppendChild(n);
                    break;
                */
                default:

                    break;
            }

            return xmlD;
		}

        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
    }
}

