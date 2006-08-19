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

#if USING_IPOD

using Mono.Posix;
using IPod;
using System;
using System.Collections; 
using System.IO;
using Entagged;

public class IPodSync
{
	private Store store;
	private Device device;
	private Hashtable stored_casts;

	internal IPodSync (Store s) 
	{
		store = s;
	}

	public IPodSync (Store s, Device dev) : this (s)
	{
		device = dev;
	}
	
	public IPodSync (Store s, string devpath) : this (s)
	{
		device = new Device (devpath);
	}
	
	// TODO: umm, why aren't we just using a Hashtable to store
	// this, rather than an array list?
	public void ReadStoredCasts ()
	{
		stored_casts = new Hashtable ();
		string fname = Path.Combine (
			Path.Combine (device.MountPoint, "iPod_Control"), "monopod");
		try {
			StreamReader r = new StreamReader (fname);
			bool done = false;
			while (! done) {
				string s = r.ReadLine ();
				if (s != null) {
					string[] l = s.Split (' ');
					int monopodid = Int32.Parse (l [0]);
					int ipodid = Int32.Parse (l [1]);
					stored_casts.Add (monopodid, ipodid);
				} else {
					done = true;
				}
			}
			r.Close ();
		} catch (Exception e) {
			System.Console.WriteLine ("Failed to open {0}, {1}", fname, e.Message);
		}
	}

	public void WriteStoredCasts ()
	{
		string fname = Path.Combine (
			Path.Combine (device.MountPoint, "iPod_Control"), "monopod");
		try {
			StreamWriter w = new StreamWriter (fname);
			foreach (int monopodid in stored_casts.Keys) {
				w.WriteLine ("{0} {1}", monopodid, stored_casts[monopodid]);
			}
			w.Close ();
		} catch (Exception e) {
			System.Console.WriteLine ("Failed to write {0}, {1}", fname, e.Message);
		}
	}

	private bool ShowIPodFull () 
	{
		ErrorDialog ed = new ErrorDialog (
			Catalog.GetString ("Not Enough Space - Monopod Podcast Client"),
			Catalog.GetString ("Not enough space on iPod"),
			Catalog.GetString ("Monopod automatically removes podcasts you have listened to. ") +
			Catalog.GetString ("Try clearing space by removing other music from your iPod.")
			);
		ed.Run ();
		ed.Destroy ();
		return false;
	}
	
	public void Sync ()
	{
		SongDatabase db = device.SongDatabase;
		string plistname = Catalog.GetString ("Recent podcasts");
		Playlist p = null;
		ArrayList old_songs = new ArrayList ();
		ArrayList played_songs = new ArrayList ();

		// search for, and remove, the playlist if it's there already
		foreach (Playlist cand in db.Playlists) {
			if (cand.Name == plistname)
				p = cand;
		}

		if (p != null) {
			// there's a playlist there already, have a squiz through
			// and see if anything's been played
			foreach (Song s in p.Songs) {
				old_songs.Add (s);
				if (s.PlayCount > 0) {
					played_songs.Add (s);
				}
			}
			db.RemovePlaylist (p);
		}

		// start with a fresh playlist		
		p = db.CreatePlaylist (plistname);
		
		ReadStoredCasts ();
		
		lock (store) {
			store.Synchronizing = true;
		}
		
		try {
			
			foreach (Cast k in store.RecentCasts (10))
			{
				int ipodid = -1;
				if (stored_casts.ContainsKey (k.ID)) {
					 ipodid = (int) stored_casts[k.ID];
				}
							
				if (ipodid == -1) {
					// if the song isn't stored on the ipod
					Song song = db.CreateSong ();
					// System.Console.WriteLine ("Getting details on {0}", k.LocalName);
					AudioFileWrapper wrap = new AudioFileWrapper (k.LocalName);
					
					if (wrap.Artist == null) {
						song.Artist = k.MyChannel.Title;
					} else {
						song.Artist = wrap.Artist;
					}
					
					song.Album = k.MyChannel.Title; 
					song.Title = k.Title;
					song.Genre = Catalog.GetString ("Podcast");
					song.Comment = k.Description;
					System.Console.WriteLine ("Song {0} Duration {1}", k.Title, wrap.Duration);
					song.Length = wrap.Duration * 1000;
					song.Year = wrap.Year;
					song.TrackNumber = wrap.TrackNumber;
					// song.TotalTracks = wrap.TrackCount;
					song.Filename = k.LocalName;
					
					stored_casts.Add (k.ID, song.Id);
					p.InsertSong (0, song);
					System.Console.WriteLine ("Added cast {0}", song);
				} else {
					// else, the song is on the ipod
					Song song = null;
					foreach (Song cand in db.Songs) {
						if (cand.Id == ipodid) {
							song = cand;
							break;
						}
					}
					if (song != null) {
						// we've found the song on the ipod, so we just add it into
						// the play list
						p.InsertSong (0, song);

						if (old_songs.Contains (song)) {
							// our playlist requires we keep this, so don't delete
							old_songs.Remove (song);
						}
						System.Console.WriteLine ("{0}-{1} already there, adding into list", k.ID, k.Title);
					} else {
						// weird.  we think the song's there, but it's not on the ipod
						// any more.  so, we remove it from the ids list
						stored_casts.Remove (k.ID);
						System.Console.WriteLine ("{0}-{1} already there, skipping", k.ID, k.Title);
					}
				}
			} // end foreach
			
			// now, delete songs that are old
			foreach (Song s in old_songs) {
				if (played_songs.Contains (s)) {
					foreach (int mp in stored_casts.Keys) {
						if ((int) stored_casts [mp] == s.Id) {
							System.Console.WriteLine ("Removing played cast {0}", s.Title);
							stored_casts.Remove (mp);
						}
					}
					db.RemoveSong (s);
				} else {
					// add unplayed ones onto the playlist
					// TODO: make this configurable, ie. fill only X megs
					p.InsertSong (0, s);
					// no need to change stored_casts, the song will be there				
				}
			}

			db.Save ();
			// TODO: currently if run out of room when saving, we don't WriteStoredCasts
			// at all.  To do things properly, we ought only to write which casts we
			// managed to save.
			WriteStoredCasts ();
		} catch (IPod.InsufficientSpaceException ie) {
			System.Console.WriteLine ("Not enough room");
			GLib.Idle.Add (ShowIPodFull);
		} catch (Exception e) {
			System.Console.WriteLine ("Exception during syncing: {0}", e);		
		} finally {
			lock (store) {
				store.Synchronizing = false;
			}		
		}

		System.Console.WriteLine ("Saving done");
	}

}
#endif