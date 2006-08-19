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

using System;
using System.Threading;
using Gtk;
using IPod;

public class IPodChooseWindow : Window 
{
	private Store store;
	private DeviceCombo combo;
	private ThreadNotify notify;
	private ProgressDialog progress;
	
	[Glade.Widget] private Dialog window;
	[Glade.Widget] private HBox combo_container;
	[Glade.Widget] private Button update_button;
	[Glade.Widget] private Button refresh_button;

	public IPodChooseWindow (Store s) : base (IntPtr.Zero)
	{
		store = s;
		Glade.XML gxml = new Glade.XML (null, "IPodWindow.glade", "window", null);
		gxml.Autoconnect (this);
		Raw = window.Handle;
		combo = new DeviceCombo ();
		combo.Changed += OnDeviceChanged;
		combo_container.PackStart (combo, true, true, 0);
		refresh_button.Clicked += OnRefreshClicked;
		update_button.Clicked += OnUpdateClicked;
		window.Icon = MainClass.program_pixbuf48;
		notify = new ThreadNotify (new ReadyEvent (OnNotify));
		progress = new ProgressDialog (this);
		SetDevice ();
		window.Response += OnWindowResponse;
		window.DeleteEvent += OnWindowDeleteEvent;	
		combo.ShowAll ();
	}

	private void SetDevice ()
	{
		if (combo.ActiveDevice == null) {
			update_button.Sensitive = false;
			progress.SongDatabase = null;
		} else {
			update_button.Sensitive = true;
			progress.SongDatabase = combo.ActiveDevice.SongDatabase;
		}
	}

	private void OnDeviceChanged (object o, EventArgs args) {
		SetDevice ();
	}

	private void OnRefreshClicked (object o, EventArgs args) {
		combo.Refresh ();
	}
	
	private void OnUpdateClicked (object o, EventArgs args)
	{
		if (combo.ActiveDevice != null) {
			window.Sensitive = false;
			Thread thread = new Thread (new ThreadStart (DoUpdate));
			thread.Start ();	
		}
	}
	
	private void DoUpdate ()
	{
		IPodSync sync = new IPodSync (store, combo.ActiveDevice);
		sync.Sync ();
		notify.WakeupMain ();	
	}
	
	private void OnNotify () 
	{
		window.Sensitive = true;
	}
	
	private void OnWindowResponse (object o, ResponseArgs args)
	{
		switch ((int) args.ResponseId) {
			case (int) ResponseType.DeleteEvent:
			case (int) ResponseType.Cancel:
			case (int) ResponseType.Close:
			   	window.Visible = false;
			   	break;
			default:
				break;
		}
	}
	
	private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
	{
	    args.RetVal = true;
	}
}

#endif
