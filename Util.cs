using System;
using System.Web;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using SnowmeetOfficialAccount.Models;
using SnowmeetOfficialAccount.Controllers;
namespace SnowmeetOfficialAccount
{
    public class Util
    {
        public static bool isDev = false;

        public static string workingPath = $"{Environment.CurrentDirectory}";

        public static string GetLongTimeStamp(DateTime currentDateTime)
        {
            TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }
        public static string UrlEncode(string urlStr)
        {
            return HttpUtility.UrlEncode(urlStr.Trim().Replace(" ", "+").Replace("'", "\""));
        }

        public static string UrlDecode(string urlStr)
        {
            if (urlStr == null || urlStr.Trim().Equals(""))
            {
                return "";
            }
            try
            {
                return HttpUtility.UrlDecode(urlStr).Replace(" ", "+").Trim();
            }
            catch
            {
                return "";
            }

        }
        public static string GetWebContent(string url, string postData, string contentType)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = contentType;
            int len = System.Text.Encoding.UTF8.GetByteCount(postData);
            req.ContentLength = len;
            Stream sPost = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(sPost);
            sw.Write(postData);
            sw.Close();
            sPost.Close();
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string str = sr.ReadToEnd();
            sr.Close();
            s.Close();
            return str;
        }
        public static string GetWebContent(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                Stream s = res.GetResponseStream();
                StreamReader sr = new StreamReader(s);
                string str = sr.ReadToEnd();
                sr.Close();
                s.Close();
                res.Close();
                req.Abort();
                return str;
            }
            catch
            {
                return "";
            }
        }
        public static string GetWebContent(string url, string postData)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            Stream sPost = req.GetRequestStream();
            StreamWriter sw = new StreamWriter(sPost);
            sw.Write(postData);
            sw.Close();
            sPost.Close();
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string str = sr.ReadToEnd();
            sr.Close();
            s.Close();
            return str;
        }
        public static string CreateVerifyCode(int digit)
        {
            Random rnd = new Random();
            long max = (long)Math.Pow(10, digit) - 1;
            long num = rnd.NextInt64(0, max);
            return num.ToString().PadLeft(digit, '0');
        }
        public static string GetRandomCode(int digit)
        {
            string code = "";
            for (int i = 0; i < digit; i++)
            {
                code = code + (new Random()).Next(0, 10).ToString();
            }
            return code;
        }
        public static async Task<Staff?> GetStaffBySessionKey(AppDBContext db, string sessionKey, string sessionType = "wechat_mini_openid")
        {
            StaffController staffHelper = new StaffController(db);
            Staff staff = await staffHelper.GetStaffBySessionKey(sessionKey, sessionType);
            return staff;
        }
        public static CoreDataModLog CreateCoreDataModLog(string table, string filed, int key, object? prev, object? curr, int? memberId, int? staffId, string scene, long? traceId = null)
        {
            if (traceId == null)
            {
                traceId = (DateTime.Now - DateTime.Parse("1970-1-1")).Ticks;
            }
            return new CoreDataModLog()
            {
                id = 0,
                table_name = table,
                field_name = filed,
                key_value = key,
                prev_value = prev == null ? "null" : prev.ToString(),
                current_value = curr == null ? "null" : curr.ToString(),
                member_id = memberId,
                staff_id = staffId,
                scene = scene,
                trace_id = (long)traceId,
                create_date = DateTime.Now
            };
        }
    }
}
