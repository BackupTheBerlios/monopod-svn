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
using System.IO;
using System.Net;
using System.Data;
using System.Xml;
using System.Collections;
using Mono.Data.SqliteClient;

public class Channel : IEnumerator, IEnumerable {
	public static string CreationSQL = @"
		CREATE TABLE channel ( 
		   id 			integer not null primary key, 
		   url 			text not null,
		   homepage     text default '',
		   title        text not null, 		
		   description	text default '', 		
		   imageurl		text default '',
		   error		text default '', 		
		   fetched 		numeric not null default 0,	
		   subscribed	integer not null default 0,
		   category     text default ''
		)";
		
	private int id = -1;
	private Store store;
	
	public string Url;
	public string Homepage;
	public string Title;
	public string Description;
	public string ImageUrl;
	public string LocalImageName;
	private System.DateTime _fetched;
	
	private bool _subscribed;
	public bool Subscribed {
		get { return _subscribed; }
		set { _subscribed = value; SaveSubscribed (); }
	}
	
	private string _category = "";
	public string Category {
		get { return _category; }
		set { _category = value; SaveCategory (); }
	}
	
	public System.DateTime Fetched {
		get { return _fetched; }
	}
	
	private string _error;
	public string Error {
		get { return _error; }
	}
	
	// id-less constructor makes a new channel
	// not yet stored in the db
	protected Channel (Store s)
	{
		store = s;
	}
	
	public Channel (Store s, int dbid) : this (s)
	{
		id = dbid;
		FromDB ();
	}
	
	public Channel (Store s, string url, string homepage,
	 	string title, string description, string imageurl,
	 	string category) : this (s)
	{
		Url = url;
		Homepage = homepage;
		Title = title;
		Description = description;
		ImageUrl = imageurl;
		_category = category;
		ToDB ();
	}
	
	public Channel (Store s, string url) : this (s)
	{
		Url = url;
		Refresh ();
	}
	
	public Cast GetCast (int id)
	{
		return new Cast (this, id);
	}
	
	public int ID {
		get { return id; }
	}
	
	public Store Store {
		get { return store; }
	}
	
	public void Refresh ()
	{
		XmlDocument xml;
		_error = "";
		_fetched = DateTime.UtcNow;
		try {
			xml = WebUtil.FetchXmlDocument (Url);
			FromXML (xml);
			ToDB ();
			LoadCasts (xml);
		} catch (Exception e) {
			System.Console.WriteLine ("RSS Parsing Exception {0}", e.ToString ());
			_error = e.Message;
			SaveError (); // store error and fetch attempt time
		}
	}
	
	void FromXML (XmlDocument xml)
	{
		string title = WebUtil.GetXmlNodeText (xml, "/rss/channel/title");
		
		if (title == "") {
			throw new System.ApplicationException ("Channel has no title");
		}
		
		Title = StringUtils.StripHTML (title.Trim ());
		Description = StringUtils.StripHTML (
			 WebUtil.GetXmlNodeText (xml, "/rss/channel/description").Trim ());
		Homepage = WebUtil.GetXmlNodeText (xml, "/rss/channel/link").Trim ();
		ImageUrl = WebUtil.GetXmlNodeText (xml, "/rss/channel/image/url").Trim ();
		System.Console.WriteLine ("{0} - {1}", Title, Homepage);
	}
	
	private static bool KnownType (string type) 
	{
		switch (type) {
			case "audio/mpeg":
			case "application/ogg":
			case "audio/x-wav":
				return true;
			default:
				return false;
		}
	}
	
	void LoadCasts (XmlDocument xml)
	{
		ArrayList toadd = new ArrayList ();
		XmlNodeList items = xml.SelectNodes ("//item");
		foreach (XmlNode item in items) {
			string encurl = WebUtil.GetXmlNodeText (item, "enclosure/@url");
			string type = WebUtil.GetXmlNodeText (item, "enclosure/@type");
			// HACK: if type is empty, assume it's audio/mpeg.
			if (encurl != "" && (type == null || type == "" || KnownType (type))) {
				toadd.Add (item);
			}
		}
		// add in reverse order, so the IDs go up in chronological order
		toadd.Reverse ();
		foreach (XmlNode item in toadd)
		{
			// create and store cast
			string encurl = WebUtil.GetXmlNodeText (item, "enclosure/@url");
			Cast k = Cast.CastForUrl (this, encurl);
			string title = 	StringUtils.StripHTML (WebUtil.GetXmlNodeText (item, "title").Trim());
			string desc = StringUtils.StripHTML (WebUtil.GetXmlNodeText (item, "description").Trim());
			string type = WebUtil.GetXmlNodeText (item, "enclosure/@type");
			// HACK: if type is empty, assume it's audio/mpeg.
			if (type == null || type == "") {
				type = "audio/mpeg";
			}
			string length = WebUtil.GetXmlNodeText (item, "enclosure/@length");
			if (k != null) {
				k.Update (title, desc, type, length);
			} else {
			// CreateCastIfNeeded ();
				new Cast (this,	encurl, title, desc, type, length);
			}
			System.Console.WriteLine ("{0} {1}", title, encurl);
		}
	}
	

