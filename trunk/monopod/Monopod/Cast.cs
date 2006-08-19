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

public class Cast {
    public static string CreationSQL = @"
    	CREATE TABLE cast (
    		id			integer not null primary key,
    		channel_id	integer not null,
    		url			text,
    		title       text,
    		description	text,
    		date		text,
    		fetched		numeric not null default 0,
    		category	text,
    		type        string,
    		length      integer not null,
    		error		string default ''
    	)";
    public Channel MyChannel;
	public string Url;
	public string Title;
	public string Description;
	public string Date;
	private System.DateTime _fetched;
	public string Category;
	public string Type;
	public int Length;
	private int id = -1;
	Store store;
	
	public int ID {
		get { return id; }
	}
	
	public System.DateTime Fetched {
		get { return _fetched; }
	}
	
	private string _error;
	public string Error {
		get { return _error; }
	}
	
	protected Cast (Channel c) 
	{
		MyChannel = c;
		store = c.Store;
	}
	
	public Cast (Channel c, string url, string title, string description,
		string type, string length) : this (c)
	{
		Url = url;
		Title = (title == "" ? c.Title : title);
		Description = (description == "" ? Title : description);
		Type = type;
		if (length == "") 
			Length = 0;
		else 
			Length = Int32.Parse (length);
		store = c.Store;
		ToDB ();
	}
	
	public Cast (Channel c, int dbid) : this (c)
	{
		id = dbid;
		FromDB ();
	}
	
	static public Cast CastForUrl (Channel c, string  url)
	{
		Cast k = new Cast (c);
		int id = k.IdForUrl (url);
		if (id >= 0) {
			k.id = id;
			k.FromDB ();
			return k;
		} else {
			return null;
		}
	}
	
	public override string ToString () 
	{
		return String.Format (@"Cast [{0}] ({1})
	Title       {2}
	Description {3}
	Type        {4}
	Length      {5},
	Cached      {6}",
			Url, id, Title, Description, Type, Length, LocalName);
	}
	
	private int IdForUrl (string url)
	{
		int theid = -1;
		
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = "select id from cast where url='" +
				SqlUtil.SqlString (url) + "';";
			dbcmd.CommandText = sql;
			IDataReader reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				theid = Int32.Parse ((string) reader[0]);
			}	 
			reader.Close ();
		}
		return theid;
	}

	private void ToDB ()
	{
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql;
			if (id < 0) 
				id = IdForUrl (Url);
			// System.Console.WriteLine("Writing cast, id {0}", id);
			if (id >= 0) {
				// exists in DB, so we update
				sql = String.Format (@"UPDATE cast SET
					url = '{0}',
					title = '{1}', 
					description = '{2}',
					type = '{3}',
					length = '{4}',
					channel_id = {5},
					fetched = {7},
					error = '{8}'
					WHERE id = {6};",
						SqlUtil.SqlString (Url),
						SqlUtil.SqlString (Title),
						SqlUtil.SqlString (Description),
						SqlUtil.SqlString (Type),
						Length,
						MyChannel.ID,
						id,
						_fetched.Ticks,
						(_error == null ? "" : SqlUtil.SqlString (_error))
					);
			} else {
				// is new, so we insert
				sql = String.Format (@"INSERT INTO cast
					(url, title, description, type, length, 
						channel_id) VALUES
					('{0}', '{1}', '{2}', '{3}', '{4}', '{5}');",
						SqlUtil.SqlString (Url),
						SqlUtil.SqlString (Title),
						SqlUtil.SqlString (Description),
						SqlUtil.SqlString (Type),
						Length,
						MyChannel.ID
					);
			}
			dbcmd.CommandText = sql;
			dbcmd.ExecuteNonQuery ();
			if (id < 0) {
				id = store.DbCon.LastInsertRowId;
			}
		}
	}

	public void Update (string title, string desc, string type, string length)
	{
		Title = title;
		Description = desc;
		Type = type;
		Length = Int32.Parse (length);
		ToDB ();
	}

	private void SaveError ()
	{
		lock (store) {
			if (id >= 0) {
				string sql = String.Format (@"UPDATE cast SET
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

	void FromDB ()
	{
		IDataReader reader;
		lock (store) {
			IDbCommand dbcmd = store.DbCon.CreateCommand ();
			string sql = "select url, title, description, type, length, " + 
				"fetched, error from cast where id = " + id.ToString ();
			dbcmd.CommandText = sql;
			reader = dbcmd.ExecuteReader ();
			if (reader.Read ()) {
				Url = (string) reader[0];
				Title = (string) reader[1];
				Description = (string) reader[2];
				Type = (string) reader[3];
				Length = Int32.Parse ((string) reader[4]);
				_fetched = new DateTime (Int64.Parse ((string) reader[5]));
				_error = (string) reader[6];
			}
			reader.Close ();
		}
		reader = null;
	}

	public string Extension {
		get {
			switch (Type) {
				case "audio/mpeg":
					return "mp3";
				case "application/ogg":
					return "ogg";
				case "audio/x-wav":
					return "wav";
				default:
					return "unknown";
			}
		}
	}
	
	public string LocalName {
		get {
			// this one is: id + suffix
			return Path.Combine (MyChannel.CacheDir,
				Store.SanitizeName(id.ToString () + "-" + Title + "." + Extension));
		}
	}
	
	public bool Fetch () 
	{
		_error = "";
		// fetches synchronously
		try {
			MyChannel.CheckCacheDir ();
			WebResponse resp = WebUtil.GetURI (Url);
			// TODO handle response codes
			BinaryReader input = new BinaryReader (resp.GetResponseStream ());
			BinaryWriter output = new BinaryWriter (File.Open (LocalName, FileMode.Create));
			int read;
			byte[] buf = new byte[1024];
			while ((read = input.Read (buf, 0, 1024)) > 0)
			{
				output.Write (buf, 0, read);
			}
			output.Close ();
			input.Close ();
			_fetched = DateTime.UtcNow;
		} catch (Exception e) {
			System.Console.WriteLine ("Error fetching {0}: {1}", Url, e.ToString ());
			_error = e.Message;
		}
		lock (store) {
			SaveError (); // saves error code and fetch time
		}
		return true;
	}
};


