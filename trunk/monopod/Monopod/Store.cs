/*
 * Copyright (C) 2005 Edd Dumbill <edd@gnome.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */
 
using System;
using System.Data;
using System.Collections;
using System.IO;
using System.Xml;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using Mono.Data.SqliteClient;
using GConf;

public class Store : IEnumerable, IEnumerator {
	private SqliteConnection connection;
	private string _filename;
	private string _cachedir;
	
	delegate Channel ChannelFetcher (Channel c);
	delegate Channel NewChannelFetcher (string url);
	delegate Cast CastFetcher (Cast c);
	
	public delegate void ChannelWatcher (Channel c);
	public event ChannelWatcher ChannelUpdated; 
	public event ChannelWatcher ChannelAdded; 
	
	private ArrayList updates;
	private ArrayList newchans;
	
	private int _fetching = 0;
	private int _fetchingcasts = 0;
	private Object _fetchinglock = new Object ();
	
	// Synchronizing flag. We can't delete stuff if
	// synchronizing.  Adding's OK.
	private bool _synchronizing = false;
	public bool Synchronizing {
		get { return _synchronizing; }
		set { _synchronizing = value; }
	}
	
	public string Filename {
		get { return _filename; }
	}
		
	public string CacheDir {
		get { return _cachedir; }
	}
	
	public SqliteConnection DbCon {
		get { return connection; }
	}

	public Store (string fname, string cachedir)
	{
		updates = new ArrayList ();
		newchans = new ArrayList ();
		connection = new SqliteConnection ("URI=file:" + fname);
		connection.Open ();

		IDbCommand dbcmd = connection.CreateCommand ();
		// setting encoding only works on sqlite3
		//dbcmd.CommandText = "PRAGMA encoding = \"UTF-8\";";
		//if (dbcmd.ExecuteNonQuery () != 0) {
		//	throw new Exception ("Couldn't set encoding");
		//}		
		string sql = "PRAGMA table_info(channel)";
		dbcmd = connection.CreateCommand ();
		dbcmd.CommandText = sql;
		try {
			IDataReader result = dbcmd.ExecuteReader ();
 			if (!result.Read ()) {
 				// schema doesn't exist.
 				System.Console.WriteLine ("Creating new database schema");
 				CreateSchema ();                
			}
			result.Close ();
		} catch (Exception e) {
			// TODO: catch further exceptions here.
		}
		_cachedir = cachedir;
		InitializeCache ();
		// TODO: catch problems making the cache dir 
	}
	
	public Channel GetChannel (int id)
	{
		return new Channel (this, id);
	}
	
	private void InitializeCache ()
	{
		if (!Directory.Exists (_cachedir)) {
			DirectoryInfo d = Directory.CreateDirectory (_cachedir);
			if ( d == null ) {
				// TODO: throw a wobbly
			}
		}
	}
	
	private void CreateSchema ()
	{
		IDbCommand dbcmd = connection.CreateCommand ();
		

		dbcmd.CommandText = Channel.CreationSQL;
		if (dbcmd.ExecuteNonQuery () != 0) {
			throw new Exception ("Couldn't create Channel table");
		}
		dbcmd.CommandText = Cast.CreationSQL;
		if (dbcmd.ExecuteNonQuery () != 0) {
			throw new Exception ("Couldn't create Cast table");
		}	
	}
	
	public void Close ()
	{
		connection.Close ();
		connection = null;
	}
	
	public int NumberOfChannels {
		get {
			IDbCommand cmd = connection.CreateCommand ();
			cmd.CommandText = "SELECT COUNT(*) FROM channel";
			string n = (string) cmd.ExecuteScalar ();
			return Int32.Parse (n);
		}
	}
	
	// IEnumerable
	
	public IEnumerator GetEnumerator () { return this; }
	
	// IEnumerator implementation
	
	private IDataReader itereader = null;
	private Channel iterchan;
	
	public void Reset ()
	{
		IDbCommand cmd = connection.CreateCommand ();
		cmd.CommandText = "SELECT id FROM channel ORDER BY title";
		itereader = cmd.ExecuteReader ();
	}
	
	public bool MoveNext ()
	{
		if (itereader == null) {
			Reset ();
		}
			
		if (! itereader.Read ()) {
			itereader.Close ();
			itereader = null;
			return false;
		}
		int i = Int32.Parse ((string) itereader[0]);
		iterchan = new Channel (this, i);
		return true;
	}
	
	public object Current {
		get { return iterchan; }
	}
	
