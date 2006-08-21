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
using Gtk;
using GLib;
using Mono.Unix;

class MainClass
{
	private ChannelWindow channels;
#if USING_IPOD
	private IPodChooseWindow ipodwindow;
#endif
	private Store store;

	public static Gdk.Pixbuf program_pixbuf;
	public static Gdk.Pixbuf program_pixbuf16;
	public static Gdk.Pixbuf program_pixbuf48;
	
	private static Gnome.Client session_client;
	
	public static void Main (string[] args)
	{
		Catalog.Init ("monopod", Defines.GNOME_LOCALE_DIR);

		new Gnome.Program ("monopod", Defines.VERSION, Gnome.Modules.UI, args);
		// Application.Init ();
		session_client = Gnome.Global.MasterClient ();
		session_client.Die += new EventHandler (OnDieEvent);
		session_client.SaveYourself += new Gnome.SaveYourselfHandler (OnSaveYourselfEvent);

		// LogFunc logFunc = new LogFunc (Log.PrintTraceLogFunction);
		// Log.SetLogHandler ("GLib-GObject", LogLevelFlags.Critical, logFunc);
		
		program_pixbuf = new Gdk.Pixbuf (null, "podcast.png");  
		program_pixbuf16 = program_pixbuf.ScaleSimple (16, 16, Gdk.InterpType.Hyper);
		program_pixbuf48 = program_pixbuf.ScaleSimple (48, 48, Gdk.InterpType.Hyper);
		new MainClass ();
		Application.Run ();
	}

	private Menu popupMenu;
	
	private static string userdir;
	
	public MainClass()
	{
		userdir = Path.Combine (
			Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
			"Monopod");
		
		// check userdir exists, make if not
		if (!Directory.Exists (userdir)) {
			DirectoryInfo d = Directory.CreateDirectory (userdir);
			if ( d == null ) {
				// TODO: throw a wobbly
			}
		}	
		store = new Store (Path.Combine (userdir, ".monopod.db"), userdir);
		if (store.NumberOfChannels == 0) {
			store.AddDefaultChannels ();
			channels = new ChannelWindow (store);
			channels.Show ();
		} else {
			channels = new ChannelWindow (store);
		}
#if USING_IPOD
		ipodwindow = new IPodChooseWindow (store);
#endif		
		InitIcon ();
		InitMenu ();

		// kick off downloading
		store.FetchNextChannel ();
		store.FetchNextCast ();
		
	}
	
	private void InitIcon()
	{
		EventBox eb = new EventBox();
		
		eb.Add (new Image(program_pixbuf16)); // using stock icon
		// hooking event
		eb.ButtonPressEvent += new ButtonPressEventHandler (this.OnImageClick);
		Egg.TrayIcon icon = new Egg.TrayIcon ("Monopod");
		icon.Add(eb);
		// showing the trayicon
		icon.ShowAll();
	}
	
	private void InitMenu ()
	{
      	popupMenu = new Menu();  
      	MenuItem item = new MenuItem ( Catalog.GetString ("_Subscriptions"));
      	item.Activated += new EventHandler (this.OnSubscriptions);
      	popupMenu.Add (item);
      	
      	MenuItem item2 = new MenuItem ( Catalog.GetString ("Show _Podcasts"));
      	item2.Activated += new EventHandler (this.OnShowPodcasts);
      	popupMenu.Add (item2);

#if USING_IPOD
	 	MenuItem item3 = new MenuItem ( Catalog.GetString ("_Update iPod"));
	 	item3.Activated += new EventHandler (this.OnUpdateIPod);
	 	popupMenu.Add (item3);
#endif
      	
      	ImageMenuItem aboutItem = new ImageMenuItem (Catalog.GetString ("_About"));
      	aboutItem.Image  = new Image (Gnome.Stock.About, IconSize.Menu);
      	popupMenu.Add (aboutItem); 
      	ImageMenuItem menuPopup1 = new ImageMenuItem (Catalog.GetString ("_Quit"));
      	Image appimg = new Image (Gtk.Stock.Quit, IconSize.Menu );
      	menuPopup1.Image = appimg;
      	popupMenu.Add(menuPopup1);

      	menuPopup1.Activated += OnQuit;
      	aboutItem.Activated += OnAbout;
		popupMenu.ShowAll();

	} 
	
	private void OnImageClick (object o, ButtonPressEventArgs args) // handler for mouse click
	{
		switch (args.Event.Button) {
	      	case 1:
	      		OnShowPodcasts (o, (EventArgs) args);
	      		break;
			case 3:
	      		popupMenu.Popup(null, null, null, args.Event.Button,
	      			args.Event.Time);
	      		break;

	      	default:
	      		break;
   		}
	}
	
	private void OnShowPodcasts (object o, EventArgs args)
	{
		Gnome.Url.Show ("file://" + userdir);
	}

#if USING_IPOD	
	private void OnUpdateIPod (object o, EventArgs args)
	{
		ipodwindow.Present ();
	}
#endif
	
	private void OnSubscriptions (object o, EventArgs args)
	{
		channels.Run ();
	}
	
	private void OnQuit (object o, EventArgs args)
	{
		Application.Quit(); // quits the application when the users clicks the popup menu
	}
	
	private void OnAbout (object o, EventArgs args)
	{
		// TODO: make use AssemblyInfo for version
		AboutDialog about = new Gtk.AboutDialog();
		
		about.Name = Catalog.GetString ("Monopod");
		about.Version = Defines.VERSION;
		about.Copyright = ("Copyright © 2005 Edd Dumbill");
		about.Comments = Catalog.GetString ("A simple podcast client.");
		about.Authors = new string[] {"Edd Dumbill <edd@gnome.org>",
 					      "James Willcox <snorp@snorp.net>"};
		about.Documenters = new string[] {};
		about.TranslatorCredits = (Catalog.GetString ("translator-credits") ==
				           "translator-credits" ? null : 
				           Catalog.GetString ("translator-credits"));
		about.Icon = program_pixbuf16;
		about.Show ();
	}

	private static void OnDieEvent (object o, EventArgs args)
	{
		Environment.Exit (0);
	}

	private static void OnSaveYourselfEvent (object o, Gnome.SaveYourselfArgs args)
	{
		string [] argv = { "monopod" };

		session_client.SetRestartCommand (1, argv);
	}
}
