using System;
using System.Net;
using System.IO;
using System.Xml;
using GConf;

public class WebUtil {
	private static string KEY_PROXY_BASE = "/system/http_proxy";
	private static string KEY_PROXY_HOST = KEY_PROXY_BASE + "/host";
	private static string KEY_PROXY_PORT = KEY_PROXY_BASE + "/port";
	private static string KEY_PROXY_USE = KEY_PROXY_BASE + "/use_http_proxy";
	private static string KEY_PROXY_AUTH = KEY_PROXY_BASE + "/use_authentication";
	private static string KEY_PROXY_AUTH_USER = KEY_PROXY_BASE + "/authentication_user";	
	private static string KEY_PROXY_AUTH_PASSWORD = KEY_PROXY_BASE + "/authentication_password";

	public static XmlDocument FetchXmlDocument (string uri)
	{
		WebResponse resp = GetURI (uri);
		StreamReader input = new StreamReader (resp.GetResponseStream ());
		string xmltext = input.ReadToEnd ();
		input.Close ();
		XmlDocument xml = new XmlDocument ();
		try {
			xml.LoadXml (xmltext);
		} catch (System.Xml.XmlException e) {
			System.Console.WriteLine (xmltext);
			throw e;
		}
		
		return xml;
	}
	 
	public static WebResponse GetURI (string uri) 
	{
		GConf.Client client = new GConf.Client ();
		bool use_proxy = false;
		bool use_auth = false;
		string user = null;
		string password = null;
		string host = null;
		int port = 80;
		
		// TODO: handle proxy exceptions properly, it doesn't look like the format
		// used by GConf is the same as the format expected by the .NET exclusions
		// list. e.g. 127.0.0.1/8 needs to be translated to 127.*
		try {
			use_proxy = (bool) client.Get (KEY_PROXY_USE);

			if (use_proxy) {
				host = (string) client.Get (KEY_PROXY_HOST);
				port = (int) client.Get (KEY_PROXY_PORT);
				use_auth = (bool) client.Get (KEY_PROXY_AUTH);
				if (use_auth) {
					user = (string) client.Get (KEY_PROXY_AUTH_USER);
					password = (string) client.Get (KEY_PROXY_AUTH_PASSWORD);
				}
			}
		} catch (Exception e) {
			System.Console.WriteLine ("Error reading proxy preferences: {0}",
				e.Message);
		}
	
		HttpWebRequest req = (HttpWebRequest) WebRequest.Create (uri);
		if (use_proxy) {
			string proxyuri = String.Format ("http://{0}:{1}", 
				host, port);
			if (! use_auth) {
				req.Proxy = new WebProxy (proxyuri, true);
			} else {
				req.Proxy = new WebProxy (proxyuri, true, new string[] {},
					(ICredentials) new NetworkCredential (user, password));
			}
		}
		req.UserAgent = "Monopod";
		return req.GetResponse ();
	}
	 
	public static string GetXmlNodeText (XmlNode xml, string tag)
	{
		XmlNode node;

		node = xml.SelectSingleNode (tag);
		if (node == null)
			return "";

		return node.InnerText;
	}
}