	public static string SanitizeName (string s) 
	{
		// remove /, : and \ from names
		return s.Replace ('/', '_').Replace ('\\', '_').Replace (':', '_');
	}
	
	public ArrayList ChannelSearch (string[] queryterms)
	{
		ArrayList ids = new ArrayList ();
		lock (this) {
			string sql = "SELECT DISTINCT id FROM channel WHERE ";

			for (int i = 0; i < queryterms.Length; i++) 
			{
				string term = queryterms [i];
				if (term == "")
					continue;
				sql = sql + "(title LIKE '%" + SqlUtil.SqlString (term) + "%' ";
				sql = sql + "OR description LIKE '%" + SqlUtil.SqlString (term) + "%' ";
			    sql = sql + "OR category LIKE '%" + SqlUtil.SqlString (term) + "%' ";
				sql = sql + ") ";
				if (i < queryterms.Length-1) 
					sql = sql + "AND "; 
			}
			System.Console.WriteLine (sql);
			IDbCommand cmd = connection.CreateCommand ();
			cmd.CommandText = sql;
			IDataReader reader = cmd.ExecuteReader ();
			if (reader != null) {
				while (reader.Read ()) {
					ids.Add (Int32.Parse ((string) reader[0]));
					// System.Console.WriteLine("Result channel {0}", (string)reader[0]);
				}
			}
		}
		return ids;
	}
	
	public Channel NextUnfetchedChannel ()
	{
		Channel ret = null;
		DateTime now = DateTime.UtcNow;
		DateTime then = now.AddDays (-1.0);

		// We will poll channels daily, oldest first
		string sql = String.Format (@"SELECT id FROM channel WHERE subscribed = 1  
			AND fetched <= {0} ORDER BY fetched LIMIT 1",
			then.Ticks );
		// System.Console.WriteLine (sql);
		lock (this) {
			IDbCommand cmd = connection.CreateCommand ();
			cmd.CommandText = sql;
			IDataReader reader = cmd.ExecuteReader ();
			if (reader.Read ()) {
				ret = GetChannel (Int32.Parse ((string) reader[0]));
			}
		}
		return ret;
	}
	
