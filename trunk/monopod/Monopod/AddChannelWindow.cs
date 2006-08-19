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
using Gtk;

public class AddChannelWindow : Dialog 
{
	[Glade.Widget] private Dialog window;
	[Glade.Widget] private Entry url_entry;

	public string Url {
		get { return url_entry.Text.Trim (); }
	}

	public AddChannelWindow () : base (IntPtr.Zero)
	{
		Glade.XML gxml = new Glade.XML (null, "NewChannel.glade", "window", null);
		gxml.Autoconnect (this);
		Raw = window.Handle;
		url_entry.Text = "";
		url_entry.GrabFocus ();
		window.Present ();	
	}

}