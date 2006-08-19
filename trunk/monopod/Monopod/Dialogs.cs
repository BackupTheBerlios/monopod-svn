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
using Glade;

public class DeleteFilesDialog : Dialog
{
	[Glade.Widget] private Window window;
	[Glade.Widget] private Button cancel_button;

	public DeleteFilesDialog () : base (IntPtr.Zero)
	{
		Glade.XML gxml = new Glade.XML (null, "DeleteFiles.glade", "window", null);
		gxml.Autoconnect (this);
		window.Icon = MainClass.program_pixbuf;
		Raw = window.Handle;
		this.Default = cancel_button;
		window.Present ();
	}
}

public class ErrorDialog : Dialog
{
	[Glade.Widget] private Window window;
	[Glade.Widget] private Label error_message;
	[Glade.Widget] private Label error_explanation;

	public ErrorDialog (string title, string error,
			      string explanation) : base (IntPtr.Zero)
	{
		Glade.XML gxml = new Glade.XML (null, "ErrorDialog.glade", "window", null);
		gxml.Autoconnect (this);
		Raw = window.Handle;
		window.Title = title;
		window.Icon = MainClass.program_pixbuf;
		error_message.Markup = "<big><b>" + error + "</b></big>";
		error_message.UseMarkup = true;
		error_explanation.Text = explanation;
		window.Present ();
	}
}