	void FromDB ()
	{
		lock (store) {
			
			// System.Console.WriteLine ("FromDB, sotre is {0} con {1}", store, 
			//	(store.DbCon == null ? "null" : "nonnull"));
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = "select url, homepage, title, description, imageurl, fetched, " + 
				"subscribed, category, error from channel where id = " + id.ToString ();
			// System.Console.WriteLine (sql);
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				Url = (string) reader[0];
				Homepage = (string) reader[1];
				Title = (string) reader[2];
				Description = (string) reader[3];
				ImageUrl = (string) reader[4];
				long ticks = (long)reader[5];
				_fetched = new DateTime (ticks);
				_subscribed = (bool) ((int)reader[6] != 0);
				_category = (string) reader[7];
				_error = (string) reader[8];
				// TODO: marshal the other two values across proeprly
			}
			reader.Close ();
			reader = null;
		}
	}
	
	private int IdForUrl (string url)
	{
		int theid = -1;
		
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = "select id from channel where url='" +
				SqlUtil.SqlString (url) + "';";
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				theid = Int32.Parse (reader[0].ToString());
			}	 
			reader.Close ();
		}
		return theid;
	}

	public int NumberCasts ()
	{
		int num = 0;
	
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = String.Format (@"select count(*) from cast where
				channel_id = {0}",
					id); 
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				num = Int32.Parse (reader[0].ToString());
			}	 
			reader.Close ();
		}
		return num;		
	}

	public int NumberErrorCasts ()
	{
		int num = 0;
	
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = String.Format (@"select count(*) from cast where
				channel_id = {0}  and error != ''",
					id); 
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				num = Int32.Parse (reader[0].ToString());
			}	 
			reader.Close ();
		}
		return num;		
	}

	public int NumberUndownloadedCasts ()
	{
		int num = 0;
	
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = String.Format (@"select count(*) from cast where
				channel_id = {0} and fetched = 0 and error = ''",
					id); 
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				num = Int32.Parse (reader[0].ToString());
			}	 
			reader.Close ();
		}
		return num;		
	}
	
	public override string ToString ()
	{
		return String.Format (@"Channel [{0}] ({5})
	Title    {1}
	Homepage {2}
	Image    {3}
	Description:
		{4}", 
			Url, Title, Homepage, ImageUrl, Description, id);
	}
	
	void ToDB ()
	{
		lock (store) {
			
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql;
			if (id < 0) 
				id = IdForUrl (Url);
			if (id >= 0) {
				// exists in DB, so we update
				sql = String.Format (@"UPDATE channel SET
					url = '{0}',
					title = '{1}', 
					description = '{2}',
					imageurl = '{3}',
					homepage = '{4}',
					category = '{7}',
					subscribed = {6},
					fetched = {8},
					error = '{9}'
					WHERE id = {5};",
						SqlUtil.SqlString (Url),
						SqlUtil.SqlString (Title),
						SqlUtil.SqlString (Description),
						SqlUtil.SqlString (ImageUrl),
						SqlUtil.SqlString (Homepage),
						id,
						(_subscribed ? 1 : 0),
						(_category == null ? "" : SqlUtil.SqlString (_category)),
						(_fetched.Ticks),
						(_error == null ? "" : SqlUtil.SqlString (_error))
					);
			} else {
				// is new, so we insert
				sql = String.Format ("INSERT INTO channel "+
					"(url, title, description, imageurl, homepage, subscribed, " +
					"category, fetched) VALUES " +  
					"('{0}', '{1}', '{2}', '{3}', '{4}', {5}, '{6}', {7});",
						SqlUtil.SqlString (Url),
						SqlUtil.SqlString (Title),
						SqlUtil.SqlString (Description),
						SqlUtil.SqlString (ImageUrl),
						SqlUtil.SqlString (Homepage),
						(_subscribed ? 1 : 0),
						(_category == null ? "" : SqlUtil.SqlString (_category)),
						(_fetched.Ticks)
					);
			}

			dbcmd.CommandText = sql;
			// System.Console.WriteLine(sql);
			dbcmd.ExecuteNonQuery ();
			if (id < 0) {
				id = store.DbCon.LastInsertRowId;
			}
			
		}
	}

	private void SaveError ()
	{
		lock (store) {
			if (id >= 0) {
				string sql = String.Format (@"UPDATE channel SET
					error = '{1}', fetched = {2}
					WHERE id = {0};",
						id,
						SqlUtil.SqlString (_error),
						_fetched.Ticks
					);
					
				IDbCommand dbcmd = store.DbCon.CreateCommand ();
				dbcmd.CommandText = sql;
				dbcmd.ExecuteNonQuery ();	
			}
		}
	}
	
	public void ClearError ()
	{
		_error = "";
		_fetched = new DateTime (0);
		SaveError ();
	}

	private void SaveCategory ()
	{
		lock (store) {
			if (id >= 0) {
				string sql = String.Format (@"UPDATE channel SET
					category = '{1}'
					WHERE id = {0};",
						id,
						SqlUtil.SqlString (_category) 
					);
					
				IDbCommand dbcmd = store.DbCon.CreateCommand ();
				dbcmd.CommandText = sql;
				dbcmd.ExecuteNonQuery ();	
			}
		}
	}
	
	private void SaveSubscribed ()
	{
		lock (store) {
			if (id >= 0) {
				string sql = String.Format (@"UPDATE channel SET
					subscribed = {1}
					WHERE id = {0};",
						id,
						(_subscribed ? 1 : 0) 
					);
					
				IDbCommand dbcmd = store.DbCon.CreateCommand ();
				dbcmd.CommandText = sql;
				dbcmd.ExecuteNonQuery ();	
			}
		}
	}
	
	public IEnumerator GetEnumerator () { return this; }
	
	// IEnumerator implementation
	
	private IDataReader itereader = null;
	private Cast itercast;
	
	public void Reset ()
	{
		lock (store) {
			IDbCommand cmd = store.DbCon.CreateCommand ();
			cmd.CommandText = "SELECT id FROM cast WHERE channel_id = " + 
				id.ToString ();
			itereader = cmd.ExecuteReader ();
		}
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
		int i = Int32.Parse (itereader[0].ToString());
		itercast = new Cast (this, i);
		return true;
	}
	
	public object Current {
		get { return itercast; }
	}
	
	public void CheckCacheDir ()
	{
		// makes cache dir if not exist already
		if (!Directory.Exists (CacheDir)) {
			DirectoryInfo d = Directory.CreateDirectory (CacheDir);
			if ( d == null ) {
				// TODO: throw a wobbly
			}
		}
	}
	
	public string CacheDir {
		get { 
			return Path.Combine (store.CacheDir, 
				Store.SanitizeName (Title));
		}		
	}
	
	public void WritePlaylist ()
	{
		string fname = Path.Combine (CacheDir, "playlist.m3u"); 
		string sql = String.Format (@"select id from cast where channel_id = {0} AND
			fetched > 0 AND error = '' order by id",
			id);
		ArrayList ids = new ArrayList ();
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			dbcmd.CommandText = sql;
			System.Console.WriteLine(sql);
			IDataReader reader = dbcmd.ExecuteReader ();
			while (reader.Read ()) {
				ids.Add (Int32.Parse (reader[0].ToString()));
			}
			reader.Close ();
		}
		StreamWriter output = new StreamWriter (File.Open (fname, FileMode.Create));
		output.WriteLine ("# Playlist generated by Monopod");
		output.WriteLine ("# Channel: " + Title);
		output.WriteLine ("# Url    : " + Url);
		foreach (int i in ids) {
			Cast k = GetCast (i);
			output.WriteLine (k.LocalName);
		}
		output.Close ();
	}
	
	public void Delete () {
		lock (store) {
			string sql = "delete from channel where id = " + id.ToString ();
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			dbcmd.CommandText = sql;
			dbcmd.ExecuteNonQuery ();
			dbcmd = store.DbCon.CreateCommand ();
			sql = "delete from cast where channel_id = " + id.ToString ();
			dbcmd.CommandText = sql;
			dbcmd.ExecuteNonQuery ();	
		}
	}
	
	public void TrashCacheDir ()
	{
		try {
			Directory.Delete (CacheDir, true);
		} catch (IOException e) {
			Console.WriteLine (e.Message);
		} catch (Exception e) {
			Console.WriteLine (e.Message);
		}
	}
};

