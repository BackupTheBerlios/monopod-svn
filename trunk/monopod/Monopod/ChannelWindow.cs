/*
 * Copyright (C) 2005 Edd Dumbill <edd@gnome.org>
 *
 * Portions based on code from Muine, and are:
 * Copyright (C) 2005 Tamara Roberson <foxxygirltamara@gmail.com>
 *           (C) 2003, 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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

using Gtk;
using Mono.Unix;

public class CellRendererText : Gtk.CellRendererText
{
	public CellRendererText ()
	{
	    SetProperty ("ellipsize", new GLib.Value (3));
	}
}

public class ChannelWindow : Window
{
	public enum ResponseType {
	    Close       = Gtk.ResponseType.Close,
	    DeleteEvent = Gtk.ResponseType.DeleteEvent,
	    Add         = 1,
	    Delete      = 2
	};

	public enum ShowMode {
		All,
		Subscribed,
		Errors
	};

	[Glade.Widget] private Window window;
	[Glade.Widget] private Label search_label;
	[Glade.Widget] private ScrolledWindow scrolledwindow;
	[Glade.Widget] private Container entry_container;
	[Glade.Widget] private Button delete_button;
	[Glade.Widget] private ComboBox showbox;

	private SearchEntry entry = new SearchEntry ();
	private ItemList list = new ItemList ();
	private CellRenderer text_renderer = new CellRendererText ();
	
	private uint search_timeout_id = 0;
	private bool first_time = true;
	private bool ignore_change = false;
	private const uint search_timeout = 100;
	
	private Store chanstore;
	private string lastsearch;
	private int lastmode;
	
	public ChannelWindow (Store s) : base (IntPtr.Zero)
	{
		chanstore = s;
		Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
		gxml.Autoconnect (this);
		Raw = window.Handle;
		search_label.MnemonicWidget = entry;
		entry_container.Add (entry);
		entry.KeyPressEvent += new Gtk.KeyPressEventHandler (OnEntryKeyPressEvent);
		entry.Changed += new System.EventHandler (OnEntryChanged);

		scrolledwindow.Add (list);

		window.SetDefaultSize (500, 400);
		window.Title = Catalog.GetString ("Subscriptions - Monopod Podcast Client");
		
		TreeViewColumn col = new TreeViewColumn ();
		col.Sizing = TreeViewColumnSizing.GrowOnly;
		col.Resizable = true;
		col.Spacing = 4;
		col.PackStart (TextRenderer, true);
		col.SetCellDataFunc (TextRenderer, new TreeCellDataFunc (TextCellDataFunc));
		col.Title = Catalog.GetString ("Channel");
		col.Expand = true;

		TreeViewColumn col2 = new TreeViewColumn ();
		col2.Sizing = TreeViewColumnSizing.Autosize;
		CellRendererToggle tog = new Gtk.CellRendererToggle ();
		col2.PackStart (tog, true);
		col2.SetCellDataFunc (tog, new TreeCellDataFunc (BoolCellDataFunc));
		tog.Activatable = true;
		col2.Spacing = 4;
		col2.Title = Catalog.GetString ("Subscribed");
		col2.Expand = false;
		col2.Alignment = 0.5f;
		tog.Toggled += OnCellToggled;

		//pix_error = new Gdk.Pixbuf (null, "rss-error.png");
		//pix_ok = new Gdk.Pixbuf (null, "rss-ok.png");
		//pix_queued = new Gdk.Pixbuf (null, "rss-queued.png");
		

		TreeViewColumn col3 = new TreeViewColumn ();
		col3.Sizing = TreeViewColumnSizing.Autosize;
		CellRendererPixbuf pix = new Gtk.CellRendererPixbuf ();
		col3.PackStart (pix, true);
		col3.SetCellDataFunc (pix, new TreeCellDataFunc (PixCellDataFunc));
		col3.Spacing = 4;
		col3.Title = Catalog.GetString ("Status");
		col3.Expand = false;
		col3.Alignment = 0.5f;

		list.AppendColumn (col);
		list.AppendColumn (col2);
		list.AppendColumn (col3);	

		list.Selection.Changed += OnSelectionChanged;
		
		showbox.Active = (int) ShowMode.All;
		
		window.Icon = MainClass.program_pixbuf48;
		
		entry.Show ();
		list.Show ();
		window.Realize ();

		// handler for when the database gets updated
		chanstore.ChannelUpdated += OnChannelUpdated;
		chanstore.ChannelAdded += OnChannelAdded;

		// model is simply a list of integers of channel IDs
		ListStore store = new ListStore (typeof (Channel));
		list.Model = store;
		store.SetSortFunc (0, StoreSortFunc);
		store.SetSortColumnId (0, SortType.Ascending);

		lastsearch = null;
		lastmode = -1;
	}
	
	public void Run ()
	{
		// make a clean start each time we're opened.
		// code borrowed from Muine
		if (first_time || entry.Text.Length > 0) {
			window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
			window.GdkWindow.Display.Flush ();

			ignore_change = true;
			entry.Text = "";
			ignore_change = false;
			GLib.Idle.Add (new GLib.IdleHandler (Reset));
			first_time = false;
		} else {
			list.SelectFirst ();
		}
		entry.GrabFocus ();
		window.Present ();
	}
	
	private bool RestoreCursor ()
	{
	    window.GdkWindow.Cursor = null;
	    return false;
	}

	private bool Reset ()
	{
		Search ();

		// We want to get the normal cursor back *after* treeview
		// has done its thing.
		GLib.Idle.Add (new GLib.IdleHandler (RestoreCursor));

		return false;
	}

	// Data Func
	private void TextCellDataFunc (TreeViewColumn col, CellRenderer cell,
		TreeModel model, TreeIter iter)
	{
		CellRendererText r = (CellRendererText) cell;
		Channel c = (Channel) model.GetValue (iter, 0);
		if (! c.Subscribed) {
			r.Markup = String.Format ("<b>{0}</b>\n<i>{1}</i>\n{2}",
				StringUtils.EscapeForPango (c.Title),
				StringUtils.EscapeForPango (c.Category),
				StringUtils.EscapeForPango (c.Description));
		} else {
			int n = c.NumberCasts ();
			int e = c.NumberErrorCasts ();
			r.Markup = String.Format ("<b>{0}</b>\n" +
					"<i>" + 
					Catalog.GetPluralString ("{1} out of {2} podcast downloaded, {4} unavailable.",
						"{1} out of {2} podcasts downloaded, {4} unavailable.", n) +
					"</i>\n{5}",
				StringUtils.EscapeForPango (c.Title),
				n - c.NumberUndownloadedCasts () - e,
				n,
				(n == 1 ? "" : "s"),
				e,
				StringUtils.EscapeForPango (c.Description));
		}
	}
	
	private void BoolCellDataFunc (TreeViewColumn col, CellRenderer cell,
		TreeModel model, TreeIter iter)
	{
		CellRendererToggle t = (CellRendererToggle) cell;
		Channel c = (Channel) model.GetValue (iter, 0);
		t.Active = c.Subscribed;

	}

	private void PixCellDataFunc (TreeViewColumn col, CellRenderer cell,
		TreeModel model, TreeIter iter)
	{
		CellRendererPixbuf pix = (CellRendererPixbuf) cell;
		Channel c = (Channel) model.GetValue (iter, 0);
		pix.StockSize = (uint) IconSize.Button;
		if (c.Error != null && c.Error != "") {
			pix.StockId = Stock.DialogError;
		} else {
			if (c.Subscribed) {
				if (c.NumberUndownloadedCasts() > 0) {
					pix.StockId = Stock.Refresh;
				} else {
					pix.StockId = Stock.Apply;
				}
			} else {
				// blank, for nothing known
				pix.StockId = "";
			}
		}
	}
	
	private void OnCellToggled (object o, ToggledArgs args)
	{
		TreePath path = new TreePath (args.Path);
		TreeIter iter;
		list.Model.GetIter (out iter, path);
		Channel c = (Channel) list.Model.GetValue (iter, 0);
		c.Subscribed = ! c.Subscribed;
		// schedule new subscription for network fetch
		if (c.Subscribed) {
			// blank out the error so we re-fetch if needs be
			c.ClearError ();
			GLib.Idle.Add (new GLib.IdleHandler (chanstore.FetchNextChannel));
		}
	}
	
	// Handlers
	public void OnWindowResponse (object o, ResponseArgs args)
	{
		switch ((int) args.ResponseId) {
			case (int) ResponseType.DeleteEvent:
			case (int) ResponseType.Close:
			   	window.Visible = false;

			   	break;
			   
			case (int) ResponseType.Add:
				AddChannelWindow w = new AddChannelWindow ();
				int resp = w.Run ();
				w.Hide ();
				if (resp == (int) Gtk.ResponseType.Ok && w.Url != "")
				{
					chanstore.AddNewChannel (w.Url);
				}
				break;
				
			case (int) ResponseType.Delete:
				DeleteSelection ();
				break;
				
			default:
				break;			
		}
	}
	
	private void DeleteSelection ()
	{
		ListStore store = (ListStore) list.Model;
		TreeIter iter;
		TreeModel model;
		TreePath[] selected = list.Selection.GetSelectedRows (out model);
 
 		System.Collections.ArrayList toremove = new System.Collections.ArrayList ();

		// Confirm with user

		DeleteFilesDialog dialog = new DeleteFilesDialog ();
		Gtk.ResponseType resp = (Gtk.ResponseType) dialog.Run ();
		dialog.Destroy ();
		if (resp != Gtk.ResponseType.Ok)  {
			return;
		}

		foreach (TreePath path in selected) {
			model.GetIter (out iter, path);
			Channel c = (Channel) model.GetValue (iter, 0);
			toremove.Add (c);
		}
		foreach (Channel c in toremove) {	
			store.GetIterFirst (out iter);
			while (store.IterIsValid (iter)) {
				Channel d = (Channel) store.GetValue (iter, 0);
				if (c.ID == d.ID) {
					store.Remove (ref iter);
					break;
				}
				store.IterNext (ref iter);
			}

			c.TrashCacheDir ();
			c.Delete ();
		}
	}
	
	// Adding stuff:
	//   Get URL from user
	//   Then run AddNewChannel
	
	private void OnChannelAdded (Channel c)
	{
		ListStore store = (ListStore) list.Model;
		
		if (c.Error != "") {
			c.Delete ();
			
			ErrorDialog ed = new ErrorDialog (
				Catalog.GetString ("Error Adding Channel - Monopod Podcast Client"),
				Catalog.GetString ("Channel could not be added"),
				Catalog.GetString ("The channel either could not be found or contains errors."));
			ed.Run ();
			ed.Destroy ();
		} else {
			if (TestInclusion (c, showbox.Active)) {
				store.AppendValues (c);
			}
		}
	}
	
	public void OnWindowDeleteEvent (object o, DeleteEventArgs args)
	{
	    args.RetVal = true;
	}
	
	private void OnEntryKeyPressEvent (object o, KeyPressEventArgs args)
	{
	    args.RetVal = false;
	}

	public void OnShowboxChanged (object o, EventArgs args)
	{
	    if (search_timeout_id > 0)
			GLib.Source.Remove (search_timeout_id);

	    search_timeout_id = GLib.Timeout.Add (search_timeout,
	        new GLib.TimeoutHandler (Search));
	}

	private void OnEntryChanged (object o, EventArgs args)
	{
	    if (ignore_change)
	        return;

	    if (search_timeout_id > 0)
			GLib.Source.Remove (search_timeout_id);

	    search_timeout_id = GLib.Timeout.Add (search_timeout,
	        new GLib.TimeoutHandler (Search));
	}

	private void OnSelectionChanged (object o, EventArgs args)
	{
		delete_button.Sensitive  = list.HasSelection;
	}

	private bool TestInclusion (Channel c, int mode)
	{
		if (mode == (int) ShowMode.Errors) {
			if (c.Error != null && c.Error != "")
				return true;
		} else if (mode == (int) ShowMode.Subscribed) {
			if (c.Subscribed)
				return true;
		} else {
			return true;
		}
		return false;
	}
	
	protected bool Search ()
	{
		ListStore store = (ListStore) list.Model;
		System.Collections.ArrayList ids;
		int mode = showbox.Active;		
		string search = entry.Text.Trim ();
		
		if (search == lastsearch && mode == lastmode) {
			return false;
		}
		if (search.Length > 0) {
			ids = chanstore.ChannelSearch (entry.SearchBits);
			
			TreeIter iter = new TreeIter ();
			store.GetIterFirst (out iter);

			while (store.IterIsValid (iter)) {
				Channel c = (Channel) store.GetValue (iter, 0);
				// System.Console.WriteLine("Visiting id {0}", c.ID);
				if (! ids.Contains (c.ID)) {
					// original id not in search results, so remove
					// System.Console.WriteLine("Removing {0}", c.ID);
					store.Remove (ref iter);
					// Remove moves iter on, no need to Next
				} else {
					// remove anything there already: no need to re-add it
					ids.Remove (c.ID);
					if (! TestInclusion (c, mode)) {
						store.Remove (ref iter);
					} else {
						store.IterNext (ref iter);
					}
				}
			}

			foreach (int id in ids)	{
				// System.Console.WriteLine("Adding in {0}", id);
				Channel c = chanstore.GetChannel (id);
				if (TestInclusion (c, mode))
					store.AppendValues (c);
			}
		} else {
			store.Clear ();

			foreach (Channel c in chanstore) {
				if (TestInclusion (c, mode))
					store.AppendValues (c);				
			}
		}
		lastsearch = search;
		lastmode = mode;
		list.SelectFirst ();
		return false;
	}
	
	public void OnChannelUpdated (Channel c)
	{
		// Find the part of the model containing the channel
		// and update it with this.
		ListStore store = (ListStore) list.Model;
		TreeIter iter = new TreeIter ();
		store.GetIterFirst (out iter);

		while (store.IterIsValid (iter)) {
			Channel d = (Channel) store.GetValue (iter, 0);
			if (c.ID == d.ID) {
				store.SetValue (iter, 0, c);
				// System.Console.WriteLine ("Updated view for {0}", d.Title);
				return;
			}
			store.IterNext (ref iter);
		}
	}
	
	private int StoreSortFunc (TreeModel model, TreeIter a, TreeIter b)
	{
		Channel chan_a = (Channel) model.GetValue (a, 0);
		Channel chan_b = (Channel) model.GetValue (b, 0);
		return String.Compare(chan_a.Title.ToLower(), chan_b.Title.ToLower ());
	}
	
	public SearchEntry Entry { 
		get { return entry; }
	}
	
	public CellRenderer TextRenderer {
		get { return text_renderer; }
	}
	
	public ItemList List {
		get { return list; }
	}
}