	public Cast NextUnfetchedCast ()
	{
		int theid = -1;
		int thechan = -1;
	
		lock (this) {
			IDbCommand dbcmd = connection.CreateCommand ();
			string sql = String.Format (@"select cast.id, channel.id from cast, channel where
				cast.fetched = 0 and cast.error = '' and channel.subscribed = 1 and channel.id = cast.channel_id
				order by cast.id limit 1"
				);
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				theid = Int32.Parse ((string) reader[0]);
				thechan = Int32.Parse ((string) reader[1]);
			}
			reader.Close ();
		}
		if (theid >= 0) {
			Channel c = GetChannel (thechan);
			Cast k = c.GetCast (theid);
			return k;
		}
		return null;
	}
	
	private Channel FetchChannel (Channel c)
	{
		c.Refresh ();
		return c;
	}

	private Channel FetchNewChannel (string url)
	{
		Channel c = new Channel (this, url);
		return c;
	}	
	
	private Cast FetchCast (Cast c)
	{
		c.Fetch ();
		return c;
	}
	
	public bool FetchNextChannel ()
	{
		bool proceed = true;
		
		// Ensure we don't run more than one fetch at a time
		lock (_fetchinglock) {
			if (_fetching > 0)
				proceed = false;
		}
		
		if (! proceed) {
			GLib.Timeout.Add (1000*30, new GLib.TimeoutHandler (FetchNextChannel));
			return false;
		}
			
		Channel c = NextUnfetchedChannel ();
		if (c != null) {
			ChannelFetcher fetcher = new ChannelFetcher (FetchChannel);
			AsyncCallback ac = new AsyncCallback (ChannelFetched);
			lock (_fetchinglock) {
				_fetching++;
			}
			fetcher.BeginInvoke (c, ac, c);
		} else {
			// schedule a re-check in a minute's time
			GLib.Timeout.Add (1000*60, new GLib.TimeoutHandler (FetchNextChannel));
		}
		return false;
	}

	public bool AddNewChannel (string url)
	{
		if (url != "") {
			NewChannelFetcher fetcher = new NewChannelFetcher (FetchNewChannel);
			AsyncCallback ac = new AsyncCallback (NewChannelFetched);
			lock (_fetchinglock) {
				_fetching++;
			}
			fetcher.BeginInvoke (url, ac, url);
		}
		return false;
	}

	public bool FetchNextCast ()
	{
		bool proceed = true;
		
		// Ensure we don't run more than one fetch at a time
		lock (_fetchinglock) {
			if (_fetchingcasts > 0)
				proceed = false;
		}
		if (! proceed) {
			GLib.Timeout.Add (1000*30, new GLib.TimeoutHandler (FetchNextCast));
			return false;
		}
		Cast c = NextUnfetchedCast ();
		if (c != null) {
			CastFetcher fetcher = new CastFetcher (FetchCast);
			AsyncCallback ac = new AsyncCallback (CastFetched);
			lock (_fetchinglock) {
				_fetchingcasts++;
			}
			System.Console.WriteLine("Starting to download {0}", c.Url);
			fetcher.BeginInvoke (c, ac, c);
		} else {
			// schedule a re-check in a minute's time
			GLib.Timeout.Add (1000*60, new GLib.TimeoutHandler (FetchNextCast));
		}
		return false;
	}
	
	private void ChannelFetched (IAsyncResult ar)
	{
		ChannelFetcher fetcher = (ChannelFetcher) ((AsyncResult) ar).AsyncDelegate;
		Channel c = fetcher.EndInvoke (ar);
		System.Console.WriteLine ("Fetched {0}", c);
		lock (updates) {
			updates.Add (c);
		}
		lock (_fetchinglock) {
			_fetching--;
		}
		GLib.Idle.Add (new GLib.IdleHandler (NotifyChannelWatchers));
		FetchNextChannel ();
		FetchNextCast ();
	}
	
	private void NewChannelFetched (IAsyncResult ar)
	{
		NewChannelFetcher fetcher = (NewChannelFetcher) ((AsyncResult) ar).AsyncDelegate;
		Object o = fetcher.EndInvoke (ar);
		System.Console.WriteLine("Fetched New {0}", o);
		Channel c = (Channel) o;
		System.Console.WriteLine ("Fetched new {0}", c);
		lock (newchans) {
			newchans.Add (c);
		}
		lock (_fetchinglock) {
			_fetching--;
		}
		GLib.Idle.Add (new GLib.IdleHandler (NotifyNewChannelWatchers));
	}
	
	private void CastFetched (IAsyncResult ar)
	{
		CastFetcher fetcher = (CastFetcher) ((AsyncResult) ar).AsyncDelegate;
		Cast c = fetcher.EndInvoke (ar);
		c.MyChannel.WritePlaylist ();
		System.Console.WriteLine ("Fetched {0}", c);
		lock (updates) {
			updates.Add (c.MyChannel);
		}
		lock (_fetchinglock) {
			_fetchingcasts--;
		}
		GLib.Idle.Add (new GLib.IdleHandler (NotifyChannelWatchers));
		WritePlaylist ();
		FetchNextCast ();
	}

	private bool NotifyChannelWatchers ()
	{
		Channel c;
		lock (updates) {
			if (updates.Count > 0) {
				c = (Channel) updates [0];
				updates.RemoveAt (0);
				System.Console.WriteLine ("Notifying update of {0}", c.Title);
				ChannelUpdated (c);
			}
		}
		return false;
	}	

	private bool NotifyNewChannelWatchers ()
	{
		Channel c;
		lock (newchans) {
			if (newchans.Count > 0) {
				c = (Channel) newchans [0];
				newchans.RemoveAt (0);
				System.Console.WriteLine ("Notifying addition of {0}", c.Title);
				ChannelAdded (c);
			}
		}
		return false;
	}
	
	public void AddDefaultChannels ()
	{
		new Channel (this, "http://downloads.bbc.co.uk/rmhttp/downloadtrial/radio4/fromourowncorrespondent/rss.xml",
			"", "From Our Own Correspondent",
			"Insight, colour, wit and analysis as the BBC's foreign correspondents take a closer look at the stories in their regions.",
			"", "Current Affairs");
		new Channel (this, "http://downloads.bbc.co.uk/rmhttp/downloadtrial/radio4/today/rss.xml",
			"", "Today",
			"Today, BBC Radio 4. The Today Programme is BBC Radio's leading news and current affairs programme.",
			"", "News");
		new Channel (this, "http://www.kcrw.com/podcast/show/bw",
			"", "KCRW's Bookworm",
			"A must for the serious reader, \"Bookworm\"  showcases writers of fiction and poetry - the established, new or emerging - all interviewed with insight and precision by the show's host and guiding spirit, Michael Silverblatt.",
			"", "Arts");
		new Channel (this, "http://www.kcrw.com/podcast/show/gf",
			"", "KCRW's Good Food",
			"Your weekly treat from Evan Kleiman.  By tuning in to Good Food, you can discover delicious recipes, great restaurants, and unique places to buy authentic ingredients; find out how to prepare the newest foods in the marketplace; learn techniques of master chefs and ideas for novices; and listen to discussions about food politics and the latest trends in food and eating.",
			"", "Cuisine");
		new Channel (this, "http://www.kcrw.com/podcast/show/fr",
			"", "KCRW's Film Reviews",
			"Joe Morgenstern shares his thoughts on current films.",
			"", "Arts");
		new Channel (this, "http://www.kcrw.com/podcast/show/tp",
			"", "KCRW's To The Point",
			"\"To the Point\" is a fast-paced, news based one-hour daily national program that focuses on the hot-button issues of the day, co-produced by KCRW and Public Radio International.",
			"", "News");
		new Channel (this, "http://www.kcrw.com/podcast/show/lr",
			"", "KCRW's Left, Right & Center",
			"Provocative, up-to-the-minute, alive and witty, KCRW's weekly confrontation over politics, policy and popular culture proves those with impeccable credentials needn't lack personality. Featuring four of the most insightful news analysts anywhere, this weekly \"love-hate relationship of the air\" reaches about 50,000 of the most influential radio listeners in Southern California.",
			"", "Current Affairs");
		new Channel (this, "http://www.kcrw.com/podcast/show/mb",
			"", "KCRW's Morning Becomes Eclectic",
			"A music experience that celebrates innovation, creativity and diversity by combining progressive pop, world beat, jazz, African, reggae, classical and new music.",
			"", "Music");		
 	}
	
	public void ImportOPML (string opmlurl)
	{
		XmlDocument xml = WebUtil.FetchXmlDocument (opmlurl);
		XmlNodeList items = xml.SelectNodes ("//outline");
		string category = "";
		foreach (XmlNode item in items) {
			string title = WebUtil.GetXmlNodeText (item, "@text");
			string url = WebUtil.GetXmlNodeText (item, "@url");
			string xmlUrl = WebUtil.GetXmlNodeText (item, "@htmlUrl");
			string htmlUrl = WebUtil.GetXmlNodeText (item, "@xmlUrl");
			string type = WebUtil.GetXmlNodeText (item, "@type");
			if (type != "link" || title == "") {
				if (title != "" && url == "" && xmlUrl == "" && htmlUrl == "") {
					// we imply this is a category
					category = title;
					// doh, how useless is this
					if (category == "Uncategorized")
						category = "";
					// System.Console.WriteLine("Found category {0}", category);
				}
				continue;
			}
			title = title.Trim (' ');
			string turl = (xmlUrl == "" ? url : xmlUrl);
			bool isnew = false;
			lock (this) {
				IDbCommand dbcmd = connection.CreateCommand ();
				string sql = "select id from channel where url='" +
					SqlUtil.SqlString (turl) + "';";
				dbcmd.CommandText = sql;
				IDataReader reader = dbcmd.ExecuteReader ();
				if (! reader.Read ())
					isnew = true;
				reader.Close ();
			}
			if (isnew)
				new Channel (this, turl, htmlUrl, title, turl, "", category);
		}
	}

	public ArrayList RecentCasts (int num)
	{
		ArrayList res = new ArrayList ();
		int chanid = 0;
		int id = 0;
		
		string sql = String.Format (@"select id, channel_id from cast where 
			fetched > 0 AND error = '' order by fetched desc limit {0}",
			num);

		lock (this) {
			IDbCommand dbcmd = connection.CreateCommand ();
			dbcmd.CommandText = sql;

			IDataReader reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				id = Int32.Parse ((string) reader[0]);
				chanid = Int32.Parse ((string) reader[1]);
				Cast k = GetChannel (chanid).GetCast (id);
				res.Add (k);				
			}
			reader.Close ();
		}
		return res;
	}

	public void WritePlaylist (string fname)
	{
		StreamWriter output = new StreamWriter (File.Open (fname, FileMode.Create));
		output.WriteLine ("# Playlist generated by Monopod");
		output.WriteLine ("# Most 50 recent podcasts.");
		foreach (Cast k in RecentCasts (50)) { 
			output.WriteLine (k.LocalName);				
		}
		output.Close ();
	}

	public void WritePlaylist ()
	{
		WritePlaylist (Path.Combine (CacheDir, "recent.m3u")); 
	}

}
